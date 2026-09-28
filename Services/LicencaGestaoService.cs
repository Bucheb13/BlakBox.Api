using Microsoft.EntityFrameworkCore;
using BlakBox.Api.Data;
using BlakBox.Api.Models;

namespace BlakBox.Api.Services;

public class LicencaGestaoService
{
    private readonly LicencaDbContext _context;

    public LicencaGestaoService(
        LicencaDbContext context)
    {
        _context = context;
    }

    public async Task<Licenca> CriarNovaAsync(int instalacaoId)
    {
        var instalacao = await _context.Instalacoes
            .Include(x => x.Cliente)
            .Include(x => x.Sistema)
            .Include(x => x.Licencas)
            .FirstOrDefaultAsync(x => x.InstalacaoId == instalacaoId);

        if (instalacao == null)
        {
            throw new InvalidOperationException("Instalação não encontrada.");
        }

        var ultimaLicenca = instalacao.Licencas
            .OrderByDescending(x => x.LicencaId)
            .FirstOrDefault();

        if (ultimaLicenca != null &&
            !string.Equals(ultimaLicenca.SituacaoComercial,
                Licenca.SituacaoCancelada, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "A instalação já possui uma licença não cancelada.");
        }

        var licenca = new Licenca
        {
            OficinaId = instalacao.InstalacaoKey,
            NomeOficina = string.IsNullOrWhiteSpace(instalacao.NomeInstalacao)
                ? instalacao.Cliente.Nome
                : instalacao.NomeInstalacao,
            InstalacaoId = instalacao.InstalacaoId,
            Ativa = false,
            SituacaoComercial = Licenca.SituacaoNormal,
            Mensagem = "Licença aguardando ativação."
        };

        _context.Licencas.Add(licenca);
        await _context.SaveChangesAsync();

        _context.LicencaHistoricos.Add(CriarHistorico(
            licenca,
            "Criação",
            "Administrador",
            "Nova licença criada pelo painel para a instalação."));
        await _context.SaveChangesAsync();

        return licenca;
    }

    public async Task AtivarAsync(
        Licenca licenca,
        string codigoPlano,
        string origem = "Administrador",
        string? observacao = null)
    {
        if (licenca.Ativa)
        {
            throw new InvalidOperationException(
                "A licença já está ativa.");
        }

        if (licenca.DataInicio.HasValue)
        {
            throw new InvalidOperationException(
                "Esta licença já possui uma data de início. " +
                "Para uma licença existente e inativa, utilize a reativação.");
        }

        if (!licenca.InstalacaoId.HasValue)
        {
            throw new InvalidOperationException(
                "A licença não está vinculada a uma instalação.");
        }

        if (string.IsNullOrWhiteSpace(codigoPlano))
        {
            throw new InvalidOperationException(
                "O plano é obrigatório.");
        }

        var instalacao =
            await ObterInstalacaoAsync(licenca);

        if (instalacao == null)
        {
            throw new InvalidOperationException(
                "A instalação vinculada à licença não foi encontrada.");
        }

        if (!instalacao.Ativa)
        {
            throw new InvalidOperationException(
                "A instalação está inativa.");
        }

        if (!instalacao.Sistema.Ativo)
        {
            throw new InvalidOperationException(
                "O sistema desta instalação está inativo.");
        }

        var plano =
            await ObterPlanoAsync(
                instalacao.SistemaId,
                codigoPlano);

        if (plano == null)
        {
            throw new InvalidOperationException(
                $"Plano \"{codigoPlano}\" não encontrado " +
                "para o sistema da instalação.");
        }

        if (!plano.Ativo)
        {
            throw new InvalidOperationException(
                $"O plano \"{plano.Nome}\" está inativo.");
        }

        var agora =
            RelogioSistema.Agora;

        var vencimento =
            CalcularVencimento(
                agora,
                plano);

        var validaAte =
            vencimento.AddDays(
                instalacao.Sistema.DiasTolerancia);

        var historico =
            CriarHistorico(
                licenca,
                "Ativação",
                origem,
                observacao);

        historico.Plano =
            plano.Nome;

        historico.DataInicioNova =
            agora;

        historico.DataVencimentoNovo =
            vencimento;

        historico.ValidaAteNova =
            validaAte;

        historico.AtivaNova =
            true;

        licenca.Ativa =
            true;

        licenca.PlanoId =
            plano.PlanoId;

        licenca.Plano =
            plano.Nome;

        licenca.SituacaoComercial =
            Licenca.SituacaoNormal;

        licenca.DataInicio =
            agora;

        licenca.DataVencimento =
            vencimento;

        licenca.ValidaAte =
            validaAte;

        licenca.UltimaValidacao =
            agora;

        licenca.Mensagem =
            null;

        _context.LicencaHistoricos.Add(
            historico);

        await _context.SaveChangesAsync();
    }

