using RolES.Backend.DTOs;

namespace RolES.Backend.Modelos;

public class Usuario
{
    public int Id { get; private set; }

    public string Nome { get; private set; }

    public string Senha { get; private set; }
    public string Email { get; private set; }
    public DateOnly Nascimento { get; private set; }

    // Construtor para novos cadastros
    public Usuario(string nome, string email, string senha, DateOnly nascimento)
    {
        Validar(nome, email, senha);

        Nome = nome;
        Email = email;
        Senha = senha;
        Nascimento = nascimento;
    }

    // Construtor para carregar do banco de dados
    public Usuario(int id, string nome, string email, string senha, DateOnly nascimento)
    {
        Id = id;
        Nome = nome;
        Email = email;
        Senha = senha;
        Nascimento = nascimento;
    }

    private static void Validar(string nome, string email, string senha)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new ArgumentException("O nome do usuário é obrigatório.", nome);
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("O e-mail do usuário é obrigatório.", email);
        if (string.IsNullOrWhiteSpace(senha))
            throw new ArgumentException("A senha do usuário é obrigatório.", senha);
        
    }
}