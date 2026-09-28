using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BlakBox.Api.Data;

namespace BlakBox.Api.Controllers;

[ApiController]
[Route("api/sistemas")]
public class SistemasController : ControllerBase
{
    private readonly LicencaDbContext _context;

    public SistemasController(
        LicencaDbContext context)
    {
        _context = context;
    }

    /*
     * ============================================================
     * LISTAR SISTEMAS ATIVOS
     * ============================================================
     *
     * Utilizado pelo OficinaWeb durante a configuração inicial.
     */
    [HttpGet]
    public async Task<IActionResult> ListarAtivos()
    {
        var sistemas =
            await _context.Sistemas
                .AsNoTracking()
                .Where(
                    x =>
                        x.Ativo)

                .OrderBy(
                    x =>
                        x.Nome)

                .Select(
                    x => new
                    {
                        sistemaId =
                            x.SistemaId,

                        codigo =
                            x.Codigo,

                        nome =
                            x.Nome
                    })

                .ToListAsync();

        return Ok(sistemas);
    }
}