    public async Task RenovarAsync(
        Licenca licenca,
        string origem = "Administrador",
        string? observacao = null)
    {
        ValidarPodeRenovar(licenca);

        if (!licenca.PlanoId.HasValue)
        {
            throw new InvalidOperationException(
                "A licença não possui um plano vinculado.");
        }

        var instalacao =
            await ObterInstalacaoAsync(licenca);

        if (instalacao == null)
        {
            throw new InvalidOperationException(
                "A instalação vinculada à licença não foi encontrada.");
        }

        if (!instalacao.Ativa)
        {
            throw new InvalidOperationException(
                "A instalação está inativa.");
        }

        if (!instalacao.Sistema.Ativo)
        {
            throw new InvalidOperationException(
                "O sistema desta instalação está inativo.");
        }

        var plano =
            await _context.Planos
                .FirstOrDefaultAsync(
                    x =>
                        x.PlanoId ==
                        licenca.PlanoId.Value);

        if (plano == null)
        {
            throw new InvalidOperationException(
                "O plano da licença não foi encontrado.");
        }

        if (!plano.Ativo)
        {
            throw new InvalidOperationException(
                $"O plano \"{plano.Nome}\" está inativo.");
        }

        /*
         * Garante que o plano pertence ao mesmo sistema
         * da instalação.
         */
        if (plano.SistemaId !=
            instalacao.SistemaId)
        {
            throw new InvalidOperationException(
                "O plano da licença não pertence ao sistema da instalação.");
        }

        var agora =
            RelogioSistema.Agora;

        var inicioRenovacao =
            licenca.DataVencimento.HasValue &&
            licenca.DataVencimento.Value > agora
                ? licenca.DataVencimento.Value
                : agora;

        var novoVencimento =
            CalcularVencimento(
                inicioRenovacao,
                plano);

        var novaValidade =
            novoVencimento.AddDays(
                instalacao.Sistema.DiasTolerancia);

        var historico =
            CriarHistorico(
                licenca,
                "Renovação",
                origem,
                observacao);

        historico.Plano =
            plano.Nome;

        historico.DataVencimentoNovo =
            novoVencimento;

        historico.ValidaAteNova =
            novaValidade;

        historico.AtivaNova =
            true;

        licenca.Ativa =
            true;

        licenca.PlanoId =
            plano.PlanoId;

        licenca.Plano =
            plano.Nome;

        licenca.SituacaoComercial =
            Licenca.SituacaoNormal;

        licenca.DataVencimento =
            novoVencimento;

        licenca.ValidaAte =
            novaValidade;

        licenca.UltimaValidacao =
            agora;

        licenca.Mensagem =
            null;

        _context.LicencaHistoricos.Add(
            historico);

        await _context.SaveChangesAsync();
    }

    public async Task ReativarAsync(
        Licenca licenca,
        string origem = "Administrador",
        string? observacao = null)
    {
        if (licenca.Ativa)
        {
            throw new InvalidOperationException(
                "A licença já está ativa.");
        }

        if (!licenca.DataInicio.HasValue)
        {
            throw new InvalidOperationException(
                "A licença ainda não foi ativada.");
        }

        if (string.Equals(
            licenca.SituacaoComercial,
            Licenca.SituacaoCancelada,
            StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Uma licença cancelada não pode ser reativada.");
        }

        var instalacao =
            await ObterInstalacaoAsync(licenca);

        if (instalacao == null)
        {
            throw new InvalidOperationException(
                "A instalação vinculada à licença não foi encontrada.");
        }

        if (!instalacao.Ativa)
        {
            throw new InvalidOperationException(
                "A instalação está inativa.");
        }

        if (!instalacao.Sistema.Ativo)
        {
            throw new InvalidOperationException(
                "O sistema desta instalação está inativo.");
        }

        if (!licenca.PlanoId.HasValue)
        {
            throw new InvalidOperationException(
                "A licença não possui um plano vinculado.");
        }

        var plano =
            await _context.Planos
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x =>
                        x.PlanoId ==
                        licenca.PlanoId.Value);

        if (plano == null)
        {
            throw new InvalidOperationException(
                "O plano da licença não foi encontrado.");
        }

        if (!plano.Ativo)
        {
            throw new InvalidOperationException(
                $"O plano \"{plano.Nome}\" está inativo.");
        }

        if (plano.SistemaId !=
            instalacao.SistemaId)
        {
            throw new InvalidOperationException(
                "O plano da licença não pertence ao sistema da instalação.");
        }

        var agora =
            RelogioSistema.Agora;

        var historico =
            CriarHistorico(
                licenca,
                "Reativação",
                origem,
                observacao);

        historico.AtivaNova =
            true;

        /*
         * A reativação não muda:
         *
         * DataInicio
         * DataVencimento
         * ValidaAte
         * Plano
         *
         * Apenas devolve a licença ao estado ativo.
         */
        licenca.Ativa =
            true;

        licenca.SituacaoComercial =
            Licenca.SituacaoNormal;

        licenca.UltimaValidacao =
            agora;

        licenca.Mensagem =
            null;

        _context.LicencaHistoricos.Add(
            historico);

        await _context.SaveChangesAsync();
    }

