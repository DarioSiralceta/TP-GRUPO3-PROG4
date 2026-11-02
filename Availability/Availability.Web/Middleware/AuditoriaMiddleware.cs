using System.Diagnostics;

namespace Availability.Web.Middleware;

public class AuditoriaMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuditoriaMiddleware> _logger;

    public AuditoriaMiddleware(
        RequestDelegate next,
        ILogger<AuditoriaMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // La correlación permite relacionar el inicio, los eventos
        // intermedios y el fin de una misma petición.
        var correlacionId = Activity.Current?.TraceId.ToString()
            ?? context.TraceIdentifier;

        var cronometro = Stopwatch.StartNew();
        var huboExcepcion = false;

        context.Response.Headers["X-Correlation-ID"] = correlacionId;

        _logger.LogInformation(
            "AUDITORIA INICIO Componente={Componente} " +
            "CorrelacionId={CorrelacionId} Metodo={Metodo} Ruta={Ruta}",
            "Availability.Web",
            correlacionId,
            context.Request.Method,
            context.Request.Path.Value);

        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            huboExcepcion = true;

            _logger.LogError(
                ex,
                "AUDITORIA ERROR Componente={Componente} " +
                "CorrelacionId={CorrelacionId}",
                "Availability.Web",
                correlacionId);

            throw;
        }
        finally
        {
            // Registramos el fin incluso si ocurre una excepción.
            // No registramos cuerpos de peticiones, tokens ni contraseñas.
            cronometro.Stop();

            var codigoEstado = huboExcepcion
                ? StatusCodes.Status500InternalServerError
                : context.Response.StatusCode;

            _logger.LogInformation(
                "AUDITORIA FIN Componente={Componente} " +
                "CorrelacionId={CorrelacionId} " +
                "CodigoEstado={CodigoEstado} DuracionMs={DuracionMs}",
                "Availability.Web",
                correlacionId,
                codigoEstado,
                cronometro.ElapsedMilliseconds);
        }
    }
}