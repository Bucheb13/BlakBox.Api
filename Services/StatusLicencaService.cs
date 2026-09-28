using BlakBox.Api.Models;

namespace BlakBox.Api.Services;

public static class StatusLicencaService
{
    public const string AguardandoAtivacao =
        "Aguardando ativação";

    public const string Ativa =
        "Ativa";

    public const string EmTolerancia =
        "Em tolerância";

    public const string Vencida =
        "Vencida";

    public const string Inativa =
        "Inativa";

    public static string ObterStatus(
        Licenca licenca,
        int? diasTolerancia = null)
    {
        var agora =
            RelogioSistema.Agora;

        /*
         * Uma licença cancelada continua sendo
         * operacionalmente inativa.
         */
        if (string.Equals(
            licenca.SituacaoComercial,
            Licenca.SituacaoCancelada,
            StringComparison.OrdinalIgnoreCase))
        {
            return Inativa;
        }

        if (!licenca.DataInicio.HasValue)
        {
            return AguardandoAtivacao;
        }

        if (!licenca.Ativa)
        {
            return Inativa;
        }

        if (!licenca.DataVencimento.HasValue ||
            agora <= licenca.DataVencimento.Value)
        {
            return Ativa;
        }

        var validaAte = ObterValidaAte(licenca, diasTolerancia);

        if (validaAte.HasValue &&
            agora <= validaAte.Value)
        {
            return EmTolerancia;
        }

        return Vencida;
    }

    public static bool PodeUsarSistema(
        Licenca licenca,
        int? diasTolerancia = null)
    {
        var status =
            ObterStatus(licenca, diasTolerancia);

        if (string.Equals(
            licenca.SituacaoComercial,
            Licenca.SituacaoCancelada,
            StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return status == Ativa ||
               status == EmTolerancia;
    }

    public static DateTime? ObterValidaAte(
        Licenca licenca,
        int? diasTolerancia = null)
    {
        var tolerancia = diasTolerancia
            ?? licenca.Instalacao?.Sistema?.DiasTolerancia;

        if (tolerancia.HasValue && licenca.DataVencimento.HasValue)
        {
            return licenca.DataVencimento.Value.AddDays(tolerancia.Value);
        }

        return licenca.ValidaAte;
    }

    public static bool PodeRenovar(
        Licenca licenca)
    {
        if (string.Equals(
            licenca.SituacaoComercial,
            Licenca.SituacaoCancelada,
            StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var status =
            ObterStatus(licenca);

        return status == Ativa ||
               status == EmTolerancia ||
               status == Vencida;
    }

    public static bool PodeAtivar(
        Licenca licenca)
    {
        if (string.Equals(
            licenca.SituacaoComercial,
            Licenca.SituacaoCancelada,
            StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var status =
            ObterStatus(licenca);

        return status == AguardandoAtivacao ||
               status == Inativa;
    }

    public static bool PodeReativar(
        Licenca licenca)
    {
        if (string.Equals(
            licenca.SituacaoComercial,
            Licenca.SituacaoCancelada,
            StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return !licenca.Ativa &&
               licenca.DataInicio.HasValue;
    }
}
