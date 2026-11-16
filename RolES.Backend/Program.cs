using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MySql.Data.MySqlClient;
using RolES.Backend.Dados;
using RolES.Backend.DTOs;
using RolES.Backend.Modelos;
using RolES.Backend.Servicos;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

// Cadastro de Serviços Customizados
builder.Services.AddScoped<BdConexao>();
builder.Services.AddScoped<UsuarioServicos>();
builder.Services.AddScoped<TokenServicos>();

// Cadastro de JWT Bearer
builder.Services.AddAuthentication(modelos =>
{
    modelos.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    modelos.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(configuracaoJwt =>
{
    var configuracaoApp = builder.Configuration;

    configuracaoJwt.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = configuracaoApp["JWT_EMISSOR"],
        ValidateAudience = true,
        ValidAudience = configuracaoApp["JWT_AUDIENCIA"],
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuracaoApp["JWT_CHAVE"]!))
    };

    configuracaoJwt.Events = new JwtBearerEvents
    {
        OnMessageReceived = contexto =>
        {
            contexto.Token = contexto.Request.Cookies["token_acesso"];
            return Task.CompletedTask;
        }
    };
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();
else
    app.UseHttpsRedirection();

app.MapGet("/", () => // Metodo sem nome
{
    var dataResposta = DateTime.UtcNow;
    return Results.Ok(new
    {
        Mensagem = "Servidor está online.",
        Data = dataResposta
    });
});

app.MapPost("/usuario", async (CadastroUsuarioDto corpoRequisicao, UsuarioServicos uServicos) =>
{
    try
    {
        var novoUsuario = new Usuario(corpoRequisicao.Nome,
            corpoRequisicao.Email,
            corpoRequisicao.Senha,
            corpoRequisicao.Nascimento);
        var linhasAfetadas = await uServicos.AdicionarAsync(novoUsuario);

        if (linhasAfetadas > 0) return Results.Created();

        return Results.Conflict(new
            { Mensagem = "Houve um erro ao tentar criar o seu usuário. Tente novamente mais tarde." });
    }
    catch (MySqlException exception)
    {
        if (exception.Number == 1062)
            return Results.BadRequest(new
            {
                Mensagem = "O e-mail do usuário já consta cadastrado."
            });
        throw;
    }
    catch (ArgumentException exception)
    {
        return Results.BadRequest(new
        {
            Mensagem = exception.Message
        });
    }
    catch(Exception exception)
    {
        Console.WriteLine(exception.Message);
        return Results.InternalServerError();
    }
});

app.MapPost("/login", async (LoginUsuarioDto corpoRequisicao,
    HttpContext httpContext,
    UsuarioServicos uServicos,
    TokenServicos tServicos) =>
{
    try
    {
        var usuarioEncontrado = await uServicos.BuscarPorEmailAsync(corpoRequisicao.Email);

        if (usuarioEncontrado == null)
            return Results.NotFound(new
            {
                Mensagem = "O usuário não foi encontrado."
            });

        if (corpoRequisicao.Senha != usuarioEncontrado.Senha)
            return Results.BadRequest(new
            {
                Mensagem = "O email/senha estão incorretos."
            });

        var tokenAcesso = tServicos.GerarToken(usuarioEncontrado);
        var tokenAtualizacao = tServicos.GerarRefreshToken();

        var opcoesCookie = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            IsEssential = true,
            Expires = DateTime.UtcNow.AddDays(7),
            SameSite = SameSiteMode.Strict
        };

        httpContext.Response.Cookies.Append("token_atualizacao", tokenAtualizacao, opcoesCookie);

        opcoesCookie.Expires = DateTime.UtcNow.AddMinutes(15);

        httpContext.Response.Cookies.Append("token_acesso", tokenAcesso, opcoesCookie);

        return Results.Ok();
    }
    catch
    {
        return Results.InternalServerError();
    }
});

app.Run();