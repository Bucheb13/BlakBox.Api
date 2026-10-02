using BlakBox.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace BlakBox.Api.Controllers;

// Mantem compatibilidade com links de PDF gerados pela versao anterior da pagina.
[AllowAnonymous]
[EnableRateLimiting("public-quotes")]
[Route("orcamento/{token}")]
public sealed class OrcamentoPublicoController : ControllerBase
{
    private readonly OrcamentoPublicoService _orcamentos;

    public OrcamentoPublicoController(OrcamentoPublicoService orcamentos) =>
        _orcamentos = orcamentos;

    [HttpGet("pdf")]
    public async Task<IActionResult> Baixar(string token)
    {
        Response.Headers.CacheControl = "no-store, private";
        Response.Headers["X-Robots-Tag"] = "noindex, nofollow, noarchive";

        var pdf = await _orcamentos.BaixarPdfAsync(Request.Host.Host, token);
        if (pdf is null)
            return NotFound();

        var tokenPart = token.Length > 8 ? token[..8] : token;
        return File(pdf, "application/pdf", $"Orcamento_OS_{tokenPart}.pdf");
    }
}
