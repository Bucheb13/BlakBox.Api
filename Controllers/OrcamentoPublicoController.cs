using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using BlakBox.Api.Models;
using BlakBox.Api.Services;

namespace BlakBox.Api.Controllers;

[AllowAnonymous]
[EnableRateLimiting("public-quotes")]
[Route("orcamento/{token}")]
public sealed class OrcamentoPublicoController : ControllerBase
{
    private readonly OrcamentoPublicoService _orcamentos;

    public OrcamentoPublicoController(OrcamentoPublicoService orcamentos) =>
        _orcamentos = orcamentos;

    [HttpGet]
    public async Task<IActionResult> Exibir(string token)
    {
        Response.Headers.CacheControl = "no-store, private";
        Response.Headers["X-Robots-Tag"] = "noindex, nofollow, noarchive";
        var quote = await _orcamentos.ObterAsync(Request.Host.Host, token);
        if (quote == null)
            return NotFound();

        return Content(CriarHtml(quote, token), "text/html; charset=utf-8");
    }

    [HttpGet("pdf")]
    public async Task<IActionResult> Baixar(string token)
    {
        Response.Headers.CacheControl = "no-store, private";
        Response.Headers["X-Robots-Tag"] = "noindex, nofollow, noarchive";
        var pdf = await _orcamentos.BaixarPdfAsync(Request.Host.Host, token);
        if (pdf == null)
            return NotFound();
        return File(pdf, "application/pdf", $"Orcamento_{token[..8]}.pdf");
    }

    private static string CriarHtml(OrcamentoPublico quote, string token)
    {
        var html = new StringBuilder();
        string E(string? value) => HtmlEncoder.Default.Encode(value ?? string.Empty);
        string Dinheiro(decimal value) => "R$ " + value.ToString("N2", CultureInfo.GetCultureInfo("pt-BR"));

        html.Append("<!doctype html><html lang=\"pt-BR\"><head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width, initial-scale=1\"><meta name=\"robots\" content=\"noindex,nofollow\"><title>Orçamento OS #")
            .Append(quote.OrdemServicoId).Append(" — ").Append(E(quote.Oficina))
            .Append("</title><style>")
            .Append("""
            :root{font-family:system-ui,-apple-system,"Segoe UI",sans-serif;color:#182230;background:#f3f6fa}
            *{box-sizing:border-box}body{margin:0;padding:32px 16px}main{max-width:760px;margin:auto}
            article{background:#fff;border:1px solid #e2e8f0;border-radius:18px;box-shadow:0 12px 36px #18223012;overflow:hidden}
            header{padding:28px 32px;background:#111827;color:#fff}header p{margin:0 0 8px;color:#bdc8d8}
            h1{margin:0;font-size:26px}.content{padding:28px 32px 32px}
            .meta{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:16px;margin-bottom:24px}
            .label{display:block;margin-bottom:4px;color:#667085;font-size:12px;text-transform:uppercase;letter-spacing:.05em}
            .value{font-weight:600}.description{margin:0 0 24px;color:#475467;white-space:pre-wrap}
            table{width:100%;border-collapse:collapse}th,td{padding:12px 8px;border-bottom:1px solid #eaecf0;text-align:left}
            th{color:#667085;font-size:12px;text-transform:uppercase}.number{text-align:right;white-space:nowrap}
            .totals{max-width:320px;margin:20px 0 26px auto}.row{display:flex;justify-content:space-between;gap:16px;padding:6px 0}
            .grand{border-top:1px solid #d0d5dd;margin-top:6px;padding-top:14px;font-size:20px;font-weight:750}
            .download{display:block;width:100%;border-radius:10px;padding:15px 20px;background:#1457d9;color:white;text-align:center;text-decoration:none;font-weight:700}
            footer{padding:18px 32px;border-top:1px solid #eaecf0;color:#667085;font-size:13px;text-align:center}
            @media(max-width:560px){body{padding:12px}header,.content{padding:22px 18px}footer{padding:16px 18px}.meta{grid-template-columns:1fr;gap:12px}th,td{padding:10px 4px;font-size:13px}}
            """)
            .Append("</style></head><body><main><article><header><p>")
            .Append(E(quote.Oficina)).Append("</p><h1>Orçamento da OS #").Append(quote.OrdemServicoId)
            .Append("</h1></header><section class=\"content\"><div class=\"meta\"><div><span class=\"label\">Cliente</span><span class=\"value\">")
            .Append(E(quote.Cliente)).Append("</span></div><div><span class=\"label\">Emitido em</span><span class=\"value\">")
            .Append(E(quote.DataEmissao.ToString("dd/MM/yyyy"))).Append("</span></div>");

        if (!string.IsNullOrWhiteSpace(quote.Veiculo))
            html.Append("<div><span class=\"label\">Veículo</span><span class=\"value\">").Append(E(quote.Veiculo)).Append("</span></div>");
        if (!string.IsNullOrWhiteSpace(quote.Placa))
            html.Append("<div><span class=\"label\">Placa</span><span class=\"value\">").Append(E(quote.Placa)).Append("</span></div>");
        html.Append("</div>");
        if (!string.IsNullOrWhiteSpace(quote.Descricao))
            html.Append("<p class=\"description\">").Append(E(quote.Descricao)).Append("</p>");

        html.Append("<table><thead><tr><th>Descrição</th><th class=\"number\">Qtd.</th><th class=\"number\">Unitário</th><th class=\"number\">Total</th></tr></thead><tbody>");
        foreach (var item in quote.Itens)
        {
            html.Append("<tr><td>").Append(E(item.Descricao)).Append("</td><td class=\"number\">")
                .Append(item.Quantidade).Append("</td><td class=\"number\">")
                .Append(Dinheiro(item.ValorUnitario)).Append("</td><td class=\"number\">")
                .Append(Dinheiro(item.Total)).Append("</td></tr>");
        }
        html.Append("</tbody></table><div class=\"totals\"><div class=\"row\"><span>Peças e serviços</span><span>")
            .Append(Dinheiro(quote.TotalPecas)).Append("</span></div>");
        if (quote.Desconto > 0)
            html.Append("<div class=\"row\"><span>Desconto</span><span>− ").Append(Dinheiro(quote.Desconto)).Append("</span></div>");
        html.Append("<div class=\"row grand\"><span>Total</span><span>").Append(Dinheiro(quote.Total))
            .Append("</span></div></div><a class=\"download\" href=\"/orcamento/").Append(E(token))
            .Append("/pdf\">Baixar orçamento em PDF</a></section><footer>Este orçamento foi enviado por ")
            .Append(E(quote.Oficina)).Append(".</footer></article></main></body></html>");
        return html.ToString();
    }
}
