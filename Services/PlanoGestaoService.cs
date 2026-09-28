using Microsoft.EntityFrameworkCore;
using BlakBox.Api.Data;
using BlakBox.Api.Models;

namespace BlakBox.Api.Services;

public class PlanoGestaoService
{
    private readonly LicencaDbContext _context;

    public PlanoGestaoService(
        LicencaDbContext context)
    {
        _context = context;
    }

    public async Task<List<Plano>> ListarAsync()
    {
        return await _context.Planos
            .AsNoTracking()
            .Include(x => x.Sistema)
            .Include(x => x.Licencas)
            .OrderBy(x => x.Sistema.Nome)
            .ThenBy(x => x.Nome)
            .ToListAsync();
    }

    public async Task<Plano?> ObterAsync(
        int planoId)
    {
        return await _context.Planos
            .Include(x => x.Sistema)
            .Include(x => x.Licencas)
            .FirstOrDefaultAsync(
                x => x.PlanoId == planoId);
    }

    public async Task<Plano> CriarAsync(
        int sistemaId,
        string codigo,
        string nome,
        int? duracaoDias,
        int? duracaoMeses,
        decimal? valor)
    {
        codigo =
            NormalizarCodigo(codigo);

        nome =
            NormalizarNome(nome);

        ValidarDuracao(
            duracaoDias,
            duracaoMeses);

        ValidarValor(valor);

        var sistema =
            await _context.Sistemas
                .FirstOrDefaultAsync(
                    x =>
                        x.SistemaId ==
                        sistemaId);

        if (sistema == null)
        {
            throw new InvalidOperationException(
                "Sistema não encontrado.");
        }

        if (!sistema.Ativo)
        {
            throw new InvalidOperationException(
                "O sistema selecionado está inativo.");
        }

        var existe =
            await _context.Planos
                .AnyAsync(
                    x =>
                        x.SistemaId ==
                        sistemaId &&
                        x.Codigo ==
                        codigo);

        if (existe)
        {
            throw new InvalidOperationException(
                $"Já existe um plano com o código \"{codigo}\" neste sistema.");
        }

        var plano =
            new Plano
            {
                SistemaId =
                    sistemaId,

                Codigo =
                    codigo,

                Nome =
                    nome,

                DuracaoDias =
                    duracaoDias,

                DuracaoMeses =
                    duracaoMeses,

                Ativo =
                    true,

                Valor =
                    valor,

                CriadoEm =
                    RelogioSistema.Agora
            };

        _context.Planos.Add(
            plano);

        await _context.SaveChangesAsync();

        return plano;
    }

    public async Task AtualizarAsync(
        int planoId,
        int sistemaId,
        string codigo,
        string nome,
        int? duracaoDias,
        int? duracaoMeses,
        decimal? valor)
    {
        var plano =
            await ObterAsync(planoId);

        if (plano == null)
        {
            throw new InvalidOperationException(
                "Plano não encontrado.");
        }

        codigo =
            NormalizarCodigo(codigo);

        nome =
            NormalizarNome(nome);

        ValidarDuracao(
            duracaoDias,
            duracaoMeses);

        ValidarValor(valor);

        var sistema =
            await _context.Sistemas
                .FirstOrDefaultAsync(
                    x =>
                        x.SistemaId ==
                        sistemaId);

        if (sistema == null)
        {
            throw new InvalidOperationException(
                "Sistema não encontrado.");
        }

        if (!sistema.Ativo)
        {
            throw new InvalidOperationException(
                "O sistema selecionado está inativo.");
        }

        var duplicado =
            await _context.Planos
                .AnyAsync(
                    x =>
                        x.PlanoId != planoId &&
                        x.SistemaId ==
                        sistemaId &&
                        x.Codigo ==
                        codigo);

        if (duplicado)
        {
            throw new InvalidOperationException(
                $"Já existe outro plano com o código \"{codigo}\" neste sistema.");
        }

        /*
         * Não permitimos trocar o sistema de um plano
         * que já está sendo utilizado por licenças.
         */
        if (plano.Licencas.Any() &&
            plano.SistemaId != sistemaId)
        {
            throw new InvalidOperationException(
                "Não é permitido trocar o sistema de um plano que já possui licenças.");
        }

        plano.SistemaId =
            sistemaId;

        plano.Codigo =
            codigo;

        plano.Nome =
            nome;

        plano.DuracaoDias =
            duracaoDias;

        plano.DuracaoMeses =
            duracaoMeses;

        plano.Valor =
            valor;

        await _context.SaveChangesAsync();
    }

    public async Task AlterarStatusAsync(
        int planoId,
        bool ativo)
    {
        var plano =
            await ObterAsync(planoId);

        if (plano == null)
        {
            throw new InvalidOperationException(
                "Plano não encontrado.");
        }

        if (plano.Ativo == ativo)
        {
            return;
        }

        /*
         * Inativar o plano não altera licenças existentes.
         *
         * Apenas impede novas ativações ou alterações
         * para esse plano.
         */
        plano.Ativo =
            ativo;

        await _context.SaveChangesAsync();
    }

    private static string NormalizarCodigo(
        string codigo)
    {
        if (string.IsNullOrWhiteSpace(codigo))
        {
            throw new InvalidOperationException(
                "O código do plano é obrigatório.");
        }

        codigo =
            codigo.Trim()
                .ToUpperInvariant();

        if (codigo.Length > 50)
        {
            throw new InvalidOperationException(
                "O código do plano deve ter no máximo 50 caracteres.");
        }

        return codigo;
    }

    private static string NormalizarNome(
        string nome)
    {
        if (string.IsNullOrWhiteSpace(nome))
        {
            throw new InvalidOperationException(
                "O nome do plano é obrigatório.");
        }

        nome =
            nome.Trim();

        if (nome.Length > 100)
        {
            throw new InvalidOperationException(
                "O nome do plano deve ter no máximo 100 caracteres.");
        }

        return nome;
    }

    private static void ValidarDuracao(
        int? duracaoDias,
        int? duracaoMeses)
    {
        var possuiDias =
            duracaoDias.HasValue &&
            duracaoDias.Value > 0;

        var possuiMeses =
            duracaoMeses.HasValue &&
            duracaoMeses.Value > 0;

        if (possuiDias == possuiMeses)
        {
            throw new InvalidOperationException(
                "O plano deve possuir duração em dias ou em meses, mas não ambos.");
        }

        if (duracaoDias.HasValue &&
            duracaoDias.Value > 36500)
        {
            throw new InvalidOperationException(
                "A duração em dias é muito grande.");
        }

        if (duracaoMeses.HasValue &&
            duracaoMeses.Value > 120)
        {
            throw new InvalidOperationException(
                "A duração em meses não pode ultrapassar 120 meses.");
        }
    }

    private static void ValidarValor(
        decimal? valor)
    {
        if (!valor.HasValue)
        {
            return;
        }

        if (valor.Value < 0)
        {
            throw new InvalidOperationException(
                "O valor do plano não pode ser negativo.");
        }

        if (valor.Value > 999999999999.99m)
        {
            throw new InvalidOperationException(
                "O valor do plano é muito alto.");
        }
    }
}
