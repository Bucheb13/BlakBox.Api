using System.Security.Cryptography;
using System.Text;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using BlakBox.Api.Data;
using BlakBox.Api.Models;
using BlakBox.Api.Models.Api;
using BlakBox.Api.Services;

namespace BlakBox.Api.Controllers;

[ApiController]
[Route("api/licenca")]
public class LicencaController : ControllerBase
{
    private readonly LicencaDbContext _context;
    private readonly InstalacaoGestaoService _instalacaoService;
    private readonly CredencialInstalacaoService _credencialService;
    private readonly IConfiguration _configuration;

    public LicencaController(
        LicencaDbContext context,
        InstalacaoGestaoService instalacaoService,
        CredencialInstalacaoService credencialService,
        IConfiguration configuration)
    {
        _context = context;
        _instalacaoService = instalacaoService;
        _credencialService = credencialService;
        _configuration = configuration;
    }

    /*
     * ============================================================
     * REGISTRO / SINCRONIZAÇÃO DA INSTALAÇÃO
     * ============================================================
     */
    [AllowAnonymous]
    [EnableRateLimiting("registration")]
    [RequestSizeLimit(32_768)]
    [HttpPost("registrar")]
    public async Task<IActionResult> Registrar(
        [FromBody] RegistrarLicencaRequest request)
    {
        if (request == null)
        {
            return BadRequest(new
            {
                sucesso = false,
                codigo = "REQUISICAO_INVALIDA",
                mensagem = "A requisição é obrigatória."
            });
        }

        if (string.IsNullOrWhiteSpace(request.OficinaId))
        {
            return BadRequest(new
            {
                sucesso = false,
                codigo = "OFICINA_ID_OBRIGATORIO",
                mensagem = "OficinaId é obrigatório."
            });
        }

        if (string.IsNullOrWhiteSpace(request.NomeOficina))
        {
            return BadRequest(new
            {
                sucesso = false,
                codigo = "NOME_OFICINA_OBRIGATORIO",
                mensagem = "NomeOficina é obrigatório."
            });
        }

        if (string.IsNullOrWhiteSpace(request.SistemaCodigo))
        {
            return BadRequest(new
            {
                sucesso = false,
                codigo = "SISTEMA_CODIGO_OBRIGATORIO",
                mensagem = "SistemaCodigo é obrigatório."
            });
        }

        var oficinaId =
            request.OficinaId.Trim();

        var nomeOficina =
            request.NomeOficina.Trim();

        var sistemaCodigo =
            request.SistemaCodigo.Trim();

        /*
         * --------------------------------------------------------
         * 1. LOCALIZA O SISTEMA
         * --------------------------------------------------------
         */
        var sistema =
            await _context.Sistemas
                .FirstOrDefaultAsync(
                    x =>
                        x.Codigo == sistemaCodigo);

        if (sistema == null)
        {
            return NotFound(new
            {
                sucesso = false,
                codigo = "SISTEMA_NAO_ENCONTRADO",
                mensagem =
                    $"O sistema '{sistemaCodigo}' não está cadastrado na Central."
            });
        }

        if (!sistema.Ativo)
        {
            return Conflict(new
            {
                sucesso = false,
                codigo = "SISTEMA_INATIVO",
                mensagem =
                    $"O sistema '{sistemaCodigo}' está inativo."
            });
        }

        /*
         * --------------------------------------------------------
         * 2. NORMALIZA OS DADOS DA OFICINA
         * --------------------------------------------------------
         */
        var documento =
            NormalizarCampo(request.Cnpj);

        var email =
            NormalizarCampo(request.Email);

        var telefone =
            NormalizarCampo(request.Telefone);

        var endereco =
            NormalizarCampo(request.Endereco);

        var numero =
            NormalizarCampo(request.Numero);

        /*
         * --------------------------------------------------------
         * 3. PROCURA A INSTALAÇÃO
         * --------------------------------------------------------
         */
        var instalacao =
            await _context.Instalacoes
                .Include(x => x.Cliente)
                .Include(x => x.Sistema)
                .FirstOrDefaultAsync(
                    x =>
                        x.SistemaId == sistema.SistemaId &&
                        x.InstalacaoKey == oficinaId);

        var bootstrapTokenValido = ValidarBootstrapToken();
        var bootstrapTokenConfigurado = !string.IsNullOrWhiteSpace(
            _configuration["Registration:BootstrapToken"]);
        var chaveInstalacaoValida = false;

        if (instalacao == null && bootstrapTokenConfigurado && !bootstrapTokenValido)
        {
            return Unauthorized(new
            {
                sucesso = false,
                codigo = "TOKEN_REGISTRO_INVALIDO",
                mensagem = "Token de autorização do registro ausente ou inválido."
            });
        }

        if (instalacao != null && !bootstrapTokenValido)
        {
            var chaveAtual = Request.Headers["X-Installation-Key"].FirstOrDefault();
            chaveInstalacaoValida = await _credencialService.ValidarChaveAtivaAsync(
                instalacao.InstalacaoId,
                chaveAtual ?? string.Empty);

            if (!chaveInstalacaoValida)
            {
                return Unauthorized(new
                {
                    sucesso = false,
                    codigo = "CREDENCIAL_OBRIGATORIA",
                    mensagem = "Informe a credencial ativa da instalação ou o token de autorização do registro."
                });
            }
        }

        if (instalacao is { Ativa: false })
        {
            return Conflict(new
            {
                sucesso = false,
                codigo = "INSTALACAO_INATIVA",
                mensagem = "A instalação está inativa e não pode ser sincronizada."
            });
        }

        Cliente? cliente;

        /*
         * --------------------------------------------------------
         * 4. INSTALAÇÃO EXISTENTE
         * --------------------------------------------------------
         */
        if (instalacao != null)
        {
            cliente =
                instalacao.Cliente;

            if (cliente == null)
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new
                    {
                        sucesso = false,
                        codigo = "CLIENTE_NAO_VINCULADO",
                        mensagem =
                            "A instalação encontrada não possui um cliente vinculado."
                    });
            }

