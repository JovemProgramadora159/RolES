using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using RolES.Backend.Modelos;

namespace RolES.Backend.Servicos;

public class TokenServicos
{
    private readonly string _chave;

    private readonly string _emissor;
    private readonly string _audiencia;

    public TokenServicos(IConfiguration configuracao)
    {
        _chave = configuracao["JWT_CHAVE"] ??
                 throw new ArgumentNullException(configuracao["JWT_CHAVE"],
                     "A chave JWT não está configurada corretamente!");
        _emissor = configuracao["JWT_EMISSOR"] ??
                   throw new ArgumentNullException(configuracao["JWT_EMISSOR"],
                       "O emissor JWT não está configurado corretamente!");
        _audiencia = configuracao["JWT_AUDIENCIA"] ??
                     throw new ArgumentNullException(configuracao["JWT_AUDIENCIA"],
                         "A audiencia JWT não está configurada corretamente!");
    }


    public string GerarToken(Usuario usuario)
    {
        var manipuladorToken = new JwtSecurityTokenHandler();

        var chaveJwtBytes = Encoding.UTF8.GetBytes(_chave);

        var descricaoToken = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity([
                new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                new Claim(ClaimTypes.Email, usuario.Email)
            ]),
            Issuer = _emissor,
            Audience = _audiencia,
            Expires = DateTime.UtcNow.AddMinutes(15),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(chaveJwtBytes),
                SecurityAlgorithms.HmacSha256Signature
            )
        };

        var token = manipuladorToken.CreateToken(descricaoToken);

        return manipuladorToken.WriteToken(token);
    }

    public string GerarRefreshToken()
    {
        var numeroAleatorio = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(numeroAleatorio);
        return Convert.ToBase64String(numeroAleatorio);
    }
}   