using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using RolES.Backend.Dados;
using RolES.Backend.DTOs;
using RolES.Backend.Excecoes;
using RolES.Backend.Extensoes;
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
builder.Services.AddAuthorization();

// Adiciona middleware de excecoes
builder.Services.AddExceptionHandler<ManipuladorGlobalExcecoes>();
builder.Services.AddProblemDetails();

var app = builder.Build();

if (app.Environment.IsDevelopment()) app.MapOpenApi();
else app.UseHttpsRedirection();

app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();

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

    if (usuarioEncontrado == null || corpoRequisicao.Senha != usuarioEncontrado.Senha)
        throw new RequisicaoInvalidaException("E-mail ou senha incorretos.");

    await httpContext.EmitirSessaoAutenticacaoAsync(usuarioEncontrado, tServicos, app.Environment.IsDevelopment());

    return Results.Ok();
});

app.MapPost("/refresh", async (HttpContext httpContext, UsuarioServicos uServicos, TokenServicos tServicos) =>
{
    if (!httpContext.Request.Cookies.TryGetValue("token_atualizacao", out var refreshTokenAntigo) ||
        string.IsNullOrEmpty(refreshTokenAntigo))
    {
        throw new RequisicaoInvalidaException("Token de atualização ausente.");
    }

    var usuarioId = await tServicos.ValidarRefreshTokenAsync(refreshTokenAntigo);
    if (usuarioId == null)
    {
        throw new RequisicaoInvalidaException("Token de atualização inválido ou expirado.");
    }

    var usuario = await uServicos.BuscarPorIdAsync(usuarioId.Value);
    if (usuario == null)
    {
        throw new NaoEncontradoException("Usuário não encontrado.");
    }

    await tServicos.RevogarRefreshTokenAsync(refreshTokenAntigo);

    await httpContext.EmitirSessaoAutenticacaoAsync(usuario, tServicos, app.Environment.IsDevelopment());
    return Results.Ok();
});

app.MapGet("/eu", (ClaimsPrincipal usuario) =>
    {
        var id = usuario.FindFirstValue(ClaimTypes.NameIdentifier);
        var nome = usuario.FindFirstValue(ClaimTypes.Name);
        var email = usuario.FindFirstValue(ClaimTypes.Email);

        return Results.Ok(new
        {
            Id = id,
            Nome = nome,
            Email = email
        });
    })
    .RequireAuthorization();

app.Run();