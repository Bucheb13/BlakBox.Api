using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using BlakBox.Api.Data;
using BlakBox.Api.Models;

namespace BlakBox.Api.Services;

public class CredencialInstalacaoService
{
    private const int TamanhoChaveBytes = 32;

    private readonly LicencaDbContext _context;
    private readonly IDataProtector _protector;

    public CredencialInstalacaoService(
        LicencaDbContext context,
        IDataProtectionProvider protectionProvider)
    {
        _context = context;
        _protector = protectionProvider.CreateProtector(
            "BlakBox.Api.InstallationCredentialRecovery.v1");
    }

    public async Task<CredencialCriadaResultado> CriarAsync(
        int instalacaoId)
    {
        await using var transacao =
            await _context.Database.BeginTransactionAsync();

        // Serializa rotações da mesma instalação entre processos/réplicas.
        await _context.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock({instalacaoId}::bigint)");

        var instalacao =
            await _context.Instalacoes
                .FirstOrDefaultAsync(
                    x =>
                        x.InstalacaoId ==
                        instalacaoId);

        if (instalacao == null)
        {
            throw new InvalidOperationException(
                "Instalação não encontrada.");
        }

        /*
         * Antes de criar uma nova credencial, guardamos os hashes
         * das credenciais ativas.
         *
         * Isso permite que uma nova chave possa ser associada ao
         * fluxo de recuperação da credencial anterior.
         */
        var credenciaisAtuais =
            await _context.CredenciaisInstalacao
                .Where(
                    x =>
                        x.InstalacaoId ==
                        instalacaoId &&
                        x.Ativa)
                .ToListAsync();

        foreach (var credencial in credenciaisAtuais)
        {
            credencial.Ativa =
                false;

            credencial.RevogadaEm =
                RelogioSistema.Agora;

            credencial.ChaveRecuperacaoProtegida = null;
            credencial.RecuperacaoExpiraEm = null;
        }

        /*
         * 32 bytes = 256 bits de aleatoriedade.
         */
        var chaveBytes =
            RandomNumberGenerator.GetBytes(
                TamanhoChaveBytes);

        var chave =
            Convert.ToBase64String(
                chaveBytes);

        var hash =
            CalcularHash(chave);

        var ultimosCaracteres =
            ObterUltimosCaracteres(chave);

        var credencialNova =
            new CredencialInstalacao
            {
                InstalacaoId =
                    instalacaoId,

                ChaveHash =
                    hash,

                UltimosCaracteres =
                    ultimosCaracteres,

                Ativa =
                    true,

                CriadoEm =
                    RelogioSistema.Agora
            };

        foreach (var credencialAnterior in credenciaisAtuais)
        {
            credencialAnterior.ChaveRecuperacaoProtegida =
                _protector.Protect(chave);
            credencialAnterior.RecuperacaoExpiraEm =
                RelogioSistema.Agora.AddMinutes(10);
        }

        _context.CredenciaisInstalacao.Add(
            credencialNova);

        await _context.SaveChangesAsync();
        await transacao.CommitAsync();

        /*
         * A chave original nunca é armazenada no banco.
         */
        return new CredencialCriadaResultado
        {
            CredencialId =
                credencialNova
                    .CredencialInstalacaoId,

            InstalacaoId =
                instalacaoId,

            Chave =
                chave,

            UltimosCaracteres =
                ultimosCaracteres
        };
    }

