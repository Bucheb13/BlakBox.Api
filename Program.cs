using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Npgsql;
using System.Net;
using System.Threading.RateLimiting;

using BlakBox.Api.Authentication;
using BlakBox.Api.Data;
using BlakBox.Api.Middleware;
using BlakBox.Api.Services;

var builder = WebApplication.CreateBuilder(args);

if (!builder.Environment.IsDevelopment())
{
    var connectionString = RequireSetting(builder.Configuration, "ConnectionStrings:Default", 1);
    var postgres = new NpgsqlConnectionStringBuilder(connectionString);
    if (string.IsNullOrWhiteSpace(postgres.Host) ||
        string.IsNullOrWhiteSpace(postgres.Database) ||
        string.IsNullOrWhiteSpace(postgres.Username) ||
        string.IsNullOrWhiteSpace(postgres.Password) ||
        postgres.SslMode is SslMode.Disable or SslMode.Prefer)
    {
        throw new InvalidOperationException(
            "ConnectionStrings:Default deve ter host, banco, usuário, senha e SSL habilitado.");
    }

    RequireSetting(builder.Configuration, "Admin_Username", 1);
    RequireSetting(builder.Configuration, "Admin_Password", 6);
    var allowedHosts = RequireSetting(builder.Configuration, "AllowedHosts", 1);
    if (allowedHosts.Contains('*'))
    {
        throw new InvalidOperationException(
            "AllowedHosts deve listar os domínios públicos da API; curingas não são aceitos em produção.");
    }

}

// =====================================================
// SERVIÇOS
// =====================================================

builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/");
    options.Conventions.AllowAnonymousToPage("/Login");
});

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = _ =>
            new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(new
            {
                sucesso = false,
                codigo = "REQUISICAO_INVALIDA",
                mensagem = "A requisição contém campos ausentes ou inválidos."
            });
    });

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    var proxies = builder.Configuration["ForwardedHeaders:KnownProxies"];
    if (!string.IsNullOrWhiteSpace(proxies))
    {
        foreach (var value in proxies.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            if (IPAddress.TryParse(value, out var address))
            {
                options.KnownProxies.Add(address);
            }
        }
    }
});

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen();


// =====================================================
// BANCO DE DADOS
// =====================================================

builder.Services.AddDbContext<LicencaDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("Default")
    ));

builder.Services.AddDataProtection()
    .SetApplicationName("BlakBox.Api")
    .PersistKeysToDbContext<LicencaDbContext>();


// =====================================================
// AUTENTICAÇÃO
// =====================================================

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme =
            CookieAuthenticationDefaults.AuthenticationScheme;

        options.DefaultChallengeScheme =
            CookieAuthenticationDefaults.AuthenticationScheme;
    })
    .AddCookie(options =>
    {
        options.Cookie.Name = builder.Environment.IsProduction()
            ? "__Host-BlakBox.Admin"
            : "BlakBox.Admin";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
        options.Cookie.Path = "/";
        options.LoginPath = "/Login";
        options.AccessDeniedPath = "/Login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Events.OnValidatePrincipal = context =>
        {
            var configuredPassword = builder.Configuration["Admin_Password"];
            var configuredUsername = builder.Configuration["Admin_Username"];
            var stampClaim = context.Principal?.FindFirst("admin_security_stamp")?.Value;
            var expectedStamp = configuredPassword == null || configuredUsername == null
                ? string.Empty
                : Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
                    System.Text.Encoding.UTF8.GetBytes(configuredUsername + "\0" + configuredPassword)));
            if (!string.Equals(stampClaim, expectedStamp, StringComparison.Ordinal))
            {
                context.RejectPrincipal();
                return context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }

            return Task.CompletedTask;
        };
    })
    .AddScheme<
        AuthenticationSchemeOptions,
        InstalacaoAuthenticationHandler>(
            InstalacaoAuthenticationHandler.SchemeName,
            _ =>
            {
            });

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.ContentType = "application/json; charset=utf-8";
        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            sucesso = false,
            codigo = "LIMITE_DE_REQUISICOES",
            mensagem = "Muitas requisições. Aguarde antes de tentar novamente."
        }, cancellationToken);
    };
    options.AddPolicy("registration", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 8,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
    options.AddPolicy("license-validation", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 300,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
    options.AddPolicy("admin-login", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 6,
                Window = TimeSpan.FromMinutes(5),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});


// =====================================================
// SERVIÇOS DA APLICAÇÃO
// =====================================================

builder.Services.AddScoped<LicencaService>();

builder.Services.AddScoped<LicencaGestaoService>();

builder.Services.AddScoped<CredencialInstalacaoService>();

builder.Services.AddScoped<SistemaGestaoService>();

builder.Services.AddScoped<ClienteGestaoService>();

builder.Services.AddScoped<InstalacaoGestaoService>();

builder.Services.AddScoped<PlanoGestaoService>();
builder.Services.AddHostedService<ApiRequisicaoLimpezaService>();


// =====================================================
// AUTORIZAÇÃO
// =====================================================

builder.Services.AddAuthorization();


var app = builder.Build();

// Tabela adicional criada de forma idempotente para instalações já existentes.
await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<LicencaDbContext>();
    await db.Database.ExecuteSqlRawAsync("""
        CREATE TABLE IF NOT EXISTS "DataProtectionKeys" (
            "Id" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
            "FriendlyName" character varying(450),
            "Xml" text
        );
        CREATE TABLE IF NOT EXISTS "ConfiguracaoR2Instalacao" (
            "InstalacaoId" integer PRIMARY KEY
                REFERENCES "Instalacao" ("InstalacaoId") ON DELETE CASCADE,
            "Endpoint" character varying(500),
            "Bucket" character varying(150),
            "AccessKeyProtegida" character varying(2000),
            "SecretKeyProtegida" character varying(4000),
            "PublicBaseUrl" character varying(500),
            "Ativo" boolean NOT NULL DEFAULT FALSE,
            "AtualizadoEm" timestamp without time zone NOT NULL
        );
        """);
}

