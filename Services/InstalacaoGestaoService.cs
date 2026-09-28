using Microsoft.EntityFrameworkCore;
using BlakBox.Api.Data;
using BlakBox.Api.Models;

namespace BlakBox.Api.Services;

public class InstalacaoGestaoService
{
    private readonly LicencaDbContext _context;

    public InstalacaoGestaoService(
        LicencaDbContext context)
    {
        _context = context;
    }

    public async Task<List<Instalacao>> ListarAsync()
    {
        return await _context.Instalacoes
            .AsNoTracking()
            .Include(x => x.Cliente)
            .Include(x => x.Sistema)
            .Include(x => x.Licencas)
            .Include(x => x.Credenciais)
            .OrderBy(x => x.NomeInstalacao)
            .ThenBy(x => x.InstalacaoKey)
            .ToListAsync();
    }

    public async Task<Instalacao?> ObterAsync(
        int instalacaoId)
    {
        return await _context.Instalacoes
            .Include(x => x.Cliente)
            .Include(x => x.Sistema)
            .Include(x => x.Licencas)
            .Include(x => x.Credenciais)
            .FirstOrDefaultAsync(
                x =>
                    x.InstalacaoId ==
                    instalacaoId);
    }

    /*
     * ============================================================
     * CRIAR INSTALAÇÃO
     * ============================================================
     */
    public async Task<Instalacao> CriarAsync(
        int clienteId,
        int sistemaId,
        string instalacaoKey,
        string? nomeInstalacao)
    {
        instalacaoKey =
            NormalizarInstalacaoKey(
                instalacaoKey);

        nomeInstalacao =
            NormalizarCampo(
                nomeInstalacao);

        await ValidarClienteAsync(
            clienteId);

        await ValidarSistemaAsync(
            sistemaId);

        var existe =
            await _context.Instalacoes
                .AnyAsync(
                    x =>
                        x.SistemaId ==
                            sistemaId
                        &&
                        x.InstalacaoKey ==
                            instalacaoKey);

        if (existe)
        {
            throw new InvalidOperationException(
                "Já existe uma instalação com esta chave para o sistema selecionado.");
        }

        var instalacao =
            new Instalacao
            {
                ClienteId =
                    clienteId,

                SistemaId =
                    sistemaId,

                InstalacaoKey =
                    instalacaoKey,

                NomeInstalacao =
                    nomeInstalacao,

                Ativa =
                    true,

                CriadoEm =
                    RelogioSistema.Agora,

                UltimaComunicacao =
                    null
            };

        _context.Instalacoes.Add(
            instalacao);

        await _context.SaveChangesAsync();

        return instalacao;
    }

    /*
     * ============================================================
     * SINCRONIZAR INSTALAÇÃO
     * ============================================================
     *
     * Utilizado pelo OficinaWeb.
     *
     * A identidade:
     *
     *   SistemaId + InstalacaoKey
     *
     * permanece imutável.
     *
     * O NomeInstalacao pode ser atualizado porque ele vem
     * diretamente do OficinaWeb.
     */
    public async Task<Instalacao> SincronizarAsync(
        int instalacaoId,
        string nomeInstalacao)
    {
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

        var nomeNormalizado =
            NormalizarCampo(
                nomeInstalacao);

        if (string.IsNullOrWhiteSpace(
                nomeNormalizado))
        {
            throw new InvalidOperationException(
                "O nome da instalação é obrigatório.");
        }

        if (!string.Equals(
                instalacao.NomeInstalacao,
                nomeNormalizado,
                StringComparison.Ordinal))
        {
            instalacao.NomeInstalacao =
                nomeNormalizado;

            await _context.SaveChangesAsync();
        }

        return instalacao;
    }

