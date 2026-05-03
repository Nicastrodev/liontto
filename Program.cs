using LionttoMoveis.Data;
using LionttoMoveis.Repository;
using LionttoMoveis.Services;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews(options =>
{
    options.ModelBindingMessageProvider.SetValueMustNotBeNullAccessor(_ => "Preencha este campo.");
    options.ModelBindingMessageProvider.SetValueIsInvalidAccessor(_ => "Valor invalido para o campo informado.");
    options.ModelBindingMessageProvider.SetAttemptedValueIsInvalidAccessor((_, campo) => $"Preencha o campo {campo}.");
});

// ❌ CORS removido (não precisa se front/back estão juntos)

// =======================
// 🔥 MYSQL CONFIG (SIMPLES E ROBUSTO)
// =======================

var connectionString = builder.Configuration["MYSQL_URL"];

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new Exception("MYSQL_URL não encontrada nas variáveis de ambiente.");
}

// Converte mysql:// para formato .NET
connectionString = ConvertMySqlUrlToConnectionString(connectionString);

Console.WriteLine($"[config] MySQL conectado via MYSQL_URL");

// 🔥 DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(
        connectionString,
        new MySqlServerVersion(new Version(8, 0, 0)),
        mySqlOptions => mySqlOptions.EnableRetryOnFailure(5)
    )
);

// Repositórios
builder.Services.AddScoped<MaterialRepository>();
builder.Services.AddScoped<ClienteRepository>();
builder.Services.AddScoped<ProdutoRepository>();
builder.Services.AddScoped<PedidoRepository>();
builder.Services.AddScoped<MovimentacaoRepository>();

// Serviços
builder.Services.AddScoped<EstoqueService>();
builder.Services.AddScoped<SeedService>();

builder.Services.AddSession();

var app = builder.Build();

// =======================
// 🔥 MIDDLEWARE
// =======================

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();
app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// =======================
// 🔥 INIT DATABASE (SAFE)
// =======================

using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // ⚠️ NÃO usar migrate automático em produção ainda
        db.Database.EnsureCreated();

        Console.WriteLine("[DB] Banco conectado com sucesso");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[ERROR] Falha ao conectar no banco: {ex.Message}");
        // ❗ NÃO derruba o app
    }
}

Console.WriteLine("🚀 Liontto Moveis rodando!");
app.Run();


// =======================
// 🔥 HELPER
// =======================

static string ConvertMySqlUrlToConnectionString(string mysqlUrl)
{
    var uri = new Uri(mysqlUrl);

    var userInfo = uri.UserInfo.Split(':');
    var user = userInfo[0];
    var password = userInfo.Length > 1 ? userInfo[1] : "";

    var database = uri.AbsolutePath.Trim('/');

    var builder = new MySqlConnectionStringBuilder
    {
        Server = uri.Host,
        Port = (uint)(uri.IsDefaultPort ? 3306 : uri.Port),
        UserID = user,
        Password = password,
        Database = database,
        CharacterSet = "utf8mb4",
        SslMode = MySqlSslMode.Preferred
    };

    return builder.ConnectionString;
}