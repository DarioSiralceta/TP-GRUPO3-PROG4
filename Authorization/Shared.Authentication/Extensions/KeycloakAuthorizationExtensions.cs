
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Shared.Authentication.Extensions;

public static class KeycloakAuthorizationExtensions
{
    public static IServiceCollection AddKeycloakAuthorization(
        this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy("CanReadBooking", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireAssertion(context =>
                    HasRealmRole(context, "booking.read"));
            });

            options.AddPolicy("CanProcessPayment", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireAssertion(context =>
                    HasRealmRole(context, "payments.process"));
            });
        });

        return services;
    }

    private static bool HasRealmRole(
        AuthorizationHandlerContext context,
        string role)
    {
        var realmAccess = context.User.FindFirst("realm_access");

        if (realmAccess is null)
            return false;

        try
        {
            using var document =
                System.Text.Json.JsonDocument.Parse(realmAccess.Value);

            var roles = document.RootElement.GetProperty("roles");

            return roles.EnumerateArray()
                .Any(r => r.GetString() == role);
        }
        catch (System.Text.Json.JsonException)
        {
            return false;
        }
        catch (KeyNotFoundException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }
}