app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    await next();
});

app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
{
    var exception = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerPathFeature>()?.Error;
    var logger = context.RequestServices.GetRequiredService<ILoggerFactory>()
        .CreateLogger("GlobalExceptionHandler");
    if (exception != null)
    {
        logger.LogError(exception, "Falha não tratada. TraceId: {TraceId}", context.TraceIdentifier);
    }

    context.Response.Clear();
    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
    if (context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
    {
        context.Response.ContentType = "application/json; charset=utf-8";
        await context.Response.WriteAsJsonAsync(new
        {
            sucesso = false,
            codigo = "ERRO_INTERNO",
            mensagem = "Ocorreu um erro interno. Informe o identificador da requisição ao suporte.",
            traceId = context.TraceIdentifier
        });
        return;
    }

    context.Response.ContentType = "text/html; charset=utf-8";
    await context.Response.WriteAsync(
        "<!doctype html><html lang=\"pt-BR\"><meta charset=\"utf-8\"><title>Erro</title>" +
        "<h1>Não foi possível concluir a operação.</h1><p>Identificador: " +
        System.Net.WebUtility.HtmlEncode(context.TraceIdentifier) + "</p></html>");
}));


// =====================================================
// ARQUIVOS ESTÁTICOS
// =====================================================
//
// Permite servir arquivos de wwwroot:
//
// /css/instalacoes.css
// /css/clientes.css
// /js/...
// /images/...
//
// Deve ficar antes do mapeamento das páginas/endpoints.
//

app.UseStaticFiles();

app.UseRouting();


// =====================================================
// SWAGGER
// =====================================================

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();

    app.UseSwaggerUI();
}


// =====================================================
// AUTENTICAÇÃO
// =====================================================

app.UseAuthentication();

app.UseRateLimiter();


// =====================================================
// MONITORAMENTO
// =====================================================
//
// O middleware permanece depois da autenticação,
// permitindo que informações da instalação autenticada
// estejam disponíveis quando o monitoramento processar
// a requisição.
//

app.UseMiddleware<ApiMonitoramentoMiddleware>();


// =====================================================
// AUTORIZAÇÃO
// =====================================================

app.UseAuthorization();


// =====================================================
// ENDPOINTS
// =====================================================

app.MapControllers();

app.MapRazorPages();

app.MapGet("/health/live", () => Results.Ok(new { status = "ok" }));
app.MapGet("/health/ready", async (
    LicencaDbContext db,
    CancellationToken cancellationToken) =>
{
    try
    {
        if (!await db.Database.CanConnectAsync(cancellationToken))
        {
            return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
        }

        // Confirma também que as colunas da versão implantada existem.
        await db.CredenciaisInstalacao
            .AsNoTracking()
            .Select(x => new { x.ChaveRecuperacaoProtegida, x.RecuperacaoExpiraEm })
            .Take(1)
            .ToListAsync(cancellationToken);

        return Results.Ok(new { status = "ready" });
    }
    catch
    {
        return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
    }
});


// =====================================================
// EXECUÇÃO
// =====================================================

app.Run();

static string RequireSetting(
    IConfiguration configuration,
    string key,
    int minimumLength)
{
    var value = configuration[key];
    if (string.IsNullOrWhiteSpace(value) || value.Length < minimumLength)
    {
        throw new InvalidOperationException(
            $"A configuração obrigatória '{key}' não foi definida ou é menor que {minimumLength} caracteres.");
    }

    return value;
}