    /*
     * ============================================================
     * ATUALIZAÇÃO ADMINISTRATIVA
     * ============================================================
     *
     * Mantida para utilização interna do painel da Central.
     *
     * A identidade da instalação continua protegida quando já
     * existe licença.
     */
    public async Task AtualizarAsync(
        int instalacaoId,
        int sistemaId,
        string instalacaoKey,
        string? nomeInstalacao)
    {
        var instalacao =
            await ObterAsync(
                instalacaoId);

        if (instalacao == null)
        {
            throw new InvalidOperationException(
                "Instalação não encontrada.");
        }

        instalacaoKey =
            NormalizarInstalacaoKey(
                instalacaoKey);

        nomeInstalacao =
            NormalizarCampo(
                nomeInstalacao);

        await ValidarSistemaAsync(
            sistemaId);

        var existe =
            await _context.Instalacoes
                .AnyAsync(
                    x =>
                        x.InstalacaoId !=
                            instalacaoId
                        &&
                        x.SistemaId ==
                            sistemaId
                        &&
                        x.InstalacaoKey ==
                            instalacaoKey);

        if (existe)
        {
            throw new InvalidOperationException(
                "Já existe outra instalação com esta chave para o sistema selecionado.");
        }

        /*
         * Sistema e InstalacaoKey formam a identidade
         * permanente da instalação depois que ela possui licença.
         */
        if (instalacao.Licencas.Any())
        {
            if (instalacao.SistemaId !=
                sistemaId)
            {
                throw new InvalidOperationException(
                    "Não é permitido trocar o sistema de uma instalação que já possui licença.");
            }

            if (!string.Equals(
                    instalacao.InstalacaoKey,
                    instalacaoKey,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Não é permitido alterar a chave de uma instalação que já possui licença.");
            }
        }

        instalacao.SistemaId =
            sistemaId;

        instalacao.InstalacaoKey =
            instalacaoKey;

        instalacao.NomeInstalacao =
            nomeInstalacao;

        await _context.SaveChangesAsync();
    }

    /*
     * ============================================================
     * ALTERAR STATUS
     * ============================================================
     */
    public async Task AlterarStatusAsync(
        int instalacaoId,
        bool ativa)
    {
        var instalacao =
            await ObterAsync(
                instalacaoId);

        if (instalacao == null)
        {
            throw new InvalidOperationException(
                "Instalação não encontrada.");
        }

        if (instalacao.Ativa ==
            ativa)
        {
            return;
        }

        instalacao.Ativa =
            ativa;

        await _context.SaveChangesAsync();
    }

    /*
     * ============================================================
     * VALIDA CLIENTE
     * ============================================================
     */
    private async Task ValidarClienteAsync(
        int clienteId)
    {
        var cliente =
            await _context.Clientes
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x =>
                        x.ClienteId ==
                        clienteId);

        if (cliente == null)
        {
            throw new InvalidOperationException(
                "Cliente não encontrado.");
        }

        if (!cliente.Ativo)
        {
            throw new InvalidOperationException(
                "O cliente selecionado está inativo.");
        }
    }

    /*
     * ============================================================
     * VALIDA SISTEMA
     * ============================================================
     */
    private async Task ValidarSistemaAsync(
        int sistemaId)
    {
        var sistema =
            await _context.Sistemas
                .AsNoTracking()
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
    }

    /*
     * ============================================================
     * NORMALIZA OFICINA ID
     * ============================================================
     */
    private static string NormalizarInstalacaoKey(
        string instalacaoKey)
    {
        if (string.IsNullOrWhiteSpace(
                instalacaoKey))
        {
            throw new InvalidOperationException(
                "A chave da instalação é obrigatória.");
        }

        instalacaoKey =
            instalacaoKey.Trim();

        if (instalacaoKey.Length > 50)
        {
            throw new InvalidOperationException(
                "A chave da instalação deve ter no máximo 50 caracteres.");
        }

        return instalacaoKey;
    }

    /*
     * ============================================================
     * NORMALIZA CAMPOS
     * ============================================================
     */
    private static string? NormalizarCampo(
        string? valor)
    {
        if (string.IsNullOrWhiteSpace(
                valor))
        {
            return null;
        }

        valor =
            valor.Trim();

        return valor.Length == 0
            ? null
            : valor;
    }
}
