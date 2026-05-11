using LionttoMoveis.Data;
using LionttoMoveis.Repository;
using LionttoMoveis.Services;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;

var builder = WebApplication.CreateBuilder(args);

var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

builder.Services.AddControllersWithViews(options =>
{
    options.ModelBindingMessageProvider.SetValueMustNotBeNullAccessor(_ => "Preencha este campo.");
    options.ModelBindingMessageProvider.SetValueIsInvalidAccessor(_ => "Valor invalido para o campo informado.");
    options.ModelBindingMessageProvider.SetAttemptedValueIsInvalidAccessor((_, campo) => $"Preencha o campo {campo}.");
});

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.IdleTimeout = TimeSpan.FromMinutes(30);
});

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor |
        ForwardedHeaders.XForwardedProto |
        ForwardedHeaders.XForwardedHost;

    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

var resolvedConnection = ResolveMySqlConnectionString(builder.Configuration);

if (string.IsNullOrWhiteSpace(resolvedConnection.ConnectionString))
{
    throw new InvalidOperationException(
        "Nenhuma configuracao de banco foi encontrada. Configure ConnectionStrings__MySQL, MYSQL_URL, MYSQL_PUBLIC_URL ou MYSQLHOST/MYSQLPORT/MYSQLUSER/MYSQLPASSWORD/MYSQLDATABASE.");
}

var connectionString = resolvedConnection.ConnectionString;
Console.WriteLine($"[DB] Connection source: {resolvedConnection.Source}");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(
        connectionString,
        new MySqlServerVersion(new Version(8, 0, 0)),
        mySqlOptions => mySqlOptions.EnableRetryOnFailure(3, TimeSpan.FromSeconds(3), null)
    )
);

builder.Services.AddScoped<MaterialRepository>();
builder.Services.AddScoped<ClienteRepository>();
builder.Services.AddScoped<ProdutoRepository>();
builder.Services.AddScoped<PedidoRepository>();
builder.Services.AddScoped<MovimentacaoRepository>();
builder.Services.AddScoped<EstoqueService>();
builder.Services.AddScoped<SeedService>();

var app = builder.Build();

