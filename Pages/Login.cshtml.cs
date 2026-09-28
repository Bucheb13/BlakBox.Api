using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace BlakBox.Api.Pages;

[EnableRateLimiting("admin-login")]
public class LoginModel : PageModel
{
    private readonly IConfiguration _configuration;

    public LoginModel(IConfiguration configuration) => _configuration = configuration;

    [BindProperty]
    public string Username { get; set; } = string.Empty;

    [BindProperty]
    public string Password { get; set; } = string.Empty;

    public string? ErrorMessage { get; private set; }

    public bool ConfiguracaoDisponivel =>
        !string.IsNullOrWhiteSpace(_configuration["Admin_Username"]) &&
        !string.IsNullOrWhiteSpace(_configuration["Admin_Password"]);

    public void OnGet()
    {
        if (!ConfiguracaoDisponivel)
        {
            ErrorMessage = "Acesso administrativo ainda não configurado. Em desenvolvimento, execute dotnet user-secrets set para Admin_Username e Admin_Password. Em produção, configure as variáveis secretas do ambiente.";
        }
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl)
    {
        var expectedUsername = _configuration["Admin_Username"];
        var expectedPassword = _configuration["Admin_Password"];
        if (string.IsNullOrWhiteSpace(expectedUsername) ||
            string.IsNullOrWhiteSpace(expectedPassword))
        {
            ErrorMessage = "Acesso administrativo ainda não configurado. Em desenvolvimento, execute dotnet user-secrets set para Admin_Username e Admin_Password. Em produção, configure as variáveis secretas do ambiente.";
            return Page();
        }

        var usernameMatches = !string.IsNullOrWhiteSpace(expectedUsername) &&
            string.Equals(Username, expectedUsername, StringComparison.Ordinal);
        var passwordMatches = CompararSegredo(Password, expectedPassword);

        if (!usernameMatches || !passwordMatches)
        {
            ErrorMessage = "Usuário ou senha inválidos.";
            return Page();
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, expectedUsername!),
            new Claim(ClaimTypes.Role, "Admin"),
            new Claim("admin_security_stamp", Convert.ToHexString(SHA256.HashData(
                Encoding.UTF8.GetBytes(expectedUsername + "\0" + expectedPassword!))))
        };
        var principal = new ClaimsPrincipal(
            new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = false,
                AllowRefresh = true
            });

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        return RedirectToPage("/Index");
    }

    private static bool CompararSegredo(string recebido, string? esperado)
    {
        if (string.IsNullOrEmpty(esperado))
        {
            return false;
        }

        var hashRecebido = SHA256.HashData(Encoding.UTF8.GetBytes(recebido));
        var hashEsperado = SHA256.HashData(Encoding.UTF8.GetBytes(esperado));
        return CryptographicOperations.FixedTimeEquals(hashRecebido, hashEsperado);
    }
}
