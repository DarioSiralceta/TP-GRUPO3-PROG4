
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Shared.Authentication.Extensions;

public static class KeycloakAuthenticationExtensions
{
    //Primer metodo configura la autenticación JWT: emisor, audiencia, validación de firma y vencimiento.
    public static IServiceCollection AddKeycloakAuthentication(
        this IServiceCollection services,
        string authority,
        string audience)
    {
        services.AddAuthentication(
            JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = authority;
                options.Audience = audience;

                // Solo para nuestro Keycloak local.
                // options.RequireHttpsMetadata = false;
                // En un despliegue real se debe exigir HTTPS.
                options.RequireHttpsMetadata = authority.StartsWith(
                     "https://",
                        StringComparison.OrdinalIgnoreCase
                 );

                options.TokenValidationParameters =
                    new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true
                    };
            });

        services.AddAuthorization();

        return services;
    }

    //segundo metodo Lee los valores de appsettings.json, variables de entorno o User Secrets y llama al primer método.
    public static IServiceCollection AddKeycloakAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        string authority = configuration["Keycloak:Authority"]
            ?? throw new InvalidOperationException(
                "Falta configurar Keycloak:Authority");

        string audience = configuration["Keycloak:Audience"]
            ?? throw new InvalidOperationException(
                "Falta configurar Keycloak:Audience");

        return services.AddKeycloakAuthentication(
            authority,
            audience
        );
    }

}