            if (!cliente.Ativo)
            {
                return Conflict(new
                {
                    sucesso = false,
                    codigo = "CLIENTE_INATIVO",
                    mensagem =
                        "O cliente vinculado à instalação está inativo."
                });
            }

            var alterouCliente =
                false;

            if (!string.Equals(
                    cliente.Nome,
                    nomeOficina,
                    StringComparison.Ordinal))
            {
                cliente.Nome =
                    nomeOficina;

                alterouCliente = true;
            }

            if (!string.IsNullOrWhiteSpace(documento) &&
                !string.Equals(
                    cliente.Documento,
                    documento,
                    StringComparison.Ordinal))
            {
                cliente.Documento =
                    documento;

                alterouCliente = true;
            }

            if (!string.IsNullOrWhiteSpace(email) &&
                !string.Equals(
                    cliente.Email,
                    email,
                    StringComparison.Ordinal))
            {
                cliente.Email =
                    email;

                alterouCliente = true;
            }

            if (!string.IsNullOrWhiteSpace(telefone) &&
                !string.Equals(
                    cliente.Telefone,
                    telefone,
                    StringComparison.Ordinal))
            {
                cliente.Telefone =
                    telefone;

                alterouCliente = true;
            }

            if (!string.IsNullOrWhiteSpace(endereco) &&
                !string.Equals(
                    cliente.Endereco,
                    endereco,
                    StringComparison.Ordinal))
            {
                cliente.Endereco =
                    endereco;

                alterouCliente = true;
            }

            if (!string.IsNullOrWhiteSpace(numero) &&
                !string.Equals(
                    cliente.Numero,
                    numero,
                    StringComparison.Ordinal))
            {
                cliente.Numero =
                    numero;

                alterouCliente = true;
            }

            if (!string.Equals(
                    instalacao.NomeInstalacao,
                    nomeOficina,
                    StringComparison.Ordinal))
            {
                instalacao.NomeInstalacao =
                    nomeOficina;

                alterouCliente = true;
            }

