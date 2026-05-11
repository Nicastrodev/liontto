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
    options.ModelBindingMessageProvider.SetValueIsInvalidAccessor(_ => "Valor inválido.");
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
        ForwardedHeaders.XForwardedProto;

    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

var connectionString = ResolveConnectionString(builder.Configuration);

Console.WriteLine("[DB] Connection String carregada.");

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseMySql(
        connectionString,
        ServerVersion.AutoDetect(connectionString),
        mysql =>
        {
            mysql.EnableRetryOnFailure(
                5,
                TimeSpan.FromSeconds(10),
                null
            );
        });
});

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
            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";

            var feature = context.Features.Get<IExceptionHandlerFeature>();

            await context.Response.WriteAsJsonAsync(new
            {
                error = "Internal server error.",
                detail = feature?.Error?.Message
            });
        });
    });

    app.UseHsts();
}

app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.MapGet("/health", () =>
{
    return Results.Ok(new
    {
        status = "ok"
    });
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapControllers();


// ==========================
// CRIA DATABASE/TABELAS
// ==========================

using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Console.WriteLine("[DB] Criando estrutura do banco...");

        await db.Database.EnsureCreatedAsync();

        Console.WriteLine("[DB] Banco criado com sucesso.");

        var seed = scope.ServiceProvider.GetRequiredService<SeedService>();

        await seed.SeedAsync();

        Console.WriteLine("[DB] Seed executado.");
    }
    catch (Exception ex)
    {
        Console.WriteLine("[DB ERROR]");
        Console.WriteLine(ex.Message);
    }
}

Console.WriteLine($"[STARTUP] Running on port {port}");

app.Run();


// ======================================
// CONNECTION STRING
// ======================================

static string ResolveConnectionString(IConfiguration config)
{
    var mysqlUrl =
        config["MYSQL_URL"] ??
        config["MYSQL_PUBLIC_URL"];

    if (!string.IsNullOrWhiteSpace(mysqlUrl))
    {
        return ConvertMySqlUrl(mysqlUrl);
    }

    var host = config["MYSQLHOST"];
    var port = config["MYSQLPORT"];
    var database = config["MYSQLDATABASE"];
    var user = config["MYSQLUSER"];
    var password = config["MYSQLPASSWORD"];

    if (!string.IsNullOrWhiteSpace(host))
    {
        return new MySqlConnectionStringBuilder
        {
            Server = host,
            Port = uint.Parse(port ?? "3306"),
            Database = database,
            UserID = user,
            Password = password,
            SslMode = MySqlSslMode.Required,
            CharacterSet = "utf8mb4",
            AllowPublicKeyRetrieval = true
        }.ConnectionString;
    }

    var fallback =
        config.GetConnectionString("MySQL");

    if (!string.IsNullOrWhiteSpace(fallback))
    {
        return fallback;
    }

    throw new Exception("Nenhuma connection string encontrada.");
}

static string ConvertMySqlUrl(string url)
{
    var uri = new Uri(url);

    var userInfo = uri.UserInfo.Split(':');

    var database = uri.AbsolutePath.Trim('/');

    return new MySqlConnectionStringBuilder
    {
        Server = uri.Host,
        Port = (uint)uri.Port,
        Database = database,
        UserID = Uri.UnescapeDataString(userInfo[0]),
        Password = Uri.UnescapeDataString(userInfo[1]),
        SslMode = MySqlSslMode.Required,
        CharacterSet = "utf8mb4",
        AllowPublicKeyRetrieval = true
    }.ConnectionString;
}