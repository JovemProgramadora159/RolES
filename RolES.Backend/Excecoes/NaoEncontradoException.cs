namespace RolES.Backend.Excecoes;

public class NaoEncontradoException(string message) : ExcecaoBase(message, 404);