    public async Task<AutenticacaoInstalacaoResultado?>
        AutenticarAsync(
            string chave)
    {
        if (string.IsNullOrWhiteSpace(chave))
        {
            return null;
        }

        var hash =
            CalcularHash(chave);

        var credencial =
            await _context.CredenciaisInstalacao
                .Include(x =>
                    x.Instalacao)
                .ThenInclude(x =>
                    x.Sistema)
                .Include(x =>
                    x.Instalacao)
                .ThenInclude(x =>
                    x.Cliente)
                .FirstOrDefaultAsync(
                    x =>
                        x.ChaveHash == hash &&
                        x.Ativa &&
                        x.Instalacao.Ativa &&
                        x.Instalacao.Cliente.Ativo &&
                        x.Instalacao.Sistema.Ativo);

        if (credencial == null)
        {
            return null;
        }

        var agora =
            RelogioSistema.Agora;

        credencial.UltimaUtilizacao =
            agora;

        credencial.Instalacao
            .UltimaComunicacao =
            agora;

        await _context.SaveChangesAsync();

        return new AutenticacaoInstalacaoResultado
        {
            InstalacaoId =
                credencial.Instalacao
                    .InstalacaoId,

            ClienteId =
                credencial.Instalacao
                    .ClienteId,

            SistemaId =
                credencial.Instalacao
                    .SistemaId,

            SistemaCodigo =
                credencial.Instalacao
                    .Sistema.Codigo,

            SistemaNome =
                credencial.Instalacao
                    .Sistema.Nome,

            InstalacaoKey =
                credencial.Instalacao
                    .InstalacaoKey
        };
    }

    public async Task<CredencialInstalacao?>
        ObterAtivaAsync(
            int instalacaoId)
    {
        return await _context
            .CredenciaisInstalacao
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x =>
                    x.InstalacaoId ==
                    instalacaoId &&
                    x.Ativa);
    }

    public async Task<bool> ValidarChaveAtivaAsync(
        int instalacaoId,
        string chave)
    {
        if (string.IsNullOrWhiteSpace(chave))
        {
            return false;
        }

        var hash = CalcularHash(chave);
        return await _context.CredenciaisInstalacao
            .AsNoTracking()
            .AnyAsync(x =>
                x.InstalacaoId == instalacaoId &&
                x.ChaveHash == hash &&
                x.Ativa);
    }

    /*
     * ============================================================
     * RECUPERAÇÃO DE CREDENCIAL REVOGADA
     * ============================================================
     *
     * O App não pede uma nova credencial.
     *
     * A API já criou a nova credencial durante a operação anterior
     * e deixou a chave disponível temporariamente.
     */
    public async Task<CredencialCriadaResultado?> RecuperarOuCriarParaCredencialRevogadaAsync(
        CredencialInstalacao credencialRevogada)
    {
        if (string.IsNullOrWhiteSpace(credencialRevogada.ChaveRecuperacaoProtegida) ||
            !credencialRevogada.RecuperacaoExpiraEm.HasValue ||
            credencialRevogada.RecuperacaoExpiraEm.Value <= RelogioSistema.Agora)
        {
            return null;
        }

        try
        {
            var chave = _protector.Unprotect(credencialRevogada.ChaveRecuperacaoProtegida);
            var hash = CalcularHash(chave);
            var credencialAtiva = await _context.CredenciaisInstalacao
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.InstalacaoId == credencialRevogada.InstalacaoId &&
                    x.ChaveHash == hash &&
                    x.Ativa);

            if (credencialAtiva == null)
            {
                return null;
            }

            return new CredencialCriadaResultado
            {
                CredencialId = credencialAtiva.CredencialInstalacaoId,
                InstalacaoId = credencialRevogada.InstalacaoId,
                Chave = chave,
                UltimosCaracteres = ObterUltimosCaracteres(chave)
            };
        }
        catch (CryptographicException)
        {
            return null;
        }
    }
    private static string CalcularHash(
        string chave)
    {
        var bytes =
            Encoding.UTF8.GetBytes(
                chave);

        var hash =
            SHA512.HashData(bytes);

        return Convert.ToHexString(
            hash);
    }

    private static string ObterUltimosCaracteres(
        string chave)
    {
        if (chave.Length <= 8)
        {
            return chave;
        }

        return chave[^8..];
    }
}

public class CredencialCriadaResultado
{
    public int CredencialId { get; set; }

    public int InstalacaoId { get; set; }

    public string Chave { get; set; } = null!;

    public string UltimosCaracteres { get; set; } = null!;
}

public class AutenticacaoInstalacaoResultado
{
    public int InstalacaoId { get; set; }

    public int ClienteId { get; set; }

    public int SistemaId { get; set; }

    public string SistemaCodigo { get; set; } = null!;

    public string SistemaNome { get; set; } = null!;

    public string InstalacaoKey { get; set; } = null!;
}
