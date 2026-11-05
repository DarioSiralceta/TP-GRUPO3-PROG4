
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Shared.Authentication.Services;

public class KeycloakTokenService
{
    private readonly HttpClient _httpClient;

    public KeycloakTokenService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<string> GetAccessTokenAsync(
        string authority,
        string clientId,
        string clientSecret,
        CancellationToken cancellationToken = default)
    {
        // Endpoint de Keycloak para solicitar tokens.
        string tokenUrl =
            $"{authority.TrimEnd('/')}/protocol/openid-connect/token";

        // Datos que Keycloak necesita para autenticar al servicio.
        using var form = new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = clientId,
                ["client_secret"] = clientSecret
            });

        // Enviamos la solicitud HTTP POST a Keycloak.
        using var response = await _httpClient.PostAsync(
            tokenUrl,
            form,
            cancellationToken);

        // Si Keycloak devuelve un error, lanzamos una excepción.
        response.EnsureSuccessStatusCode();

        // Convertimos la respuesta JSON a un objeto C#.
        var token = await response.Content.ReadFromJsonAsync<TokenResponse>(
            cancellationToken: cancellationToken);

        if (string.IsNullOrWhiteSpace(token?.AccessToken))
        {
            throw new InvalidOperationException(
                "Keycloak no devolvió un access_token.");
        }

        return token.AccessToken;
    }

    private sealed class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }
    }
}
