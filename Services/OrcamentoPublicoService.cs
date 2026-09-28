using System.Text.Json;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using BlakBox.Api.Data;
using BlakBox.Api.Models;

namespace BlakBox.Api.Services;

public sealed class OrcamentoPublicoService
{
    private const string Prefixo = "orcamentos";
    private readonly LicencaDbContext _db;
    private readonly IDataProtector _protector;

    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true };

    public OrcamentoPublicoService(
        LicencaDbContext db,
        IDataProtectionProvider protectionProvider)
    {
        _db = db;
        _protector = protectionProvider.CreateProtector("BlakBox.Api.R2Credentials.v1");
    }

    public async Task<OrcamentoPublico?> ObterAsync(string host, string token)
    {
        var config = await ObterConfiguracaoAsync(host);
        if (config == null || !TokenValido(token))
            return null;

        using var client = CriarCliente(config);
        try
        {
            using var response = await client.GetObjectAsync(new GetObjectRequest
            {
                BucketName = config.Bucket!,
                Key = $"{Prefixo}/{token.ToLowerInvariant()}/orcamento.json"
            });
            await using var stream = response.ResponseStream;
            return await JsonSerializer.DeserializeAsync<OrcamentoPublico>(stream, JsonOptions);
        }
        catch (AmazonS3Exception ex) when (EhNaoEncontrado(ex))
        {
            return null;
        }
    }

    public async Task<byte[]?> BaixarPdfAsync(string host, string token)
    {
        var config = await ObterConfiguracaoAsync(host);
        if (config == null || !TokenValido(token))
            return null;

        using var client = CriarCliente(config);
        try
        {
            using var response = await client.GetObjectAsync(new GetObjectRequest
            {
                BucketName = config.Bucket!,
                Key = $"{Prefixo}/{token.ToLowerInvariant()}/orcamento.pdf"
            });
            await using var stream = response.ResponseStream;
            using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer);
            return buffer.ToArray();
        }
        catch (AmazonS3Exception ex) when (EhNaoEncontrado(ex))
        {
            return null;
        }
    }

    private async Task<ConfiguracaoR2Instalacao?> ObterConfiguracaoAsync(string host)
    {
        const string dominio = ".blakbox.com.br";
        var hostNormalizado = host.TrimEnd('.').ToLowerInvariant();
        if (!hostNormalizado.EndsWith(dominio, StringComparison.Ordinal))
            return null;

        var slug = hostNormalizado[..^dominio.Length];
        if (string.IsNullOrWhiteSpace(slug) || slug.Contains('.'))
            return null;

        return await _db.ConfiguracoesR2Instalacoes
            .AsNoTracking()
            .Where(x => x.Ativo && x.OficinaSlug == slug &&
                        x.Instalacao.Ativa && x.Instalacao.Cliente.Ativo &&
                        x.Instalacao.Sistema.Ativo)
            .FirstOrDefaultAsync();
    }

    private IAmazonS3 CriarCliente(ConfiguracaoR2Instalacao config)
    {
        if (string.IsNullOrWhiteSpace(config.Endpoint) ||
            string.IsNullOrWhiteSpace(config.Bucket) ||
            string.IsNullOrWhiteSpace(config.AccessKeyProtegida) ||
            string.IsNullOrWhiteSpace(config.SecretKeyProtegida))
            throw new InvalidOperationException("Configuração R2 incompleta.");

        var endpoint = new Uri(config.Endpoint);
        var s3Config = new AmazonS3Config
        {
            ServiceURL = endpoint.ToString(),
            ForcePathStyle = true,
            AuthenticationRegion = "auto"
        };
        return new AmazonS3Client(
            _protector.Unprotect(config.AccessKeyProtegida),
            _protector.Unprotect(config.SecretKeyProtegida),
            s3Config);
    }

    private static bool TokenValido(string token) =>
        token.Length == 32 && token.All(Uri.IsHexDigit);

    private static bool EhNaoEncontrado(AmazonS3Exception ex) =>
        ex.StatusCode == System.Net.HttpStatusCode.NotFound ||
        ex.ErrorCode is "NoSuchKey" or "NotFound";
}
