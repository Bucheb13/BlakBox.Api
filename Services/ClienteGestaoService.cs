using Microsoft.EntityFrameworkCore;
using BlakBox.Api.Data;
using BlakBox.Api.Models;

namespace BlakBox.Api.Services;

public class ClienteGestaoService
{
    private readonly LicencaDbContext _context;

    public ClienteGestaoService(
        LicencaDbContext context)
    {
        _context = context;
    }

    public async Task<List<Cliente>> ListarAsync()
    {
        return await _context.Clientes
            .AsNoTracking()
            .Include(x => x.Instalacoes)
            .OrderBy(x => x.Nome)
            .ToListAsync();
    }

    public async Task<Cliente?> ObterAsync(
        int clienteId)
    {
        return await _context.Clientes
            .FirstOrDefaultAsync(
                x => x.ClienteId == clienteId);
    }

    public async Task<Cliente> CriarAsync(
        string nome,
        string? documento,
        string? email,
        string? telefone,
        string? endereco,
        string? numero)
    {
        nome = NormalizarNome(nome);
        documento = NormalizarCampo(documento);
        email = NormalizarCampo(email);
        telefone = NormalizarCampo(telefone);
        endereco = NormalizarCampo(endereco);
        numero = NormalizarCampo(numero);

        ValidarDados(
            nome,
            documento,
            email,
            telefone,
            endereco,
            numero);

        /*
         * Documento, quando informado, não pode ficar
         * duplicado.
         */
        if (!string.IsNullOrWhiteSpace(documento))
        {
            var documentoExiste =
                await _context.Clientes
                    .AnyAsync(
                        x =>
                            x.Documento == documento);

            if (documentoExiste)
            {
                throw new InvalidOperationException(
                    $"Já existe um cliente com o documento \"{documento}\".");
            }
        }

        var cliente =
            new Cliente
            {
                Nome = nome,
                Documento = documento,
                Email = email,
                Telefone = telefone,
                Endereco = endereco,
                Numero = numero,
                Ativo = true,
                CriadoEm = RelogioSistema.Agora
            };

        _context.Clientes.Add(cliente);

        await _context.SaveChangesAsync();

        return cliente;
    }

    public async Task AtualizarAsync(
        int clienteId,
        string nome,
        string? documento,
        string? email,
        string? telefone,
        string? endereco,
        string? numero)
    {
        var cliente =
            await ObterAsync(clienteId);

        if (cliente == null)
        {
            throw new InvalidOperationException(
                "Cliente não encontrado.");
        }

        nome = NormalizarNome(nome);
        documento = NormalizarCampo(documento);
        email = NormalizarCampo(email);
        telefone = NormalizarCampo(telefone);
        endereco = NormalizarCampo(endereco);
        numero = NormalizarCampo(numero);

        ValidarDados(
            nome,
            documento,
            email,
            telefone,
            endereco,
            numero);

        if (!string.IsNullOrWhiteSpace(documento))
        {
            var documentoExiste =
                await _context.Clientes
                    .AnyAsync(
                        x =>
                            x.ClienteId != clienteId &&
                            x.Documento == documento);

            if (documentoExiste)
            {
                throw new InvalidOperationException(
                    $"Já existe outro cliente com o documento \"{documento}\".");
            }
        }

        cliente.Nome =
            nome;

        cliente.Documento =
            documento;

        cliente.Email =
            email;

        cliente.Telefone =
            telefone;

        cliente.Endereco =
            endereco;

        cliente.Numero =
            numero;

        await _context.SaveChangesAsync();
    }

    public async Task AlterarStatusAsync(
        int clienteId,
        bool ativo)
    {
        var cliente =
            await ObterAsync(clienteId);

        if (cliente == null)
        {
            throw new InvalidOperationException(
                "Cliente não encontrado.");
        }

        if (cliente.Ativo == ativo)
        {
            return;
        }

        if (!ativo)
        {
            var licencasAtivasDaInstalacao = await _context.Licencas
                .Include(x => x.Instalacao)
                    .ThenInclude(x => x!.Sistema)
                .Where(x => x.Instalacao != null &&
                    x.Instalacao.ClienteId == clienteId && x.Instalacao.Ativa)
                .ToListAsync();

            var possuiAssinaturaUtilizavel = licencasAtivasDaInstalacao.Any(x =>
                    !string.Equals(x.SituacaoComercial,
                        Licenca.SituacaoCancelada, StringComparison.OrdinalIgnoreCase) &&
                    StatusLicencaService.PodeUsarSistema(
                        x, x.Instalacao!.Sistema.DiasTolerancia));

            if (possuiAssinaturaUtilizavel)
            {
                throw new InvalidOperationException(
                    "Não é possível inativar este cliente enquanto houver uma instalação com licença válida.");
            }
        }

        cliente.Ativo =
            ativo;

        await _context.SaveChangesAsync();
    }

    private static string NormalizarNome(
        string nome)
    {
        if (string.IsNullOrWhiteSpace(nome))
        {
            throw new InvalidOperationException(
                "O nome do cliente é obrigatório.");
        }

        return nome.Trim();
    }

    private static string? NormalizarCampo(
        string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return null;
        }

        return valor.Trim();
    }

    private static void ValidarDados(
        string nome,
        string? documento,
        string? email,
        string? telefone,
        string? endereco,
        string? numero)
    {
        if (nome.Length > 150)
        {
            throw new InvalidOperationException(
                "O nome do cliente deve ter no máximo 150 caracteres.");
        }

        if (!string.IsNullOrWhiteSpace(documento) &&
            documento.Length > 30)
        {
            throw new InvalidOperationException(
                "O documento deve ter no máximo 30 caracteres.");
        }

        if (!string.IsNullOrWhiteSpace(email) &&
            email.Length > 150)
        {
            throw new InvalidOperationException(
                "O e-mail deve ter no máximo 150 caracteres.");
        }

        if (!string.IsNullOrWhiteSpace(telefone) &&
            telefone.Length > 30)
        {
            throw new InvalidOperationException(
                "O telefone deve ter no máximo 30 caracteres.");
        }

        if (!string.IsNullOrWhiteSpace(endereco) &&
            endereco.Length > 300)
        {
            throw new InvalidOperationException(
                "O endereço deve ter no máximo 300 caracteres.");
        }

        if (!string.IsNullOrWhiteSpace(numero) &&
            numero.Length > 20)
        {
            throw new InvalidOperationException(
                "O número deve ter no máximo 20 caracteres.");
        }
    }
}
