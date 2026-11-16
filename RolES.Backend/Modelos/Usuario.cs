using RolES.Backend.DTOs;
using RolES.Backend.Excecoes;

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
            throw new RequisicaoInvalidaException("O nome do usuário é obrigatório.");
        if (string.IsNullOrWhiteSpace(email))
            throw new RequisicaoInvalidaException("O e-mail do usuário é obrigatório.");
        if (string.IsNullOrWhiteSpace(senha))
            throw new RequisicaoInvalidaException("A senha do usuário é obrigatório.");
        
    }
}