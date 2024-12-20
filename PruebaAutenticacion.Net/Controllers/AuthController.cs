using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

[ApiController]
[Route("api/")]
public class AuthController : ControllerBase
{
    private readonly LdapService _ldapService;
    private readonly IConfiguration _configuration;

    public AuthController(LdapService ldapService, IConfiguration configuration)
    {
        _ldapService = ldapService;
        _configuration = configuration;
    }

    [HttpPost("authenticate")]
    [AllowAnonymous]
    public IActionResult Authenticate([FromBody] LoginRequest request)
    {
        try
        {
            // Llamar al servicio LDAP para autenticar al usuario
            var (isAuthenticated, userInfo, errorMessage, errorCode) = _ldapService.AuthenticateUser(request.Username, request.Password);

            if (!isAuthenticated)
            {
                // Si la autenticación falla, retorna un error detallado
                if (errorCode == 404)
                {
                    return NotFound(new { status = "error", message = errorMessage }); // Usuario no encontrado
                }
                else if (errorCode == 500)
                {
                    return StatusCode(500, new { status = "error", message = errorMessage }); // Error en el servidor LDAP
                }
                else
                {
                    return Unauthorized(new { status = "error", message = errorMessage }); // Credenciales inválidas
                }
            }

            // Si la autenticación es exitosa, devuelve los datos del usuario
            return Ok(new
            {
                status = "success",
                user = userInfo
            });
        }
        catch (Exception ex)
        {
            // Registrar el error para mayor información en el servidor
            Console.WriteLine($"Error: {ex.Message}");

            // Responder con un error de servidor interno
            return StatusCode(500, new { status = "error", message = "Internal Server Error" });
        }
    }



    private string GenerateJwtToken(string username)
    {
        var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, username) }),
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
            Issuer = _configuration["Jwt:Issuer"],
            Audience = _configuration["Jwt:Audience"]
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}

public class LoginRequest
{
    public string Username { get; set; }
    public string Password { get; set; }
}

