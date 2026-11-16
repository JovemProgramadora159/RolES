using Microsoft.AspNetCore.Diagnostics;
using MySql.Data.MySqlClient;

namespace RolES.Backend.Excecoes;

public class ManipuladorGlobalExcecoes(ILogger<ManipuladorGlobalExcecoes> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, mensagem) = exception switch
        {
            ExcecaoBase custom => (custom.StatusCode, custom.Message),
            MySqlException { Number: 1062 } => (StatusCodes.Status400BadRequest,
                "O e-mail do usuário já consta cadastrado."),
            _ => (StatusCodes.Status500InternalServerError, "Ocorreu um erro interno no servidor.")
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
            logger.LogError(exception, "Erro crítico não tratado capturado pelo middleware global.");

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(new { Mensagem = mensagem }, cancellationToken);
        return true;
    }
}