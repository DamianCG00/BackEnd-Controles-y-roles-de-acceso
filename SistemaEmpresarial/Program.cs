using Microsoft.EntityFrameworkCore;
using SistemaEmpresarial.Data;
using SistemaEmpresarial.GraphQL;
using SistemaEmpresarial.Acceso;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// 1. Configurar PostgreSQL
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. Configurar GraphQL (HotChocolate)
builder.Services.AddHttpContextAccessor();

builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddMutationType<Mutation>()
    .ModifyRequestOptions(options =>
        options.IncludeExceptionDetails = true
    );

var app = builder.Build();

// Crear la base de datos automáticamente si no existe (ideal para desarrollo rápido)
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    context.Database.EnsureCreated();

    // Roles, permisos y matriz inicial (no duplica si ya existen)
    AccessSeeder.Seed(context);
}

app.UseHttpsRedirection();

// Para las pruebas de la actividad, X-User-Id identifica al usuario autenticado.
// Esto es un mecanismo de desarrollo para la actividad, no una autenticación de producción.
app.Use(async (httpContext, next) =>
{
    var userId = httpContext.Request.Headers["X-User-Id"].FirstOrDefault();

    if (!string.IsNullOrWhiteSpace(userId))
    {
        var identity = new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.NameIdentifier, userId) },
            authenticationType: "ActivityDevAuth");

        httpContext.User = new ClaimsPrincipal(identity);
    }

    await next();
});

// 3. Habilitar el endpoint de GraphQL
app.MapGraphQL();

app.Run();
