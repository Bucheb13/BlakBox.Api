using Microsoft.EntityFrameworkCore;
using BlakBox.Api.Data;
using BlakBox.Api.Models;

namespace BlakBox.Api.Services;

public class SistemaGestaoService
{
    private readonly LicencaDbContext _context;

    public SistemaGestaoService(
        LicencaDbContext context)
    {
        _context = context;
    }

    public async Task<List<Sistema>> ListarAsync()
    {
        return await _context.Sistemas
            .AsNoTracking()
            .AsSplitQuery()
            .Include(x => x.Planos)
            .Include(x => x.Instalacoes)
            .OrderBy(x => x.Nome)
            .ToListAsync();
    }

    public async Task<Sistema?> ObterAsync(
        int sistemaId)
    {
        return await _context.Sistemas
            .FirstOrDefaultAsync(
                x => x.SistemaId == sistemaId);
    }

    public async Task<Sistema> CriarAsync(
        string codigo,
        string nome,
        int diasTolerancia)
    {
        codigo = NormalizarCodigo(codigo);
        nome = NormalizarNome(nome);

        ValidarDados(
            codigo,
            nome,
            diasTolerancia);

        var codigoExiste =
            await _context.Sistemas
                .AnyAsync(
                    x => x.Codigo == codigo);

        if (codigoExiste)
        {
            throw new InvalidOperationException(
                $"Já existe um sistema com o código \"{codigo}\".");
        }

        var sistema =
            new Sistema
            {
                Codigo = codigo,
                Nome = nome,
                Ativo = true,
                DiasTolerancia = diasTolerancia,
                CriadoEm = RelogioSistema.Agora
            };

        _context.Sistemas.Add(
            sistema);

        await _context.SaveChangesAsync();

        return sistema;
    }

    public async Task AtualizarAsync(
        int sistemaId,
        string codigo,
        string nome,
        int diasTolerancia)
    {
        var sistema =
            await ObterAsync(sistemaId);

        if (sistema == null)
        {
            throw new InvalidOperationException(
                "Sistema não encontrado.");
        }

        codigo = NormalizarCodigo(codigo);
        nome = NormalizarNome(nome);

        ValidarDados(
            codigo,
            nome,
            diasTolerancia);

        var codigoExiste =
            await _context.Sistemas
                .AnyAsync(
                    x =>
                        x.Codigo == codigo &&
                        x.SistemaId != sistemaId);

        if (codigoExiste)
        {
            throw new InvalidOperationException(
                $"Já existe outro sistema com o código \"{codigo}\".");
        }

        sistema.Codigo =
            codigo;

        sistema.Nome =
            nome;

        sistema.DiasTolerancia =
            diasTolerancia;

        await _context.SaveChangesAsync();
    }

    public async Task AlterarStatusAsync(
        int sistemaId,
        bool ativo)
    {
        var sistema =
            await ObterAsync(sistemaId);

        if (sistema == null)
        {
            throw new InvalidOperationException(
                "Sistema não encontrado.");
        }

        if (sistema.Ativo == ativo)
        {
            return;
        }

        if (!ativo)
        {
            var licencasAtivasDasInstalacoes = await _context.Licencas
                .Include(x => x.Instalacao)
                    .ThenInclude(x => x!.Sistema)
                .Where(x => x.Instalacao != null &&
                    x.Instalacao.SistemaId == sistemaId && x.Instalacao.Ativa)
                .ToListAsync();

            var possuiAssinaturaUtilizavel = licencasAtivasDasInstalacoes.Any(x =>
                !string.Equals(x.SituacaoComercial,
                    Licenca.SituacaoCancelada, StringComparison.OrdinalIgnoreCase) &&
                StatusLicencaService.PodeUsarSistema(
                    x, sistema.DiasTolerancia));

            if (possuiAssinaturaUtilizavel)
            {
                throw new InvalidOperationException(
                    "Não é possível inativar este sistema enquanto houver uma instalação com licença válida.");
            }
        }

        sistema.Ativo =
            ativo;

        await _context.SaveChangesAsync();
    }

    private static string NormalizarCodigo(
        string codigo)
    {
        if (string.IsNullOrWhiteSpace(codigo))
        {
            throw new InvalidOperationException(
                "O código do sistema é obrigatório.");
        }

        return codigo
            .Trim()
            .ToUpperInvariant();
    }

    private static string NormalizarNome(
        string nome)
    {
        if (string.IsNullOrWhiteSpace(nome))
        {
            throw new InvalidOperationException(
                "O nome do sistema é obrigatório.");
        }

        return nome.Trim();
    }

    private static void ValidarDados(
        string codigo,
        string nome,
        int diasTolerancia)
    {
        if (codigo.Length > 50)
        {
            throw new InvalidOperationException(
                "O código do sistema deve ter no máximo 50 caracteres.");
        }

        if (nome.Length > 150)
        {
            throw new InvalidOperationException(
                "O nome do sistema deve ter no máximo 150 caracteres.");
        }

        if (diasTolerancia < 0)
        {
            throw new InvalidOperationException(
                "Os dias de tolerância não podem ser negativos.");
        }

        if (diasTolerancia > 3650)
        {
            throw new InvalidOperationException(
                "Os dias de tolerância não podem ultrapassar 3650 dias.");
        }

    }
}

