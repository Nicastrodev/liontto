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

var (connStr, connSource) = ResolveMySqlConnectionString(builder.Configuration);
Console.WriteLine($"[config] MySQL connection source: {connSource}");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(
        connStr,
        ServerVersion.AutoDetect(connStr),
        mySqlOptions => mySqlOptions.EnableRetryOnFailure(3)
    )
);

builder.Services.AddScoped<MaterialRepository>();
builder.Services.AddScoped<ClienteRepository>();
builder.Services.AddScoped<ProdutoRepository>();
builder.Services.AddScoped<PedidoRepository>();
builder.Services.AddScoped<MovimentacaoRepository>();

builder.Services.AddScoped<EstoqueService>();
builder.Services.AddScoped<SeedService>();

builder.Services.AddSession();

var app = builder.Build();

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

var applyMigrationsOnStartup = ReadBool(builder.Configuration["APPLY_MIGRATIONS_ON_STARTUP"], false);
var seedOnStartup = ReadBool(builder.Configuration["SEED_ON_STARTUP"], !app.Environment.IsProduction());

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

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

Console.WriteLine("Liontto Moveis - ASP.NET Core + MySQL");
app.Run();

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

    return (BuildConnectionStringFromParts(config), "MYSQLHOST/MYSQLPORT/MYSQLUSER/MYSQLPASSWORD/MYSQLDATABASE");
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
    if (!Uri.TryCreate(mysqlUrl, UriKind.Absolute, out var uri) ||
        !"mysql".Equals(uri.Scheme, StringComparison.OrdinalIgnoreCase))
    {
        throw new InvalidOperationException("MYSQL_URL/MYSQL_PUBLIC_URL invalida. Use formato mysql://usuario:senha@host:porta/database");
    }

    var userInfo = uri.UserInfo.Split(':', 2, StringSplitOptions.None);
    var user = userInfo.Length > 0 ? Uri.UnescapeDataString(userInfo[0]) : string.Empty;
    var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty;
    var database = uri.AbsolutePath.Trim('/');

    if (string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(database))
        throw new InvalidOperationException("MYSQL_URL/MYSQL_PUBLIC_URL deve conter usuario e database.");

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
    var host = config["MYSQLHOST"];
    var user = config["MYSQLUSER"];
    var password = config["MYSQLPASSWORD"] ?? config["MYSQL_ROOT_PASSWORD"];
    var database = config["MYSQLDATABASE"] ?? config["MYSQL_DATABASE"];

    var portRaw = config["MYSQLPORT"];
    var port = uint.TryParse(portRaw, out var parsedPort) ? parsedPort : 3306;

    if (string.IsNullOrWhiteSpace(host) ||
        string.IsNullOrWhiteSpace(user) ||
        string.IsNullOrWhiteSpace(database))
    {
        throw new InvalidOperationException(
            "Nao foi possivel resolver a conexao MySQL. Defina ConnectionStrings__MySQL, DB_CONNECTION, MYSQL_URL, MYSQL_PUBLIC_URL ou variaveis MYSQLHOST/MYSQLPORT/MYSQLUSER/MYSQLPASSWORD/MYSQLDATABASE.");
    }

    var builder = new MySqlConnectionStringBuilder
    {
        Server = host,
        Port = port,
        UserID = user,
        Password = password,
        Database = database,
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
           raw.Equals("yes", StringComparison.OrdinalIgnoreCase) ||
           raw.Equals("on", StringComparison.OrdinalIgnoreCase);
}
