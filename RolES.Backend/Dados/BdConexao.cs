using MySql.Data.MySqlClient;

namespace RolES.Backend.Dados;

public class BdConexao(IConfiguration configuracao)
{
    private readonly string _stringConexao = configuracao["MYSQL_URI"] ?? throw new ArgumentNullException("MYSQL_URI",
        "A variável de ambiente MYSQL_URI não foi informada.");

    public async Task<MySqlConnection> CriarConexaoAsync()
    {
        var mySqlConexao = new MySqlConnection(_stringConexao);
        await mySqlConexao.OpenAsync();
        return mySqlConexao;
    }
}