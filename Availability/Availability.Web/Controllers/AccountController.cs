using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Availability.Web.Controllers;

public class AccountController : Controller
{
    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        // Validamos el destino para impedir redirecciones
        // a sitios externos enviados por el usuario.
        var destino = Url.IsLocalUrl(returnUrl)
            ? returnUrl!
            : Url.Content("~/");

        return Challenge(
            new AuthenticationProperties
            {
                RedirectUri = destino
            },
            OpenIdConnectDefaults.AuthenticationScheme);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        // Cerramos la sesión del MVC y la sesión de Keycloak.
        return SignOut(
            new AuthenticationProperties
            {
                RedirectUri = Url.Content("~/")
            },
            CookieAuthenticationDefaults.AuthenticationScheme,
            OpenIdConnectDefaults.AuthenticationScheme);
    }
}