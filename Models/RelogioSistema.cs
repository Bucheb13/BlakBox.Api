namespace BlakBox.Api.Models;

/// <summary>
/// Fornece horário de negócio de São Paulo em formato local sem offset,
/// compatível com as colunas PostgreSQL timestamp without time zone.
/// </summary>
public static class RelogioSistema
{
    private static readonly TimeZoneInfo Fuso = ObterFuso();

    public static DateTime Agora =>
        DateTime.SpecifyKind(
            TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Fuso),
            DateTimeKind.Unspecified);

    public static DateTime ParaUtcSemFuso(DateTime horarioLocal)
    {
        var local = DateTime.SpecifyKind(horarioLocal, DateTimeKind.Unspecified);
        return DateTime.SpecifyKind(
            TimeZoneInfo.ConvertTimeToUtc(local, Fuso),
            DateTimeKind.Unspecified);
    }

    private static TimeZoneInfo ObterFuso()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time");
        }
    }
}