app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler(errorApp =>
    {
        errorApp.Run(async context =>
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";

            var feature = context.Features.Get<IExceptionHandlerFeature>();
            var detail = feature?.Error?.Message ?? "Unexpected server error.";
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Internal server error.",
                detail
            });
        });
    });
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseStaticFiles();
app.UseRouting();
app.UseSession();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapGet("/health/db", async (AppDbContext db, CancellationToken ct) =>
{
    var canConnect = await db.Database.CanConnectAsync(ct);
    return canConnect
        ? Results.Ok(new { status = "ok", database = "up" })
        : Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapControllers();

app.Lifetime.ApplicationStarted.Register(() =>
{
    _ = Task.Run(async () => await InitializeDatabaseAsync(app, builder.Configuration));
});

Console.WriteLine($"[STARTUP] Listening on http://0.0.0.0:{port}");

app.Run();

static async Task InitializeDatabaseAsync(WebApplication app, IConfiguration configuration)
{
    var shouldInitDb = configuration.GetValue("DB_INIT_ON_STARTUP", true);
    var shouldApplyMigrations = configuration.GetValue("APPLY_MIGRATIONS_ON_STARTUP", false);
    var shouldSeed = configuration.GetValue("SEED_ON_STARTUP", false);

    if (!shouldInitDb)
    {
        Console.WriteLine("[DB] Startup initialization skipped (DB_INIT_ON_STARTUP=false).");
        return;
    }

    using var scope = app.Services.CreateScope();

    try
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (shouldApplyMigrations)
        {
            await db.Database.MigrateAsync();
            Console.WriteLine("[DB] Migrations applied successfully.");
        }
        else
        {
            await db.Database.EnsureCreatedAsync();
            Console.WriteLine("[DB] EnsureCreated executed successfully.");
        }

        if (shouldSeed)
        {
            var seedService = scope.ServiceProvider.GetRequiredService<SeedService>();
            await seedService.SeedAsync();
            Console.WriteLine("[DB] Seed executed successfully.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[DB WARNING] Initialization failed, app will continue running. Error: {ex.Message}");
    }
}

static (string? ConnectionString, string Source) ResolveMySqlConnectionString(IConfiguration configuration)
{
    var explicitConnection = configuration["ConnectionStrings__MySQL"];
    if (!string.IsNullOrWhiteSpace(explicitConnection))
        return (explicitConnection, "ConnectionStrings__MySQL");

    var dbConnection = configuration["DB_CONNECTION"];
    if (!string.IsNullOrWhiteSpace(dbConnection))
        return (dbConnection, "DB_CONNECTION");

    var mysqlUrl = configuration["MYSQL_URL"];
    if (!string.IsNullOrWhiteSpace(mysqlUrl))
        return (ConvertMySqlUrlToConnectionString(mysqlUrl), "MYSQL_URL");

    var mysqlPublicUrl = configuration["MYSQL_PUBLIC_URL"];
    if (!string.IsNullOrWhiteSpace(mysqlPublicUrl))
        return (ConvertMySqlUrlToConnectionString(mysqlPublicUrl), "MYSQL_PUBLIC_URL");

    var host = configuration["MYSQLHOST"];
    var user = configuration["MYSQLUSER"];
    var password = configuration["MYSQLPASSWORD"];
    var database = configuration["MYSQLDATABASE"] ?? configuration["MYSQL_DATABASE"];

    if (!string.IsNullOrWhiteSpace(host) &&
        !string.IsNullOrWhiteSpace(user) &&
        !string.IsNullOrWhiteSpace(database))
    {
        var portRaw = configuration["MYSQLPORT"];
        var hasPort = uint.TryParse(portRaw, out var parsedPort);

        var fromParts = new MySqlConnectionStringBuilder
        {
            Server = host,
            Port = hasPort ? parsedPort : 3306,
            UserID = user,
            Password = password ?? string.Empty,
            Database = database,
            CharacterSet = "utf8mb4",
            SslMode = MySqlSslMode.Preferred,
            AllowPublicKeyRetrieval = true
        };

        return (fromParts.ConnectionString, "MYSQLHOST/MYSQLPORT/MYSQLUSER/MYSQLPASSWORD/MYSQLDATABASE");
    }

    var appSettingsConnection = configuration.GetConnectionString("MySQL");
    if (!string.IsNullOrWhiteSpace(appSettingsConnection))
        return (appSettingsConnection, "ConnectionStrings:MySQL");

    return (null, "not-found");
}

static string ConvertMySqlUrlToConnectionString(string mysqlUrl)
{
    if (!Uri.TryCreate(mysqlUrl, UriKind.Absolute, out var uri))
        throw new InvalidOperationException("MYSQL_URL/MYSQL_PUBLIC_URL invalida.");

    var userInfo = uri.UserInfo.Split(':', 2);
    var user = userInfo.Length > 0 ? Uri.UnescapeDataString(userInfo[0]) : string.Empty;
    var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty;

    var database = string.IsNullOrWhiteSpace(uri.AbsolutePath.Trim('/'))
        ? "railway"
        : Uri.UnescapeDataString(uri.AbsolutePath.Trim('/'));

    var sslMode = ParseSslMode(uri.Query);

    var csBuilder = new MySqlConnectionStringBuilder
    {
        Server = uri.Host,
        Port = (uint)(uri.IsDefaultPort ? 3306 : uri.Port),
        UserID = user,
        Password = password,
        Database = database,
        CharacterSet = "utf8mb4",
        SslMode = sslMode,
        AllowPublicKeyRetrieval = true
    };

    return csBuilder.ConnectionString;
}

static MySqlSslMode ParseSslMode(string queryString)
{
    if (string.IsNullOrWhiteSpace(queryString))
        return MySqlSslMode.Preferred;

    var query = queryString.TrimStart('?')
        .Split('&', StringSplitOptions.RemoveEmptyEntries)
        .Select(pair => pair.Split('=', 2))
        .Where(parts => parts.Length == 2)
        .ToDictionary(
            parts => parts[0].ToLowerInvariant(),
            parts => Uri.UnescapeDataString(parts[1]).ToLowerInvariant());

    if (!query.TryGetValue("sslmode", out var mode))
        return MySqlSslMode.Preferred;

    return mode switch
    {
        "none" => MySqlSslMode.None,
        "preferred" => MySqlSslMode.Preferred,
        "required" => MySqlSslMode.Required,
        "verifyca" => MySqlSslMode.VerifyCA,
        "verifyfull" => MySqlSslMode.VerifyFull,
        _ => MySqlSslMode.Preferred
    };
}
