using Microsoft.EntityFrameworkCore;
using BlakBox.Api.Data;
using BlakBox.Api.Models;

namespace BlakBox.Api.Services;

public class LicencaService
{
    private readonly LicencaDbContext _context;

    public LicencaService(LicencaDbContext context)
    {
        _context = context;
    }

    public async Task<List<Licenca>> ListarAsync()
    {
        return await _context.Licencas
            .OrderBy(x => x.NomeOficina)
            .ToListAsync();
    }

    public async Task<Licenca?> ObterPorOficinaIdAsync(string oficinaId)
    {
        return await _context.Licencas
            .FirstOrDefaultAsync(x => x.OficinaId == oficinaId);
    }

    public async Task<Licenca> CriarAsync(Licenca licenca)
    {
        licenca.DataInicio ??= RelogioSistema.Agora;

        if (licenca.DataVencimento == null)
        {
            licenca.DataVencimento = licenca.DataInicio.Value.AddMonths(1);
        }

        licenca.Ativa = true;

        licenca.UltimaValidacao = RelogioSistema.Agora;

        if (licenca.InstalacaoId.HasValue)
        {
            var instalacao = await _context.Instalacoes
                .Include(x => x.Sistema)
                .FirstOrDefaultAsync(
                    x => x.InstalacaoId == licenca.InstalacaoId.Value);

            if (instalacao?.Sistema != null && licenca.DataVencimento.HasValue)
            {
                licenca.ValidaAte = licenca.DataVencimento.Value
                    .AddDays(instalacao.Sistema.DiasTolerancia);
            }
        }

        if (string.IsNullOrWhiteSpace(licenca.Mensagem))
        {
            licenca.Mensagem = "Licença ativa.";
        }

        _context.Licencas.Add(licenca);

        await _context.SaveChangesAsync();

        return licenca;
    }

    public async Task SalvarAsync(Licenca licenca)
    {
        _context.Licencas.Update(licenca);

        await _context.SaveChangesAsync();
    }
}
