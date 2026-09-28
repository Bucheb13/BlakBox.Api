using System.Globalization;
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
        return quote == null
            ? NotFound()
            : Content(CriarHtml(quote, token), "text/html; charset=utf-8");
    }

    [HttpGet("pdf")]
    public async Task<IActionResult> Baixar(string token)
    {
        Response.Headers.CacheControl = "no-store, private";
        Response.Headers["X-Robots-Tag"] = "noindex, nofollow, noarchive";
        var pdf = await _orcamentos.BaixarPdfAsync(Request.Host.Host, token);
        if (pdf == null)
            return NotFound();

        return File(pdf, "application/pdf", $"Orcamento_OS_{token[..8]}.pdf");
    }

    private static string CriarHtml(OrcamentoPublico quote, string token)
    {
        var html = new StringBuilder();
        string E(string? value) => HtmlEncoder.Default.Encode(value ?? string.Empty);
        string Dinheiro(decimal value) =>
            "R$ " + value.ToString("N2", CultureInfo.GetCultureInfo("pt-BR"));
        void Campo(string rotulo, string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
                return;
            html.Append("<div class=\"meta-item\"><span class=\"label\">")
                .Append(E(rotulo)).Append("</span><span class=\"value\">")
                .Append(E(valor)).Append("</span></div>");
        }

        var logo = ObterLogoSeguro(quote.LogoDataUri);
        var enderecoOficina = string.Join(", ",
            new[] { quote.OficinaEndereco, quote.OficinaNumero }
                .Where(x => !string.IsNullOrWhiteSpace(x)));
        if (!string.IsNullOrWhiteSpace(quote.OficinaCep))
            enderecoOficina = string.IsNullOrWhiteSpace(enderecoOficina)
                ? $"CEP {quote.OficinaCep}"
                : $"{enderecoOficina} • CEP {quote.OficinaCep}";

        html.Append("<!doctype html><html lang=\"pt-BR\"><head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width, initial-scale=1\"><meta name=\"robots\" content=\"noindex,nofollow\"><title>Orçamento OS #")
            .Append(quote.OrdemServicoId).Append(" - ").Append(E(quote.Oficina))
            .Append("</title><style>")
            .Append("""
            :root{color-scheme:dark;--text:rgba(255,255,255,.93);--muted:rgba(255,255,255,.68);--line:rgba(255,255,255,.13);--green:#22c55e;--blue:#60a5fa;--purple:#a78bfa}
            *{box-sizing:border-box}body{margin:0;min-height:100vh;padding:38px 18px;color:var(--text);font:16px ui-sans-serif,system-ui,-apple-system,"Segoe UI",Roboto,Arial,sans-serif;overflow-x:hidden;background:radial-gradient(1200px 700px at 10% -10%,rgba(34,197,94,.22),transparent 60%),radial-gradient(1000px 650px at 110% 10%,rgba(96,165,250,.24),transparent 58%),radial-gradient(900px 700px at 45% 120%,rgba(167,139,250,.18),transparent 62%),linear-gradient(180deg,#050611,#0b1230)}
            body:before{content:"";position:fixed;inset:0;pointer-events:none;opacity:.12;background-image:url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='140' height='140'%3E%3Cfilter id='n'%3E%3CfeTurbulence type='fractalNoise' baseFrequency='.9' numOctaves='2' stitchTiles='stitch'/%3E%3C/filter%3E%3Crect width='140' height='140' filter='url(%23n)' opacity='.35'/%3E%3C/svg%3E")}
            main{position:relative;width:min(100%,900px);margin:0 auto}.brand{display:flex;align-items:center;gap:11px;margin:0 0 18px 4px;color:var(--muted);font-weight:900;letter-spacing:.02em}
            .brand-mark{display:grid;place-items:center;width:38px;height:38px;border:1px solid var(--line);border-radius:13px;background:linear-gradient(135deg,rgba(34,197,94,.23),rgba(96,165,250,.22),rgba(167,139,250,.18));color:#fff;font-size:13px}
            .brand small{display:block;margin-top:2px;color:rgba(255,255,255,.45);font-size:10px;letter-spacing:.16em}
            article{position:relative;overflow:hidden;border:1px solid var(--line);border-radius:28px;background:rgba(255,255,255,.055);box-shadow:0 28px 80px rgba(0,0,0,.48);backdrop-filter:blur(24px)}
            article:before{content:"";position:absolute;inset:-2px;pointer-events:none;background:radial-gradient(520px 220px at 18% 0%,rgba(96,165,250,.14),transparent 60%),radial-gradient(520px 220px at 82% 0%,rgba(34,197,94,.14),transparent 60%)}
            header,.content,footer{position:relative}header{padding:30px 34px 25px;border-bottom:1px solid var(--line);background:linear-gradient(115deg,rgba(255,255,255,.055),transparent 66%)}
            .office-head{display:flex;align-items:center;gap:18px}.office-logo{display:block;width:88px;height:88px;object-fit:contain;padding:8px;border:1px solid var(--line);border-radius:20px;background:rgba(255,255,255,.07)}
            .eyebrow{margin:0 0 8px;color:var(--green);font-size:12px;font-weight:900;letter-spacing:.13em;text-transform:uppercase}
            h1{margin:0;font-size:clamp(23px,4vw,32px);line-height:1.15;letter-spacing:-.035em}.shop-name{margin:8px 0 0;color:var(--muted);font-size:17px;font-weight:850}
            .company-info{display:flex;flex-wrap:wrap;gap:7px 18px;margin:20px 0 0;color:var(--muted);font-size:13px}.company-info span{overflow-wrap:anywhere}
            .content{padding:28px 34px 32px}.section-title{margin:24px 0 12px;color:var(--muted);font-size:12px;font-weight:950;letter-spacing:.1em;text-transform:uppercase}
            .meta{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:10px;margin-bottom:22px}.meta-item{min-width:0;padding:13px 15px;border:1px solid var(--line);border-radius:18px;background:rgba(255,255,255,.045)}
            .label{display:block;margin-bottom:6px;color:rgba(255,255,255,.48);font-size:11px;font-weight:850;text-transform:uppercase;letter-spacing:.09em}.value{font-weight:750;overflow-wrap:anywhere;white-space:pre-wrap}
            .panel{margin:14px 0;padding:16px 18px;border:1px solid var(--line);border-radius:18px;background:rgba(255,255,255,.04)}
            .panel-title{margin:0 0 8px;color:rgba(255,255,255,.5);font-size:11px;font-weight:900;letter-spacing:.09em;text-transform:uppercase}.panel-body{color:var(--text);line-height:1.6;white-space:pre-wrap;overflow-wrap:anywhere}
            .table-wrap{overflow-x:auto;border:1px solid var(--line);border-radius:18px;background:rgba(5,6,17,.25)}table{width:100%;border-collapse:collapse;min-width:560px}
            th,td{padding:13px 14px;border-bottom:1px solid rgba(255,255,255,.09);text-align:left}th{color:rgba(255,255,255,.52);font-size:11px;font-weight:900;text-transform:uppercase;letter-spacing:.06em}tbody tr:last-child td{border-bottom:0}
            .number{text-align:right;white-space:nowrap}.empty{padding:18px;color:var(--muted);text-align:center}
            .totals{max-width:380px;margin:20px 0 25px auto;padding:13px 16px;border:1px solid var(--line);border-radius:18px;background:rgba(255,255,255,.04)}
            .row{display:flex;justify-content:space-between;gap:16px;padding:7px 0;color:var(--muted)}.row span:last-child{color:var(--text);font-weight:750}
            .grand{margin-top:8px;padding-top:14px;border-top:1px solid var(--line);font-size:20px;font-weight:950}.grand span:last-child{background:linear-gradient(90deg,var(--green),var(--blue),var(--purple));-webkit-background-clip:text;background-clip:text;color:transparent}
            .signature{width:min(100%,390px);margin:38px auto 4px;padding-top:10px;border-top:1px solid rgba(255,255,255,.55);color:var(--muted);text-align:center;font-size:13px}
            .download{display:flex;align-items:center;justify-content:center;gap:10px;width:100%;min-height:56px;margin-top:24px;padding:14px 20px;border:1px solid rgba(255,255,255,.19);border-radius:18px;background:linear-gradient(135deg,rgba(34,197,94,.30),rgba(96,165,250,.22),rgba(167,139,250,.18));color:#fff;text-align:center;text-decoration:none;font-weight:950;box-shadow:0 12px 28px rgba(0,0,0,.24);transition:transform .22s cubic-bezier(.2,.85,.2,1),filter .22s}
            .download:hover{transform:translateY(-2px);filter:brightness(1.12)}.download:focus-visible{outline:3px solid rgba(96,165,250,.6);outline-offset:3px}
            footer{padding:17px 24px;border-top:1px solid var(--line);color:rgba(255,255,255,.48);font-size:13px;text-align:center}
            @media(max-width:560px){body{padding:20px 11px}header,.content{padding:22px 17px}footer{padding:15px 18px}.meta{grid-template-columns:1fr;gap:8px}.office-logo{width:68px;height:68px}.office-head{gap:13px}.totals{max-width:none}table{min-width:530px}}
            @media print{body{padding:0;background:#fff;color:#111}article{background:#fff;color:#111;box-shadow:none;border:1px solid #ccc}.download,.brand{display:none}header,.content,footer{color:#111}}
            """)
            .Append("</style></head><body><main><div class=\"brand\"><div class=\"brand-mark\">BB</div><div>BlakBox<small>TORQUE</small></div></div><article><header><div class=\"office-head\">");

        if (logo != null)
            html.Append("<img class=\"office-logo\" src=\"").Append(E(logo)).Append("\" alt=\"Logo ").Append(E(quote.Oficina)).Append("\">");

        html.Append("<div><p class=\"eyebrow\">Orçamento • Ordem de Serviço #")
            .Append(quote.OrdemServicoId).Append("</p><h1>Orçamento completo</h1><p class=\"shop-name\">")
            .Append(E(quote.Oficina)).Append("</p></div></div><div class=\"company-info\">");
        InfoEmpresa("CNPJ", quote.OficinaCnpj);
        InfoEmpresa("Telefone", quote.OficinaTelefone);
        InfoEmpresa("E-mail", quote.OficinaEmail);
        InfoEmpresa("Endereço", enderecoOficina);
        html.Append("</div></header><section class=\"content\"><h2 class=\"section-title\">Dados da ordem</h2><div class=\"meta\">");

        Campo("Ordem de serviço", $"#{quote.OrdemServicoId}");
        Campo("Abertura", quote.DataAbertura == default ? null : quote.DataAbertura.ToString("dd/MM/yyyy HH:mm"));
        Campo("Emissão do orçamento", quote.DataEmissao == default ? null : quote.DataEmissao.ToString("dd/MM/yyyy HH:mm"));
        Campo("Mecânico", quote.Mecanico);
        Campo("Status", quote.Status);
        Campo("Quilometragem", quote.Kilometragem?.ToString("N0", CultureInfo.GetCultureInfo("pt-BR")) is { } km ? $"{km} km" : null);

        html.Append("</div><h2 class=\"section-title\">Cliente e veículo</h2><div class=\"meta\">");
        Campo("Cliente", quote.Cliente);
        Campo("Telefone", quote.ClienteTelefone);
        Campo("E-mail", quote.ClienteEmail);
        Campo("Endereço", quote.ClienteEndereco);
        Campo("Veículo", quote.Veiculo);
        Campo("Placa", quote.Placa);
        Campo("Ano", quote.VeiculoAno?.ToString());
        Campo("Cor", quote.VeiculoCor);
        html.Append("</div>");

        Painel("Problema relatado", quote.Descricao);
        html.Append("<h2 class=\"section-title\">Peças e serviços</h2>");
        if (quote.Itens.Count == 0)
        {
            html.Append("<div class=\"panel empty\">Nenhuma peça ou serviço lançado.</div>");
        }
        else
        {
            html.Append("<div class=\"table-wrap\"><table><thead><tr><th>Descrição</th><th class=\"number\">Qtd.</th><th class=\"number\">Unitário</th><th class=\"number\">Total</th></tr></thead><tbody>");
            foreach (var item in quote.Itens)
            {
                html.Append("<tr><td>").Append(E(item.Descricao)).Append("</td><td class=\"number\">")
                    .Append(item.Quantidade).Append("</td><td class=\"number\">")
                    .Append(Dinheiro(item.ValorUnitario)).Append("</td><td class=\"number\">")
                    .Append(Dinheiro(item.Total)).Append("</td></tr>");
            }
            html.Append("</tbody></table></div>");
        }

        Painel("Checklist", quote.Checklist);
        Painel("Observações", quote.Observacoes);

        html.Append("<div class=\"totals\"><div class=\"row\"><span>Peças e serviços</span><span>")
            .Append(Dinheiro(quote.TotalPecas)).Append("</span></div><div class=\"row\"><span>Desconto</span><span>− ")
            .Append(Dinheiro(quote.Desconto)).Append("</span></div><div class=\"row grand\"><span>Total geral</span><span>")
            .Append(Dinheiro(quote.Total)).Append("</span></div></div><div class=\"signature\">Assinatura do cliente</div>")
            .Append("<a class=\"download\" download href=\"/orcamento/").Append(E(token))
            .Append("/pdf\"><span aria-hidden=\"true\">⬇</span> Baixar PDF</a></section><footer>Orçamento emitido por ")
            .Append(E(quote.Oficina)).Append(" • OS #").Append(quote.OrdemServicoId)
            .Append("</footer></article></main></body></html>");
        return html.ToString();

        void InfoEmpresa(string label, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
                html.Append("<span><strong>").Append(E(label)).Append(":</strong> ")
                    .Append(E(value)).Append("</span>");
        }

        void Painel(string titulo, string? valor)
        {
            if (!string.IsNullOrWhiteSpace(valor))
                html.Append("<section class=\"panel\"><h3 class=\"panel-title\">")
                    .Append(E(titulo)).Append("</h3><div class=\"panel-body\">")
                    .Append(E(valor)).Append("</div></section>");
        }
    }

    private static string? ObterLogoSeguro(string? dataUri)
    {
        if (string.IsNullOrWhiteSpace(dataUri) || dataUri.Length > 7_000_000)
            return null;

        var prefixo = dataUri.StartsWith("data:image/png;base64,", StringComparison.Ordinal)
            ? "data:image/png;base64,"
            : dataUri.StartsWith("data:image/jpeg;base64,", StringComparison.Ordinal)
                ? "data:image/jpeg;base64,"
                : dataUri.StartsWith("data:image/webp;base64,", StringComparison.Ordinal)
                    ? "data:image/webp;base64,"
                    : null;
        if (prefixo == null)
            return null;

        var payload = dataUri[prefixo.Length..];
        return payload.Length > 0 && payload.All(c =>
            char.IsAsciiLetterOrDigit(c) || c is '+' or '/' or '=')
            ? dataUri
            : null;
    }
}
