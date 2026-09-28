using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BlakBox.Api.Authentication;
using BlakBox.Api.Data;
using BlakBox.Api.Models;

namespace BlakBox.Api.Controllers;

[ApiController]
[Route("api/armazenamento/r2")]
public sealed class ArmazenamentoController : ControllerBase
{
    private readonly LicencaDbContext _db;
    private readonly IDataProtector _protector;

    public ArmazenamentoController(
        LicencaDbContext db,
        IDataProtectionProvider protectionProvider)
    {
        _db = db;
        _protector = protectionProvider.CreateProtector("BlakBox.Api.R2Credentials.v1");
    }

    [Authorize(AuthenticationSchemes = InstalacaoAuthenticationHandler.SchemeName)]
    [HttpPut]
    [RequestSizeLimit(16_384)]
    public async Task<IActionResult> Salvar([FromBody] ConfiguracaoR2Request request)
    {
        var idClaim = User.FindFirstValue("instalacao_id");
        if (!int.TryParse(idClaim, out var instalacaoId))
            return Unauthorized();

        if (string.IsNullOrWhiteSpace(request.Endpoint) ||
            string.IsNullOrWhiteSpace(request.Bucket) ||
            string.IsNullOrWhiteSpace(request.AccessKey) ||
            string.IsNullOrWhiteSpace(request.SecretKey))
            return BadRequest(new { sucesso = false, mensagem = "A configuração R2 está incompleta." });

        if (!Uri.TryCreate(request.Endpoint, UriKind.Absolute, out var endpoint) ||
            endpoint.Scheme != Uri.UriSchemeHttps)
            return BadRequest(new { sucesso = false, mensagem = "O endpoint R2 deve usar HTTPS." });

        var slugOficina = CriarSlug(request.NomeOficina);
        var slugEmUso = await _db.ConfiguracoesR2Instalacoes
            .AnyAsync(x => x.InstalacaoId != instalacaoId && x.OficinaSlug == slugOficina);
        if (slugEmUso)
            return Conflict(new { sucesso = false, mensagem = "O nome da oficina gera um subdomínio já utilizado. Altere o nome da oficina para que o link público seja exclusivo." });

        var config = await _db.ConfiguracoesR2Instalacoes
            .SingleOrDefaultAsync(x => x.InstalacaoId == instalacaoId);

        if (config == null)
        {
            config = new ConfiguracaoR2Instalacao { InstalacaoId = instalacaoId };
            _db.ConfiguracoesR2Instalacoes.Add(config);
        }

        config.Endpoint = endpoint.ToString().TrimEnd('/');
        config.Bucket = request.Bucket.Trim();
        config.AccessKeyProtegida = _protector.Protect(request.AccessKey.Trim());
        config.SecretKeyProtegida = _protector.Protect(request.SecretKey.Trim());
        config.PublicBaseUrl = request.PublicBaseUrl?.Trim().TrimEnd('/');
        config.OficinaSlug = slugOficina;
        config.Ativo = request.Ativo;
        config.AtualizadoEm = BlakBox.Api.Models.RelogioSistema.Agora;

        await _db.SaveChangesAsync();
        return Ok(new { sucesso = true, mensagem = "Configuração R2 sincronizada." });
    }

    private static string CriarSlug(string nome)
    {
        var semAcentos = new string(nome.Normalize(NormalizationForm.FormD)
            .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            .ToArray()).Normalize(NormalizationForm.FormC).ToLowerInvariant();
        var slug = Regex.Replace(semAcentos, "[^a-z0-9]+", "-").Trim('-');
        if (slug.Length > 63)
            slug = slug[..63].TrimEnd('-');
        return string.IsNullOrWhiteSpace(slug) ? "oficina" : slug;
    }
}

public sealed class ConfiguracaoR2Request
{
    [Required, MaxLength(500)]
    public string Endpoint { get; set; } = string.Empty;

    [Required, MaxLength(150)]
    public string Bucket { get; set; } = string.Empty;

    [Required, MaxLength(300)]
    public string AccessKey { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string SecretKey { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? PublicBaseUrl { get; set; }

    [Required, MaxLength(150)]
    public string NomeOficina { get; set; } = string.Empty;

    public bool Ativo { get; set; }
}
