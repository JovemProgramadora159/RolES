namespace RolES.Backend.Excecoes;

public class RequisicaoInvalidaException(string message) : ExcecaoBase(message, 400);