            if (alterouCliente)
            {
                await _context.SaveChangesAsync();
            }
        }
        /*
         * --------------------------------------------------------
         * 5. INSTALAÇÃO NOVA
         * --------------------------------------------------------
         */
        else
        {
            cliente = null;

            if (!string.IsNullOrWhiteSpace(documento))
            {
                cliente =
                    await _context.Clientes
                        .FirstOrDefaultAsync(
                            x =>
                                x.Documento == documento);
            }

            if (cliente != null)
            {
                if (!cliente.Ativo)
                {
                    return Conflict(new
                    {
                        sucesso = false,
                        codigo = "CLIENTE_INATIVO",
                        mensagem =
                            "O cliente encontrado pelo CNPJ está inativo."
                    });
                }

                cliente.Nome =
                    nomeOficina;

                if (!string.IsNullOrWhiteSpace(documento))
                {
                    cliente.Documento =
                        documento;
                }

                if (!string.IsNullOrWhiteSpace(email))
                {
                    cliente.Email =
                        email;
                }

                if (!string.IsNullOrWhiteSpace(telefone))
                {
                    cliente.Telefone =
                        telefone;
                }

                if (!string.IsNullOrWhiteSpace(endereco))
                {
                    cliente.Endereco =
                        endereco;
                }

                if (!string.IsNullOrWhiteSpace(numero))
                {
                    cliente.Numero =
                        numero;
                }

                await _context.SaveChangesAsync();
            }
            else
            {
                cliente =
                    new Cliente
                    {
                        Nome =
                            nomeOficina,

                        Documento =
                            documento,

                        Email =
                            email,

                        Telefone =
                            telefone,

                        Endereco =
                            endereco,

                        Numero =
                            numero,

                        Ativo =
                            true,

                        CriadoEm =
                            RelogioSistema.Agora
                    };

                _context.Clientes.Add(
                    cliente);

                await _context.SaveChangesAsync();
            }

            instalacao =
                await _instalacaoService.CriarAsync(
                    cliente.ClienteId,
                    sistema.SistemaId,
                    oficinaId,
                    nomeOficina);
        }

        /*
         * --------------------------------------------------------
         * 6. LOCALIZA OU CRIA A LICENÇA
         * --------------------------------------------------------
         */
        var licenca =
            await _context.Licencas
                .Where(x =>
                    x.OficinaId == oficinaId &&
                    (x.InstalacaoId == instalacao.InstalacaoId ||
                     x.InstalacaoId == null))
                .OrderByDescending(x => x.LicencaId)
                .FirstOrDefaultAsync();

        if (licenca == null)
        {
            licenca =
                new Licenca
                {
                    OficinaId =
                        oficinaId,

                    NomeOficina =
                        nomeOficina,

                    InstalacaoId =
                        instalacao.InstalacaoId,

                    Ativa =
                        false,

                    Plano =
                        null,

                    PlanoId =
                        null,

                    DataInicio =
                        null,

                    DataVencimento =
                        null,

                    UltimaValidacao =
                        null,

                    ValidaAte =
                        null,

                    Mensagem =
                        "Licença aguardando ativação.",

                    SituacaoComercial =
                        Licenca.SituacaoNormal
                };

            _context.Licencas.Add(
                licenca);

            await _context.SaveChangesAsync();
        }
        else
        {
            var alterouLicenca =
                false;

            if (licenca.InstalacaoId !=
                instalacao.InstalacaoId)
            {
                licenca.InstalacaoId =
                    instalacao.InstalacaoId;

                alterouLicenca = true;
            }

            if (!string.Equals(
                    licenca.NomeOficina,
                    nomeOficina,
                    StringComparison.Ordinal))
            {
                licenca.NomeOficina =
                    nomeOficina;

                alterouLicenca = true;
            }

            if (alterouLicenca)
            {
                await _context.SaveChangesAsync();
            }
        }

        /*
         * --------------------------------------------------------
         * 7. CREDENCIAL
         * --------------------------------------------------------
         */
        var credencial =
            await _credencialService.ObterAtivaAsync(
                instalacao.InstalacaoId);

        var deveCriarNovaCredencial =
            credencial == null ||
            request.SolicitarNovaCredencial ||
            (bootstrapTokenValido &&
             !chaveInstalacaoValida &&
             Request.Headers.ContainsKey("X-Installation-Key"));

        string? chaveInstalacao =
            null;

        if (deveCriarNovaCredencial)
        {
            var resultadoCredencial =
                await _credencialService.CriarAsync(
                    instalacao.InstalacaoId);

            chaveInstalacao =
                resultadoCredencial.Chave;
        }

        /*
         * --------------------------------------------------------
         * 8. RESPOSTA
         * --------------------------------------------------------
         */
        return Ok(new
        {
            sucesso = true,

            codigo =
                deveCriarNovaCredencial
                    ? "INSTALACAO_REGISTRADA_CREDENCIAL_CRIADA"
                    : "INSTALACAO_SINCRONIZADA",

            mensagem =
                deveCriarNovaCredencial
                    ? "Instalação registrada e nova credencial criada com sucesso."
                    : "Instalação sincronizada com sucesso.",

            instalacao = new
            {
                instalacao.InstalacaoId,
                instalacao.InstalacaoKey,
                instalacao.NomeInstalacao,
                instalacao.ClienteId,
                instalacao.SistemaId,

                sistemaCodigo =
                    sistema.Codigo,

                sistemaNome =
                    sistema.Nome
            },

            licenca = new
            {
                licenca.LicencaId,
                licenca.OficinaId,
                licenca.NomeOficina,
                licenca.Ativa,

                status =
                    StatusLicencaService.ObterStatus(
                        licenca),

                licenca.PlanoId,
                licenca.Plano,
                licenca.DataInicio,
                licenca.DataVencimento,
                licenca.ValidaAte,
                licenca.Mensagem
            },

            credencial =
                chaveInstalacao
        });
    }

    /*
     * ============================================================
     * CONSULTAR LICENÇA
     * ============================================================
     */
    [Authorize(
        AuthenticationSchemes =
            Authentication.InstalacaoAuthenticationHandler.SchemeName)]
    [HttpGet]
    public async Task<IActionResult> Consultar()
    {
        var licenca =
            await ObterLicencaDaInstalacaoAutenticadaAsync();

        if (licenca == null)
        {
            return NotFound(
                CriarResposta<LicencaResposta>(
                    false,
                    "LICENCA_NAO_ENCONTRADA",
                    "Não existe licença vinculada à instalação autenticada."));
        }

        return Ok(
            CriarResposta(
                true,
                "LICENCA_CONSULTADA",
                "Licença consultada com sucesso.",
                CriarDados(licenca)));
    }

    /*
     * ============================================================
     * VALIDAR LICENÇA
     * ============================================================
     *
     * IMPORTANTE:
     *
     * Este endpoint NÃO usa o InstalacaoAuthenticationHandler.
     *
     * O motivo é que ele precisa conseguir receber uma credencial
     * que foi revogada. Caso contrário o AuthenticationHandler
     * retornaria 401 antes de o método ser executado.
     *
     * A validação da chave é feita manualmente aqui.
     */
    [AllowAnonymous]
    [EnableRateLimiting("license-validation")]
    [HttpPost("validar")]
    public async Task<IActionResult> Validar()
    {
        if (!Request.Headers.TryGetValue(
                "X-Installation-Key",
                out var valoresChave))
        {
            return Unauthorized(
                CriarResposta<LicencaResposta>(
                    false,
                    "CREDENCIAL_NAO_INFORMADA",
                    "A credencial da instalação não foi informada."));
        }

        var chave =
            valoresChave.FirstOrDefault();

        if (string.IsNullOrWhiteSpace(chave))
        {
            return Unauthorized(
                CriarResposta<LicencaResposta>(
                    false,
                    "CREDENCIAL_NAO_INFORMADA",
                    "A credencial da instalação não foi informada."));
        }

        /*
         * --------------------------------------------------------
         * 1. CALCULA O HASH DA CHAVE RECEBIDA
         * --------------------------------------------------------
         */
        var hash =
            CalcularHash(chave);

        /*
         * --------------------------------------------------------
         * 2. LOCALIZA A CREDENCIAL
         * --------------------------------------------------------
         *
         * Aqui NÃO exigimos Ativa.
         *
         * Precisamos conseguir identificar uma credencial antiga
         * que tenha sido revogada para descobrir a instalação
         * correspondente.
         */
        var credencial =
            await _context.CredenciaisInstalacao
                .Include(x => x.Instalacao)
                    .ThenInclude(x => x!.Sistema)
                .Include(x => x.Instalacao)
                    .ThenInclude(x => x!.Cliente)
                .FirstOrDefaultAsync(
                    x =>
                        x.ChaveHash == hash);

        /*
         * --------------------------------------------------------
         * 3. CREDENCIAL NÃO EXISTE
         * --------------------------------------------------------
         */
        if (credencial == null ||
            credencial.Instalacao == null)
        {
            return Unauthorized(
                CriarResposta<LicencaResposta>(
                    false,
                    "CREDENCIAL_INVALIDA",
                    "A credencial da instalação é inválida."));
        }

        var instalacao =
            credencial.Instalacao;

        if (instalacao.Cliente == null ||
            !instalacao.Cliente.Ativo)
        {
            return Unauthorized(
                CriarResposta<LicencaResposta>(
                    false,
                    "CLIENTE_INATIVO",
                    "O cliente vinculado à instalação está inativo."));
        }

        /*
         * --------------------------------------------------------
         * 4. CREDENCIAL REVOGADA
         * --------------------------------------------------------
         *
         * A API é a responsável pela recuperação.
         *
         * O serviço verifica se existe uma nova credencial
         * temporariamente disponível para esta credencial antiga.
         *
         * Caso não exista, a própria API cria uma nova.
         *
         * O App nunca solicita a criação.
         */
        if (!credencial.Ativa)
        {
            if (!instalacao.Ativa)
            {
                return Unauthorized(
                    CriarResposta<LicencaResposta>(
                        false,
                        "INSTALACAO_INATIVA",
                        "A instalação está inativa."));
            }

            if (instalacao.Sistema == null ||
                !instalacao.Sistema.Ativo)
            {
                return Unauthorized(
                    CriarResposta<LicencaResposta>(
                        false,
                        "SISTEMA_INATIVO",
                        "O sistema desta instalação está inativo."));
            }

            var resultadoNovaCredencial =
                await _credencialService
                    .RecuperarOuCriarParaCredencialRevogadaAsync(
                        credencial);

            if (resultadoNovaCredencial == null)
            {
                return Unauthorized(
                    CriarResposta<LicencaResposta>(
                        false,
                        "CREDENCIAL_REVOGADA",
                        "A credencial da instalação foi revogada e não foi possível disponibilizar uma nova credencial."));
            }

            /*
             * A nova credencial é devolvida somente neste fluxo.
             */
            var licencaRecuperada =
                await ObterLicencaDaInstalacaoAsync(
                    instalacao.InstalacaoId);

            if (licencaRecuperada == null)
            {
                return Ok(
                    new
                    {
                        sucesso = true,
                        codigo = "CREDENCIAL_RECUPERADA",
                        mensagem =
                            "A credencial da instalação foi recuperada com sucesso.",
                        credencial =
                            resultadoNovaCredencial.Chave,
                        dados = (LicencaResposta?)null
                    });
            }

            var agoraRecuperacao =
                RelogioSistema.Agora;

            licencaRecuperada.UltimaValidacao =
                agoraRecuperacao;

            instalacao.UltimaComunicacao =
                agoraRecuperacao;

            await _context.SaveChangesAsync();

            var dadosRecuperacao =
                CriarDados(
                    licencaRecuperada,
                    agoraRecuperacao);

            dadosRecuperacao.Status =
                StatusLicencaService.ObterStatus(
                    licencaRecuperada,
                    instalacao.Sistema?.DiasTolerancia);

            dadosRecuperacao.PodeUsarSistema =
                StatusLicencaService.PodeUsarSistema(
                    licencaRecuperada,
                    instalacao.Sistema?.DiasTolerancia);

            return Ok(
                new
                {
                    sucesso = true,
                    codigo = "CREDENCIAL_RECUPERADA",
                    mensagem =
                        "A credencial da instalação foi recuperada com sucesso.",
                    credencial =
                        resultadoNovaCredencial.Chave,
                    dados =
                        dadosRecuperacao
                });
        }

        /*
         * --------------------------------------------------------
         * 5. CREDENCIAL VÁLIDA
         * --------------------------------------------------------
         */
        if (!instalacao.Ativa)
        {
            return Unauthorized(
                CriarResposta<LicencaResposta>(
                    false,
                    "INSTALACAO_INATIVA",
                    "A instalação está inativa."));
        }

        if (instalacao.Sistema == null ||
            !instalacao.Sistema.Ativo)
        {
            return Unauthorized(
                CriarResposta<LicencaResposta>(
                    false,
                    "SISTEMA_INATIVO",
                    "O sistema desta instalação está inativo."));
        }

        /*
         * Atualiza utilização da credencial.
         */
        credencial.UltimaUtilizacao =
            RelogioSistema.Agora;

        instalacao.UltimaComunicacao =
            RelogioSistema.Agora;

        /*
         * --------------------------------------------------------
         * 6. LOCALIZA LICENÇA
         * --------------------------------------------------------
         */
        var licenca =
            await ObterLicencaDaInstalacaoAsync(
                instalacao.InstalacaoId);

        if (licenca == null)
        {
            await _context.SaveChangesAsync();

            return NotFound(
                CriarResposta<LicencaResposta>(
                    false,
                    "LICENCA_NAO_ENCONTRADA",
                    "Não existe licença vinculada à instalação autenticada."));
        }

        var agora =
            RelogioSistema.Agora;

        var status =
            StatusLicencaService.ObterStatus(
                licenca,
                instalacao.Sistema?.DiasTolerancia);

        var podeUsar =
            StatusLicencaService.PodeUsarSistema(
                licenca,
                instalacao.Sistema?.DiasTolerancia);

        licenca.UltimaValidacao =
            agora;

        await _context.SaveChangesAsync();

        var dados =
            CriarDados(
                licenca,
                agora);

            dados.Status =
                status;

        dados.PodeUsarSistema =
            podeUsar;

        if (!podeUsar)
        {
            return Ok(
                CriarResposta(
                    false,
                    ObterCodigoStatus(status),
                    ObterMensagemStatus(status),
                    dados));
        }

        return Ok(
            CriarResposta(
                true,
                "LICENCA_VALIDA",
                "Licença válida.",
                dados));
    }

    /*
     * ============================================================
     * LOCALIZA LICENÇA DA INSTALAÇÃO AUTENTICADA
     * ============================================================
     */
    private async Task<Licenca?>
        ObterLicencaDaInstalacaoAutenticadaAsync()
    {
        var claim =
            User.FindFirst(
                "instalacao_id");

        if (claim == null)
        {
            return null;
        }

        if (!int.TryParse(
                claim.Value,
                out var instalacaoId))
        {
            return null;
        }

        return await ObterLicencaDaInstalacaoAsync(
            instalacaoId);
    }

    /*
     * ============================================================
     * LOCALIZA LICENÇA POR INSTALAÇÃO
     * ============================================================
     */
    private async Task<Licenca?>
        ObterLicencaDaInstalacaoAsync(
            int instalacaoId)
    {
        return await _context.Licencas
            .Include(x => x.Instalacao)
                .ThenInclude(x => x!.Sistema)

            .Include(x => x.Instalacao)
                .ThenInclude(x => x!.Cliente)

            .Include(x => x.PlanoRelacionamento)

            .Where(
                x =>
                    x.InstalacaoId ==
                    instalacaoId)

            .OrderByDescending(
                x =>
                    x.LicencaId)

            .FirstOrDefaultAsync();
    }

    /*
     * ============================================================
     * MONTA RESPOSTA DA LICENÇA
     * ============================================================
     */
    private static LicencaResposta CriarDados(
        Licenca licenca,
        DateTime? ultimaValidacao = null)
    {
        var instalacao =
            licenca.Instalacao;

        var sistema =
            instalacao?.Sistema;

        var cliente =
            instalacao?.Cliente;

        var plano =
            licenca.PlanoRelacionamento;

        return new LicencaResposta
        {
            LicencaId =
                licenca.LicencaId,

            InstalacaoId =
                instalacao?.InstalacaoId ??
                licenca.InstalacaoId ??
                0,

            ClienteId =
                instalacao?.ClienteId ??
                0,

            ClienteNome =
                cliente?.Nome,

            ClienteAtivo =
                cliente?.Ativo,

            StatusCliente =
                cliente == null ? null : cliente.Ativo ? "Ativo" : "Inativo",

            InstalacaoAtiva =
                instalacao?.Ativa,

            StatusInstalacao =
                instalacao == null ? null : instalacao.Ativa ? "Ativa" : "Inativa",

            SistemaId =
                instalacao?.SistemaId ??
                0,

            SistemaCodigo =
                sistema?.Codigo ??
                string.Empty,

            SistemaNome =
                sistema?.Nome ??
                string.Empty,

            SistemaAtivo =
                sistema?.Ativo,

            StatusSistema =
                sistema == null ? null : sistema.Ativo ? "Ativo" : "Inativo",

            InstalacaoKey =
                instalacao?.InstalacaoKey ??
                licenca.OficinaId,

            NomeOficina =
                licenca.NomeOficina,

            Ativa =
                licenca.Ativa,

            Status =
                StatusLicencaService.ObterStatus(
                    licenca),

            SituacaoComercial =
                licenca.SituacaoComercial,

            PlanoId =
                licenca.PlanoId,

            PlanoAtivo =
                plano?.Ativo,

            StatusPlano =
                plano == null ? null : plano.Ativo ? "Ativo" : "Inativo",

            Plano =
                plano?.Nome
                ?? licenca.Plano,

            DataInicio =
                licenca.DataInicio,

            DataVencimento =
                licenca.DataVencimento,

            ValidaAte =
                StatusLicencaService.ObterValidaAte(
                    licenca,
                    sistema?.DiasTolerancia),

            UltimaValidacao =
                ultimaValidacao
                ?? licenca.UltimaValidacao,

            PodeUsarSistema =
                StatusLicencaService.PodeUsarSistema(
                    licenca),

            Mensagem =
                licenca.Mensagem,

            DiasTolerancia =
                sistema?.DiasTolerancia ??
                0,

            DiasOffline =
                sistema?.DiasOffline ??
                0
        };
    }

    private static string ObterCodigoStatus(
        string status)
    {
        return status switch
        {
            StatusLicencaService.AguardandoAtivacao =>
                "LICENCA_AGUARDANDO_ATIVACAO",

            StatusLicencaService.Ativa =>
                "LICENCA_ATIVA",

            StatusLicencaService.EmTolerancia =>
                "LICENCA_EM_TOLERANCIA",

            StatusLicencaService.Vencida =>
                "LICENCA_VENCIDA",

            StatusLicencaService.Inativa =>
                "LICENCA_INATIVA",

            _ =>
                "LICENCA_INVALIDA"
        };
    }

    private static string ObterMensagemStatus(
        string status)
    {
        return status switch
        {
            StatusLicencaService.AguardandoAtivacao =>
                "A licença desta instalação ainda aguarda ativação.",

            StatusLicencaService.Ativa =>
                "Licença ativa.",

            StatusLicencaService.EmTolerancia =>
                "A licença está em período de tolerância.",

            StatusLicencaService.Vencida =>
                "A licença está vencida.",

            StatusLicencaService.Inativa =>
                "A licença está inativa.",

            _ =>
                "A licença não está autorizada."
        };
    }

    private static ApiResposta<T> CriarResposta<T>(
        bool sucesso,
        string codigo,
        string mensagem,
        T? dados = default)
    {
        return new ApiResposta<T>
        {
            Sucesso =
                sucesso,

            Codigo =
                codigo,

            Mensagem =
                mensagem,

            Dados =
                dados
        };
    }

    private static string? NormalizarCampo(
        string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return null;
        }

        valor =
            valor.Trim();

        return valor.Length == 0
            ? null
            : valor;
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

    private bool ValidarBootstrapToken()
    {
        var esperado = _configuration["Registration:BootstrapToken"];
        var recebido = Request.Headers["X-Registration-Token"].FirstOrDefault();
        if (string.IsNullOrEmpty(esperado) || string.IsNullOrEmpty(recebido))
        {
            return false;
        }

        var esperadoHash = SHA256.HashData(Encoding.UTF8.GetBytes(esperado));
        var recebidoHash = SHA256.HashData(Encoding.UTF8.GetBytes(recebido));
        return CryptographicOperations.FixedTimeEquals(esperadoHash, recebidoHash);
    }
}

/*
 * ================================================================
 * DTO DE REGISTRO
 * ================================================================
 */
public class RegistrarLicencaRequest
{
    [Required, StringLength(50)]
    public string OficinaId { get; set; } = null!;

    [Required, StringLength(150)]
    public string NomeOficina { get; set; } = null!;

    [Required, StringLength(50)]
    public string SistemaCodigo { get; set; } = null!;

    [StringLength(30)]
    public string? Cnpj { get; set; }

    [StringLength(30)]
    public string? Telefone { get; set; }

    [StringLength(150)]
    public string? Email { get; set; }

    [StringLength(300)]
    public string? Endereco { get; set; }

    [StringLength(50)]
    public string? Numero { get; set; }

    public bool SolicitarNovaCredencial { get; set; }
}