    public async Task CancelarAsync(
        Licenca licenca,
        string origem = "Administrador",
        string? observacao = null)
    {
        if (!licenca.Ativa)
        {
            throw new InvalidOperationException(
                "A licença já está inativa.");
        }

        var historico =
            CriarHistorico(
                licenca,
                "Cancelamento",
                origem,
                observacao);

        historico.AtivaNova =
            false;

        licenca.Ativa =
            false;

        licenca.SituacaoComercial =
            Licenca.SituacaoCancelada;

        licenca.Mensagem =
            "Licença cancelada.";

        _context.LicencaHistoricos.Add(
            historico);

        await _context.SaveChangesAsync();
    }

    public async Task AlterarPlanoAsync(
        Licenca licenca,
        string novoCodigoPlano,
        string origem = "Administrador",
        string? observacao = null)
    {
        if (!licenca.InstalacaoId.HasValue)
        {
            throw new InvalidOperationException(
                "A licença não está vinculada a uma instalação.");
        }

        if (string.IsNullOrWhiteSpace(
            novoCodigoPlano))
        {
            throw new InvalidOperationException(
                "O plano é obrigatório.");
        }

        var instalacao =
            await ObterInstalacaoAsync(licenca);

        if (instalacao == null)
        {
            throw new InvalidOperationException(
                "A instalação vinculada à licença não foi encontrada.");
        }

        var novoPlano =
            await ObterPlanoAsync(
                instalacao.SistemaId,
                novoCodigoPlano);

        if (novoPlano == null)
        {
            throw new InvalidOperationException(
                $"Plano \"{novoCodigoPlano}\" não encontrado " +
                "para o sistema da instalação.");
        }

        if (!novoPlano.Ativo)
        {
            throw new InvalidOperationException(
                $"O plano \"{novoPlano.Nome}\" está inativo.");
        }

        if (licenca.PlanoId ==
            novoPlano.PlanoId)
        {
            return;
        }

        var historico =
            CriarHistorico(
                licenca,
                "Alteração de plano",
                origem,
                observacao);

        historico.Plano =
            novoPlano.Nome;

        historico.AtivaNova =
            licenca.Ativa;

        licenca.PlanoId =
            novoPlano.PlanoId;

        licenca.Plano =
            novoPlano.Nome;

        _context.LicencaHistoricos.Add(
            historico);

        await _context.SaveChangesAsync();
    }

    public async Task AlterarStatusAsync(
        Licenca licenca,
        bool novaAtiva,
        string origem = "Administrador",
        string? observacao = null)
    {
        if (licenca.Ativa ==
            novaAtiva)
        {
            return;
        }

        if (novaAtiva &&
            string.Equals(
                licenca.SituacaoComercial,
                Licenca.SituacaoCancelada,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Uma licença cancelada não pode ser ativada diretamente. " +
                "Use uma nova ativação comercial.");
        }

        var historico =
            CriarHistorico(
                licenca,
                novaAtiva
                    ? "Ativação"
                    : "Inativação",
                origem,
                observacao);

        historico.AtivaNova =
            novaAtiva;

        licenca.Ativa =
            novaAtiva;

        if (novaAtiva)
        {
            licenca.UltimaValidacao =
                RelogioSistema.Agora;

            licenca.Mensagem =
                null;

            if (string.Equals(
                licenca.SituacaoComercial,
                Licenca.SituacaoSuspensa,
                StringComparison.OrdinalIgnoreCase))
            {
                licenca.SituacaoComercial =
                    Licenca.SituacaoNormal;
            }
        }

        _context.LicencaHistoricos.Add(
            historico);

        await _context.SaveChangesAsync();
    }

