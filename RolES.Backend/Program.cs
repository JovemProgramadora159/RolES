using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using RolES.Backend.Dados;
using RolES.Backend.DTOs;
using RolES.Backend.Excecoes;
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

// Adiciona middleware de excecoes
builder.Services.AddExceptionHandler<ManipuladorGlobalExcecoes>();
builder.Services.AddProblemDetails();

var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();
else
    app.UseHttpsRedirection();

app.UseExceptionHandler();

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
    var novoUsuario = new Usuario(corpoRequisicao.Nome,
        corpoRequisicao.Email,
        corpoRequisicao.Senha,
        corpoRequisicao.Nascimento);
    var linhasAfetadas = await uServicos.AdicionarAsync(novoUsuario);

    if (linhasAfetadas > 0) return Results.Created();

    return Results.Conflict(new
        { Mensagem = "Houve um erro ao tentar criar o seu usuário. Tente novamente mais tarde." });
});

app.MapPost("/login", async (LoginUsuarioDto corpoRequisicao,
    HttpContext httpContext,
    UsuarioServicos uServicos,
    TokenServicos tServicos) =>
{
    var usuarioEncontrado = await uServicos.BuscarPorEmailAsync(corpoRequisicao.Email);

    if (usuarioEncontrado == null)
        throw new NaoEncontradoException("O usuário não foi encontrado.");

    if (corpoRequisicao.Senha != usuarioEncontrado.Senha)
        throw new RequisicaoInvalidaException("O email/senha estão incorretos.");

    var tokenAcesso = tServicos.GerarToken(usuarioEncontrado);
    var tokenAtualizacao = TokenServicos.GerarRefreshToken();
    var expiracaoRefreshToken = DateTime.UtcNow.AddDays(7);

    var opcoesCookie = new CookieOptions
    {
        HttpOnly = true,
        Secure = true,
        IsEssential = true,
        Expires = expiracaoRefreshToken,
        SameSite = SameSiteMode.Strict
    };
    httpContext.Response.Cookies.Append("token_atualizacao", tokenAtualizacao, opcoesCookie);
    await tServicos.SalvarRefreshTokenAsync(usuarioEncontrado.Id, tokenAtualizacao, expiracaoRefreshToken);

    opcoesCookie.Expires = DateTime.UtcNow.AddMinutes(15);
    httpContext.Response.Cookies.Append("token_acesso", tokenAcesso, opcoesCookie);
    
    return Results.Ok();
});

app.Run();