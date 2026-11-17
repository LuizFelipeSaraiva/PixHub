using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace PixHub.BuildingBlocks.Api;

/// <summary>
/// Cabeçalhos de resposta que mitigam classes inteiras de ataque (clickjacking, MIME-sniffing,
/// vazamento de referrer) sem custo de implementação — parte do checklist OWASP Secure Headers.
/// Escrito à mão em vez de um pacote de terceiros: são ~6 linhas, e evita depender de mais uma lib
/// externa para algo que não muda (mesmo espírito de license-awareness da ADR-0002).
/// </summary>
public static class SecurityHeadersMiddlewareExtensions
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            var headers = context.Response.Headers;

            // Nunca deixa o navegador "adivinhar" um content-type diferente do declarado.
            headers["X-Content-Type-Options"] = "nosniff";

            // Impede que a resposta seja renderizada dentro de um <iframe> de outra origem (clickjacking).
            headers["X-Frame-Options"] = "DENY";

            // Nunca envia a URL de origem para outra origem — essas Apis não servem HTML navegável,
            // então não há motivo legítimo para vazar a URL de uma chamada para outra.
            headers["Referrer-Policy"] = "no-referrer";

            // Essas Apis não servem HTML/JS de terceiros; uma CSP restritiva documenta a intenção e
            // blinda contra XSS refletido caso um endpoint futuro passe a ecoar entrada do cliente.
            headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'";

            // HSTS só faz sentido quando a conexão já é HTTPS — em HTTP simples (dev local) o header
            // seria ignorado pelo navegador de qualquer forma, mas evitamos emiti-lo à toa.
            if (context.Request.IsHttps)
            {
                headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
            }

            await next(context);
        });
    }
}
