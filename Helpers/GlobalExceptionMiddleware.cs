using System.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace LionttoMoveis.Helpers
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                var traceId = Activity.Current?.Id ?? context.TraceIdentifier;

                _logger.LogError(
                    ex,
                    "Unhandled exception. TraceId: {TraceId}. Method: {Method}. Path: {Path}. QueryString: {QueryString}",
                    traceId,
                    context.Request.Method,
                    context.Request.Path,
                    context.Request.QueryString.Value);

                if (context.Response.HasStarted)
                    throw;

                context.Response.Clear();
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;

                if (IsApiRequest(context.Request))
                {
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsJsonAsync(new
                    {
                        error = "Ocorreu um erro interno. Tente novamente em instantes.",
                        traceId
                    });

                    return;
                }

                context.Response.ContentType = "text/html; charset=utf-8";
                var html = $@"
<!doctype html>
<html lang=""pt-BR"">
<head>
  <meta charset=""utf-8"" />
  <meta name=""viewport"" content=""width=device-width, initial-scale=1"" />
  <title>Erro interno</title>
  <style>
    body {{ font-family: 'Segoe UI', Arial, sans-serif; background:#f6f7fb; color:#1f2937; margin:0; }}
    .wrap {{ min-height:100vh; display:flex; align-items:center; justify-content:center; padding:24px; }}
    .card {{ max-width:680px; width:100%; background:#fff; border:1px solid #e5e7eb; border-radius:16px; padding:28px; box-shadow:0 12px 30px rgba(0,0,0,.08); }}
    h1 {{ margin:0 0 10px; font-size:26px; }}
    p {{ margin:0 0 12px; line-height:1.5; }}
    .trace {{ margin-top:18px; font-size:13px; color:#6b7280; }}
    a {{ display:inline-block; margin-top:16px; padding:10px 16px; border-radius:10px; background:#111827; color:#fff; text-decoration:none; font-weight:600; }}
  </style>
</head>
<body>
  <main class=""wrap"">
    <section class=""card"">
      <h1>Nao foi possivel concluir esta operacao</h1>
      <p>Nos ja registramos este erro e vamos investigar.</p>
      <p>Tente novamente em alguns instantes.</p>
      <a href=""/"">Voltar ao dashboard</a>
      <div class=""trace"">Codigo de rastreio: {traceId}</div>
    </section>
  </main>
</body>
</html>";
                await context.Response.WriteAsync(html);
            }
        }

        private static bool IsApiRequest(HttpRequest request)
        {
            if (request.Path.StartsWithSegments("/api"))
                return true;

            var accepts = request.Headers.Accept;
            var acceptsJson = accepts.Count > 0 &&
                accepts.Any(h => h?.Contains("application/json", StringComparison.OrdinalIgnoreCase) == true);
            var requestedWith = request.Headers["X-Requested-With"].ToString();

            return acceptsJson || string.Equals(requestedWith, "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);
        }
    }
}

