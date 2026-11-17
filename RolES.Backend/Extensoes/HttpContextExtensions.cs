using RolES.Backend.Modelos;
using RolES.Backend.Servicos;

namespace RolES.Backend.Extensoes;

public static class HttpContextExtensions
{
    public static async Task EmitirSessaoAutenticacaoAsync(
        this HttpContext httpContext,
        Usuario usuario,
        TokenServicos tServicos,
        bool isDevelopment)
    {
        var tokenAcesso = tServicos.GerarToken(usuario);
        var tokenAtualizacao = tServicos.GerarRefreshToken();
        var expiracaoRefreshToken = DateTime.UtcNow.AddDays(7);

        await tServicos.SalvarRefreshTokenAsync(usuario.Id, tokenAtualizacao, expiracaoRefreshToken);

        var opcoesCookie = new CookieOptions
        {
            HttpOnly = true,
            Secure = !isDevelopment,
            IsEssential = true,
            Expires = expiracaoRefreshToken,
            SameSite = SameSiteMode.Strict
        };
        httpContext.Response.Cookies.Append("token_atualizacao", tokenAtualizacao, opcoesCookie);

        opcoesCookie.Expires = DateTime.UtcNow.AddMinutes(15);
        httpContext.Response.Cookies.Append("token_acesso", tokenAcesso, opcoesCookie);
    }
}