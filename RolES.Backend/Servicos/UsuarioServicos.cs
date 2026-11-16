using System.Globalization;
using MySql.Data.MySqlClient;
using RolES.Backend.Dados;
using RolES.Backend.Modelos;

namespace RolES.Backend.Servicos;

public class UsuarioServicos(BdConexao conexao)
{
    public async Task<int> AdicionarAsync(Usuario usuario)
    {
        const string sql =
            "INSERT INTO usuarios(nome, email, nascimento, senha, cargo) VALUES(@nome, @email, @nascimento, @senha, 'comum')";

        var mySqlComando = new MySqlCommand(sql, await conexao.CriarConexaoAsync());

        mySqlComando.Parameters.AddWithValue("@nome", usuario.Nome);
        mySqlComando.Parameters.AddWithValue("@email", usuario.Email);
        mySqlComando.Parameters.AddWithValue("@nascimento",
            usuario.Nascimento.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        mySqlComando.Parameters.AddWithValue("@senha", usuario.Senha);

        var linhasAfetadas = await mySqlComando.ExecuteNonQueryAsync();

        return linhasAfetadas;
    }

    public async Task<Usuario?> BuscarPorEmailAsync(string emailBuscado)
    {
        const string sql = "SELECT id, nome, email, nascimento, senha FROM usuarios WHERE email = @email";

        var mySqlComando = new MySqlCommand(sql, await conexao.CriarConexaoAsync());

        mySqlComando.Parameters.AddWithValue("@email", emailBuscado);

        var leitor = await mySqlComando.ExecuteReaderAsync();

        if (!await leitor.ReadAsync()) return null;

        var usuarioEncontrado = new Usuario(
            leitor.GetInt32(leitor.GetOrdinal("id")),
            leitor["nome"].ToString() ?? string.Empty,
            leitor["email"].ToString() ?? string.Empty,
            leitor["senha"].ToString() ?? string.Empty,
            DateOnly.FromDateTime(leitor.GetDateTime(leitor.GetOrdinal("nascimento")))
        );

        return usuarioEncontrado;
    }
}