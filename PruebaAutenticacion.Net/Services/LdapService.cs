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
            using (var ldapConnection = new LdapConnection())
            {
                // Conexión al servidor LDAP
                ldapConnection.Connect(ldapServer, ldapPort);

                // Intento de autenticación
                ldapConnection.Bind(ldapUser, password); // Simple bind

                // Buscar información del usuario
                var searchFilter = $"(sAMAccountName={username})";
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
                    return (false, null, "User not found.", 404); // User not found
                }
            }
        }
        catch (LdapException ex)
        {
            _logger.LogError($"LDAP error: {ex.Message}");
            return (false, null, ex.Message, 500); // Internal server error
        }
        catch (Exception ex)
        {
            _logger.LogError($"General error: {ex.Message}");
            return (false, null, ex.Message, 500); // General error
        }
    }
}