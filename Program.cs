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

// CORS
var corsOriginsRaw = builder.Configuration["CORS_ALLOWED_ORIGINS"];
var corsOrigins = (corsOriginsRaw ?? string.Empty)
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

if (corsOrigins.Length > 0)
{
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("FrontendCors", policy =>
        {
            policy.WithOrigins(corsOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
    });
}

// 🔥 CONEXÃO MYSQL
var (connStr, connSource) = ResolveMySqlConnectionString(builder.Configuration);
Console.WriteLine($"[config] MySQL connection source: {connSource}");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(
        connStr,
        new MySqlServerVersion(new Version(8, 0, 0)), // ✅ SEM AutoDetect (evita crash)
        mySqlOptions => mySqlOptions.EnableRetryOnFailure(3)
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

// Middleware
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();

if (corsOrigins.Length > 0)
    app.UseCors("FrontendCors");

app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// 🔥 MIGRATION / SEED CONTROLADO
var applyMigrationsOnStartup = ReadBool(builder.Configuration["APPLY_MIGRATIONS_ON_STARTUP"], false);
var seedOnStartup = ReadBool(builder.Configuration["SEED_ON_STARTUP"], !app.Environment.IsProduction());

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    try
    {
        if (applyMigrationsOnStartup)
            db.Database.Migrate();
        else
            db.Database.EnsureCreated();

        if (seedOnStartup)
        {
            var seed = scope.ServiceProvider.GetRequiredService<SeedService>();
            await seed.SeedAsync();
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[ERROR] Falha ao inicializar banco: {ex.Message}");
        // ❗ NÃO derruba o app
    }
}

Console.WriteLine("Liontto Moveis - ASP.NET Core + MySQL");
app.Run();


// =======================
// 🔥 HELPERS
// =======================

static (string connectionString, string source) ResolveMySqlConnectionString(IConfiguration config)
{
    var connFromNet = config.GetConnectionString("MySQL");
    if (!string.IsNullOrWhiteSpace(connFromNet))
        return (NormalizeConnectionString(connFromNet), "ConnectionStrings:MySQL");

    var dbConnection = config["DB_CONNECTION"];
    if (!string.IsNullOrWhiteSpace(dbConnection))
        return (NormalizeConnectionString(dbConnection), "DB_CONNECTION");

    var mysqlUrl = config["MYSQL_URL"];
    if (!string.IsNullOrWhiteSpace(mysqlUrl))
        return (ConvertMySqlUrlToConnectionString(mysqlUrl), "MYSQL_URL");

    var mysqlPublicUrl = config["MYSQL_PUBLIC_URL"];
    if (!string.IsNullOrWhiteSpace(mysqlPublicUrl))
        return (ConvertMySqlUrlToConnectionString(mysqlPublicUrl), "MYSQL_PUBLIC_URL");

    return (BuildConnectionStringFromParts(config), "MYSQL PARTS");
}

static string NormalizeConnectionString(string raw)
{
    var value = raw.Trim();
    if (value.StartsWith("mysql://", StringComparison.OrdinalIgnoreCase))
        return ConvertMySqlUrlToConnectionString(value);

    return value;
}

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

static string BuildConnectionStringFromParts(IConfiguration config)
{
    var builder = new MySqlConnectionStringBuilder
    {
        Server = config["MYSQLHOST"],
        Port = uint.TryParse(config["MYSQLPORT"], out var p) ? p : 3306,
        UserID = config["MYSQLUSER"],
        Password = config["MYSQLPASSWORD"] ?? config["MYSQL_ROOT_PASSWORD"],
        Database = config["MYSQLDATABASE"] ?? config["MYSQL_DATABASE"],
        CharacterSet = "utf8mb4",
        SslMode = MySqlSslMode.Preferred
    };

    return builder.ConnectionString;
}

static bool ReadBool(string? raw, bool defaultValue)
{
    if (string.IsNullOrWhiteSpace(raw))
        return defaultValue;

    return raw.Equals("1", StringComparison.OrdinalIgnoreCase) ||
           raw.Equals("true", StringComparison.OrdinalIgnoreCase) ||
           raw.Equals("yes", StringComparison.OrdinalIgnoreCase);
}