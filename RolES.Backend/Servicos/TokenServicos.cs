using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using MySql.Data.MySqlClient;
using RolES.Backend.Dados;
using RolES.Backend.Modelos;

namespace RolES.Backend.Servicos;

public class TokenServicos(IConfiguration configuracao, BdConexao conexao)
{
    private readonly string _chave = configuracao["JWT_CHAVE"] ??
                                     throw new ArgumentNullException(configuracao["JWT_CHAVE"],
                                         "A chave JWT não está configurada corretamente!");

    private readonly string _emissor = configuracao["JWT_EMISSOR"] ??
                                       throw new ArgumentNullException(configuracao["JWT_EMISSOR"],
                                           "O emissor JWT não está configurado corretamente!");

    private readonly string _audiencia = configuracao["JWT_AUDIENCIA"] ??
                                         throw new ArgumentNullException(configuracao["JWT_AUDIENCIA"],
                                             "A audiencia JWT não está configurada corretamente!");


    public string GerarToken(Usuario usuario)
    {
        var manipuladorToken = new JwtSecurityTokenHandler();

        var chaveJwtBytes = Encoding.UTF8.GetBytes(_chave);

        var descricaoToken = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity([
                new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                new Claim(ClaimTypes.Name, usuario.Nome),
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

    public async Task SalvarRefreshTokenAsync(int usuarioId, string token, DateTime expiracao)
    {
        const string sql =
            "INSERT INTO tokens_atualizacao(usuario_id, token, data_expiracao, revogado) VALUES (@usuarioId, @token, @expiracao, 0)";

        await using var conexaoDb = await conexao.CriarConexaoAsync();
        await using var comando = new MySqlCommand(sql, conexaoDb);

        comando.Parameters.AddWithValue("@usuarioId", usuarioId);
        comando.Parameters.AddWithValue("@token", token);
        comando.Parameters.AddWithValue("@expiracao",
            expiracao.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));

        await comando.ExecuteNonQueryAsync();
    }

    public async Task<int?> ValidarRefreshTokenAsync(string token)
    {
        const string sql = """
                           SELECT usuario_id, data_expiracao, revogado 
                           FROM tokens_atualizacao
                           WHERE token = @token
                           """;
        await using var conexaoDb = await conexao.CriarConexaoAsync();
        await using var comando = new MySqlCommand(sql, conexaoDb);
        comando.Parameters.AddWithValue("@token", token);

        await using var leitor = await comando.ExecuteReaderAsync();

        if (!await leitor.ReadAsync()) return null;

        var revogado = leitor.GetBoolean(leitor.GetOrdinal("revogado"));
        var expiracao = leitor.GetDateTime(leitor.GetOrdinal("data_expiracao"));
        var usuarioId = leitor.GetInt32(leitor.GetOrdinal("usuario_id"));

        if (revogado || expiracao <= DateTime.UtcNow) return null;

        return usuarioId;
    }

    public async Task RevogarRefreshTokenAsync(string token)
    {
        const string sql = "UPDATE tokens_atualizacao SET revogado = 1 WHERE token = @token";

        await using var conexaoDb = await conexao.CriarConexaoAsync();
        await using var comando = new MySqlCommand(sql, conexaoDb);
        comando.Parameters.AddWithValue("@token", token);

        await comando.ExecuteNonQueryAsync();
    }
}