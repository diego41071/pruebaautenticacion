using System;
using System.Collections.Generic;
using Novell.Directory.Ldap;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

public class LdapService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<LdapService> _logger;

    public LdapService(IConfiguration configuration, ILogger<LdapService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public (bool IsAuthenticated, Dictionary<string, string> UserInfo, string ErrorMessage, int ErrorCode) AuthenticateUser(string username, string password)
    {
        string ldapServer = _configuration["LDAP:Server"];
        int ldapPort = int.Parse(_configuration["LDAP:Port"]);
        string baseDn = _configuration["LDAP:BaseDn"];
        string ldapUser = $"uid={username},ou=users,dc=example,dc=com"; // Nombre distinguido (DN).

        try
        {
            _logger.LogInformation($"Attempting to authenticate user '{username}' against LDAP server '{ldapServer}:{ldapPort}'");

            using (var ldapConnection = new LdapConnection())
            {
                // Conexión al servidor LDAP
                ldapConnection.Connect(ldapServer, ldapPort);
                _logger.LogInformation($"Connected to LDAP server at {ldapServer}:{ldapPort}");

                // Intento de autenticación
                ldapConnection.Bind(ldapUser, password); // Simple bind
                _logger.LogInformation($"User '{username}' bound to LDAP server successfully.");

                // Buscar información del usuario
                var searchFilter = $"(uid={username})";
                var searchResults = ldapConnection.Search(
                    baseDn,
                    LdapConnection.ScopeSub,
                    searchFilter,
                    new string[] { "displayName", "mail", "department", "title" },
                    false); // No devolver contraseñas

                if (searchResults.HasMore())
                {
                    var entry = searchResults.Next();
                    var userInfo = new Dictionary<string, string>
                    {
                        { "username", username },
                        { "displayName", entry.GetAttribute("displayName")?.StringValue ?? string.Empty },
                        { "email", entry.GetAttribute("mail")?.StringValue ?? string.Empty },
                        { "department", entry.GetAttribute("department")?.StringValue ?? string.Empty },
                        { "title", entry.GetAttribute("title")?.StringValue ?? string.Empty }
                    };

                    return (true, userInfo, string.Empty, 0); // No error
                }
                else
                {
                    string errorMessage = "User not found in LDAP directory.";
                    _logger.LogWarning($"Authentication failed: {errorMessage} for user '{username}'.");
                    return (false, null, errorMessage, 404); // User not found
                }
            }
        }
        catch (LdapException ex)
        {
            // Registrar error LDAP específico
            string errorMessage = $"LDAP error occurred. Code: {ex.ResultCode}, Message: {ex.Message}";
            _logger.LogError(errorMessage);
            return (false, null, errorMessage, 500); // Internal server error
        }
        catch (ArgumentException ex)
        {
            // Error de argumento
            string errorMessage = $"Argument error occurred. Message: {ex.Message}";
            _logger.LogError(errorMessage);
            return (false, null, errorMessage, 400); // Bad request error
        }
        catch (TimeoutException ex)
        {
            // Error de tiempo de espera
            string errorMessage = $"Connection to LDAP server timed out. Message: {ex.Message}";
            _logger.LogError(errorMessage);
            return (false, null, errorMessage, 504); // Gateway timeout
        }
        catch (Exception ex)
        {
            // Capturar otros errores generales
            string errorMessage = $"Unexpected error occurred. Message: {ex.Message}";
            _logger.LogError(errorMessage);
            return (false, null, errorMessage, 500); // General error
        }
    }
}
