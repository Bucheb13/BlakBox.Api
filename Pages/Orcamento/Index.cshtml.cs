using BlakBox.Api.Models;
using BlakBox.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;

namespace BlakBox.Api.Pages.Orcamento;

[AllowAnonymous]
[EnableRateLimiting("public-quotes")]
public sealed class IndexModel : PageModel
{
    private readonly OrcamentoPublicoService _orcamentos;

    public IndexModel(OrcamentoPublicoService orcamentos)
    {
        _orcamentos = orcamentos;
    }

    [BindProperty(SupportsGet = true)]
    public string Token { get; set; } = string.Empty;

    public OrcamentoPublico? Orcamento { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        Response.Headers.CacheControl = "no-store, private";
        Response.Headers["X-Robots-Tag"] = "noindex, nofollow, noarchive";

        Orcamento = await _orcamentos.ObterAsync(Request.Host.Host, Token);
        return Orcamento is null ? NotFound() : Page();
    }

    public async Task<IActionResult> OnGetBaixarAsync()
    {
        Response.Headers.CacheControl = "no-store, private";
        Response.Headers["X-Robots-Tag"] = "noindex, nofollow, noarchive";

        var pdf = await _orcamentos.BaixarPdfAsync(Request.Host.Host, Token);
        if (pdf is null)
            return NotFound();

        var tokenPart = Token.Length > 8 ? Token[..8] : Token;
        return File(pdf, "application/pdf", $"Orcamento_OS_{tokenPart}.pdf");
    }
}
