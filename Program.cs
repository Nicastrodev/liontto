// =============================================================
// Program.cs — Ponto de entrada da aplicação ASP.NET Core
// Configura EF Core + MySQL (XAMPP) em vez de MongoDB
// =============================================================

using Microsoft.EntityFrameworkCore;
using LionttoMoveis.Data;
using LionttoMoveis.Repository;
using LionttoMoveis.Services;

var builder = WebApplication.CreateBuilder(args);

// ── 1. MVC com Views Razor ──────────────────────────────────
builder.Services.AddControllersWithViews(options =>
{
    options.ModelBindingMessageProvider.SetValueMustNotBeNullAccessor(_ => "Preencha este campo.");
    options.ModelBindingMessageProvider.SetValueIsInvalidAccessor(_ => "Valor invalido para o campo informado.");
    options.ModelBindingMessageProvider.SetAttemptedValueIsInvalidAccessor((_, campo) => $"Preencha o campo {campo}.");
});

// ── 2. Conexão com MySQL via EF Core (Pomelo) ───────────────
var connStr = builder.Configuration.GetConnectionString("MySQL");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(
        connStr,
        ServerVersion.AutoDetect(connStr),
        mySqlOptions => mySqlOptions.EnableRetryOnFailure(3)
    )
);

// ── 3. Repositórios ─────────────────────────────────────────
builder.Services.AddScoped<MaterialRepository>();
builder.Services.AddScoped<ClienteRepository>();
builder.Services.AddScoped<ProdutoRepository>();
builder.Services.AddScoped<PedidoRepository>();
builder.Services.AddScoped<MovimentacaoRepository>();

// ── 4. Services ─────────────────────────────────────────────
builder.Services.AddScoped<EstoqueService>();
builder.Services.AddScoped<SeedService>();

// ── 5. Session ──────────────────────────────────────────────
builder.Services.AddSession();

var app = builder.Build();

// ── 6. Pipeline HTTP ────────────────────────────────────────
// Sempre mostra página de erro detalhada (útil em desenvolvimento)
app.UseDeveloperExceptionPage();

app.UseStaticFiles();
app.UseRouting();
app.UseSession();

app.MapControllerRoute(
    name:    "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// ── 7. Cria tabelas e seed ───────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();

    var seed = scope.ServiceProvider.GetRequiredService<SeedService>();
    await seed.SeedAsync();
}

Console.WriteLine("🪑  Líontto Móveis — ASP.NET Core + MySQL (XAMPP)");
Console.WriteLine("🌐  Acesse: http://localhost:5000");

app.Run();
