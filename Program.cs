using LionttoMoveis.Data;
using LionttoMoveis.Repository;
using LionttoMoveis.Services;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;

var builder = WebApplication.CreateBuilder(args);

// =======================
// MVC
// =======================

builder.Services.AddControllersWithViews(options =>
{
    options.ModelBindingMessageProvider.SetValueMustNotBeNullAccessor(_ => "Preencha este campo.");
    options.ModelBindingMessageProvider.SetValueIsInvalidAccessor(_ => "Valor invalido para o campo informado.");
    options.ModelBindingMessageProvider.SetAttemptedValueIsInvalidAccessor((_, campo) => $"Preencha o campo {campo}.");
});

// =======================
// 🔥 MYSQL (ULTRA SEGURO)
// =======================

var rawMysqlUrl = builder.Configuration["MYSQL_URL"];

if (string.IsNullOrWhiteSpace(rawMysqlUrl))
{
    Console.WriteLine("[ERRO] MYSQL_URL não definida!");
    throw new Exception("MYSQL_URL não encontrada.");
}

string connectionString;

try
{
    connectionString = ConvertMySqlUrlToConnectionString(rawMysqlUrl);
    Console.WriteLine("[DB] MYSQL_URL convertida com sucesso");
}
catch (Exception ex)
{
    Console.WriteLine($"[ERRO] Falha ao converter MYSQL_URL: {ex.Message}");
    throw;
}

// DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(
        connectionString,
        new MySqlServerVersion(new Version(8, 0, 0)),
        mySqlOptions =>
        {
            mySqlOptions.EnableRetryOnFailure(5);
        }
    )
);

// =======================
// DI
// =======================

builder.Services.AddScoped<MaterialRepository>();
builder.Services.AddScoped<ClienteRepository>();
builder.Services.AddScoped<ProdutoRepository>();
builder.Services.AddScoped<PedidoRepository>();
builder.Services.AddScoped<MovimentacaoRepository>();

builder.Services.AddScoped<EstoqueService>();
builder.Services.AddScoped<SeedService>();

builder.Services.AddSession();

var app = builder.Build();

// =======================
// MIDDLEWARE
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
// 🔥 HEALTH CHECK (IMPORTANTE PRO RAILWAY)
// =======================

app.MapGet("/", () => Results.Ok("API Rodando 🚀"));

// =======================
// 🔥 INIT DATABASE (NÃO CRASHA)
// =======================

using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        db.Database.EnsureCreated();

        Console.WriteLine("[DB] Conectado com sucesso");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[ERRO DB] {ex.Message}");
        // NÃO derruba o app
    }
}

Console.WriteLine("🚀 Liontto Moveis rodando!");
app.Run();

// =======================
// 🔥 HELPER
// =======================

static string ConvertMySqlUrlToConnectionString(string mysqlUrl)
{
    if (!Uri.TryCreate(mysqlUrl, UriKind.Absolute, out var uri))
        throw new Exception("MYSQL_URL inválida");

    var userInfo = uri.UserInfo.Split(':', 2);

    var user = userInfo[0];
    var password = userInfo.Length > 1 ? userInfo[1] : "";

    var database = uri.AbsolutePath.Trim('/');

    if (string.IsNullOrWhiteSpace(database))
        throw new Exception("Database não encontrada na URL");

    var builder = new MySqlConnectionStringBuilder
    {
        Server = uri.Host,
        Port = (uint)(uri.IsDefaultPort ? 3306 : uri.Port),
        UserID = user,
        Password = password,
        Database = database,
        CharacterSet = "utf8mb4",
        SslMode = MySqlSslMode.None // 🔥 IMPORTANTE PRO RAILWAY
    };

    return builder.ConnectionString;
}