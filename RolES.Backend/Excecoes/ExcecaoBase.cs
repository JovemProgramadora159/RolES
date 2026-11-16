namespace RolES.Backend.Excecoes;

public abstract class ExcecaoBase : Exception
{
    public int StatusCode { get; }

    protected ExcecaoBase(string message, int statusCode) : base(message)
    {
        StatusCode = statusCode;
    }
}