    public async Task AlterarVencimentoAsync(
        Licenca licenca,
        DateTime novoVencimento,
        string origem = "Administrador",
        string? observacao = null)
    {
        if (!licenca.DataInicio.HasValue)
        {
            throw new InvalidOperationException(
                "A licença ainda não possui data de início.");
        }

        if (novoVencimento <=
            licenca.DataInicio.Value)
        {
            throw new InvalidOperationException(
                "A data de vencimento deve ser posterior à data de início.");
        }

        var instalacao =
            await ObterInstalacaoAsync(licenca);

        if (instalacao == null)
        {
            throw new InvalidOperationException(
                "A instalação vinculada à licença não foi encontrada.");
        }

        if (!instalacao.Sistema.Ativo)
        {
            throw new InvalidOperationException(
                "O sistema desta instalação está inativo.");
        }

        var novaValidade =
            novoVencimento.AddDays(
                instalacao.Sistema.DiasTolerancia);

        var historico =
            CriarHistorico(
                licenca,
                "Alteração de vencimento",
                origem,
                observacao);

        historico.DataVencimentoNovo =
            novoVencimento;

        historico.ValidaAteNova =
            novaValidade;

        historico.AtivaNova =
            licenca.Ativa;

        licenca.DataVencimento =
            novoVencimento;

        licenca.ValidaAte =
            novaValidade;

        _context.LicencaHistoricos.Add(
            historico);

        await _context.SaveChangesAsync();
    }

    private async Task<Instalacao?> ObterInstalacaoAsync(
        Licenca licenca)
    {
        if (!licenca.InstalacaoId.HasValue)
        {
            return null;
        }

        return await _context.Instalacoes
            .Include(x =>
                x.Sistema)
            .Include(x =>
                x.Cliente)
            .FirstOrDefaultAsync(
                x =>
                    x.InstalacaoId ==
                    licenca.InstalacaoId.Value);
    }

    private async Task<Plano?> ObterPlanoAsync(
        int sistemaId,
        string codigoPlano)
    {
        var codigo =
            codigoPlano.Trim();

        /*
         * Primeiro procura pelo código oficial.
         */
        var plano =
            await _context.Planos
                .FirstOrDefaultAsync(
                    x =>
                        x.SistemaId ==
                        sistemaId &&
                        x.Codigo == codigo);

        if (plano != null)
        {
            return plano;
        }

        /*
         * Compatibilidade com o painel/instalações
         * que ainda possam enviar "Mensal", etc.
         */
        var codigoNormalizado =
            NormalizarCodigoPlano(codigo);

        return await _context.Planos
            .FirstOrDefaultAsync(
                x =>
                    x.SistemaId ==
                    sistemaId &&
                    x.Codigo ==
                    codigoNormalizado);
    }

    private static DateTime CalcularVencimento(
        DateTime inicio,
        Plano plano)
    {
        if (plano.DuracaoMeses.HasValue &&
            plano.DuracaoMeses.Value > 0)
        {
            return inicio.AddMonths(
                plano.DuracaoMeses.Value);
        }

        if (plano.DuracaoDias.HasValue &&
            plano.DuracaoDias.Value > 0)
        {
            return inicio.AddDays(
                plano.DuracaoDias.Value);
        }

        throw new InvalidOperationException(
            $"O plano \"{plano.Nome}\" não possui uma duração válida.");
    }

    private static void ValidarPodeRenovar(
        Licenca licenca)
    {
        if (!licenca.DataInicio.HasValue)
        {
            throw new InvalidOperationException(
                "A licença ainda não foi ativada.");
        }

        if (string.Equals(
            licenca.SituacaoComercial,
            Licenca.SituacaoCancelada,
            StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Uma licença cancelada não pode ser renovada.");
        }

        var status =
            StatusLicencaService.ObterStatus(
                licenca);

        if (status !=
                StatusLicencaService.Ativa &&
            status !=
                StatusLicencaService.EmTolerancia &&
            status !=
                StatusLicencaService.Vencida)
        {
            throw new InvalidOperationException(
                $"Não é possível renovar uma licença " +
                $"com status \"{status}\".");
        }
    }

    private static string NormalizarCodigoPlano(
        string codigo)
    {
        return codigo
            .Trim()
            .ToLowerInvariant() switch
        {
            "mensal" =>
                "MENSAL",

            "trimestral" =>
                "TRIMESTRAL",

            "semestral" =>
                "SEMESTRAL",

            "anual" =>
                "ANUAL",

            _ =>
                codigo.Trim()
        };
    }

    private static LicencaHistorico CriarHistorico(
        Licenca licenca,
        string acao,
        string origem,
        string? observacao)
    {
        return new LicencaHistorico
        {
            LicencaId =
                licenca.LicencaId,

            Acao =
                acao,

            Plano =
                licenca.Plano,

            DataInicioAnterior =
                licenca.DataInicio,

            DataVencimentoAnterior =
                licenca.DataVencimento,

            ValidaAteAnterior =
                licenca.ValidaAte,

            AtivaAnterior =
                licenca.Ativa,

            Origem =
                origem,

            Observacao =
                observacao,

            CriadoEm =
                RelogioSistema.Agora
        };
    }
}
