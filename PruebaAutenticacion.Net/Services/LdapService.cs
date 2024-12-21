using System.DirectoryServices.Protocols;
using System.Net;
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

    public (bool IsAuthenticated, Dictionary<string, string> UserInfo, string ErrorMessage) AuthenticateUser(string username, string password)
    {
        string ldapServer = _configuration["LDAP:Server"];
        int ldapPort = int.Parse(_configuration["LDAP:Port"]);
        string baseDn = _configuration["LDAP:BaseDn"];
        string ldapUser = $"{username}@example.com"; // Ajusta el formato según tu configuración LDAP

        try
        {
            using (var ldapConnection = new LdapConnection(new LdapDirectoryIdentifier(ldapServer, ldapPort)))
            {
                ldapConnection.Credential = new NetworkCredential(ldapUser, password);
                ldapConnection.AuthType = AuthType.Basic;

                // Intenta autenticar
                ldapConnection.Bind();

                // Busca información del usuario
                var searchRequest = new SearchRequest(
                    baseDn,
                    $"(sAMAccountName={username})",
                    SearchScope.Subtree,
                    "displayName", "mail", "department", "title");

                var searchResponse = (SearchResponse)ldapConnection.SendRequest(searchRequest);

                if (searchResponse.Entries.Count > 0)
                {
                    var entry = searchResponse.Entries[0];
                    var userInfo = new Dictionary<string, string>
                    {
                        { "username", username },
                        { "displayName", entry.Attributes["displayName"]?[0]?.ToString() ?? string.Empty },
                        { "email", entry.Attributes["mail"]?[0]?.ToString() ?? string.Empty },
                        { "department", entry.Attributes["department"]?[0]?.ToString() ?? string.Empty },
                        { "title", entry.Attributes["title"]?[0]?.ToString() ?? string.Empty }
                    };

                    return (true, userInfo, string.Empty); // Autenticación exitosa
                }
                else
                {
                    _logger.LogWarning($"User {username} not found in LDAP.");
                    return (false, null, "User not found in LDAP.");
                }
            }
        }
        catch (LdapException ex)
        {
            _logger.LogError($"LDAP error during authentication: {ex.Message}");
            return (false, null, $"LDAP error: {ex.Message}"); // Error de LDAP
        }
        catch (Exception ex)
        {
            _logger.LogError($"General error: {ex.Message}");
            return (false, null, $"General error: {ex.Message}"); // Error general
        }
    }
}
