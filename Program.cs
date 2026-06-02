using LionttoMoveis.Data;
using LionttoMoveis.Helpers;
using LionttoMoveis.Repository;
using LionttoMoveis.Services;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

var ptBr = new CultureInfo("pt-BR");
CultureInfo.DefaultThreadCurrentCulture = ptBr;
CultureInfo.DefaultThreadCurrentUICulture = ptBr;

var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

builder.Services.AddControllersWithViews(options =>
{
    options.ModelBinderProviders.Insert(0, new BrazilianNumberModelBinderProvider());
    options.ModelBindingMessageProvider.SetValueMustNotBeNullAccessor(_ => "Este campo e obrigatorio.");
    options.ModelBindingMessageProvider.SetValueIsInvalidAccessor(_ => "Informe um valor valido.");
    options.ModelBindingMessageProvider.SetAttemptedValueIsInvalidAccessor((_, field) => $"O valor informado para {field} e invalido.");
    options.ModelBindingMessageProvider.SetValueMustBeANumberAccessor(field => $"Digite um numero valido para {field}.");
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

var connection = MySqlConnectionResolver.Resolve(builder.Configuration);
Console.WriteLine($"[DB] Connection source: {connection.Source}");
Console.WriteLine($"[DB] Database target: {connection.Database}");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(
        connection.ConnectionString,
        new MySqlServerVersion(new Version(8, 0, 36)),
        mysql => mysql.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null)
    ));

builder.Services.AddScoped<MaterialRepository>();
builder.Services.AddScoped<ClienteRepository>();
builder.Services.AddScoped<ProdutoRepository>();
builder.Services.AddScoped<PedidoRepository>();
builder.Services.AddScoped<MovimentacaoRepository>();
builder.Services.AddScoped<EstoqueService>();
builder.Services.AddScoped<SeedService>();

var app = builder.Build();

app.UseForwardedHeaders();
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(ptBr),
    SupportedCultures = new[] { ptBr },
    SupportedUICultures = new[] { ptBr }
});

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler(errorApp =>
    {
        errorApp.Run(async context =>
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
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
    try
    {
        var canConnect = await db.Database.CanConnectAsync(ct);
        return canConnect
            ? Results.Ok(new { status = "ok", database = "up" })
            : Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
    }
    catch
    {
        return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
    }
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapControllers();

await InitializeDatabaseAsync(app.Services, app.Configuration);

Console.WriteLine($"[STARTUP] Running on http://0.0.0.0:{port}");

app.Run();

static async Task InitializeDatabaseAsync(IServiceProvider services, IConfiguration configuration)
{
    var initializeOnStartup = configuration.GetValue("DB_INIT_ON_STARTUP", true);
    var applyMigrationsOnStartup = configuration.GetValue("APPLY_MIGRATIONS_ON_STARTUP", false);
    var createSchemaOnStartup = configuration.GetValue("CREATE_SCHEMA_ON_STARTUP", false);
    var seedOnStartup = configuration.GetValue("SEED_ON_STARTUP", false);
    var failFastOnError = configuration.GetValue("DB_FAIL_FAST_ON_INIT_ERROR", true);

    if (!initializeOnStartup)
    {
        Console.WriteLine("[DB] Initialization skipped (DB_INIT_ON_STARTUP=false).");
        return;
    }

    try
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var existingTables = await ObterTabelasDaAplicacaoExistentesAsync(db);
        var hasApplicationTables = existingTables.Count > 0;

        if (hasApplicationTables)
        {
            Console.WriteLine($"[DB] Existing application tables found: {string.Join(", ", existingTables)}.");
        }

        if (applyMigrationsOnStartup && hasApplicationTables)
        {
            var hasMigrationHistory = await TabelaExisteAsync(db, "__EFMigrationsHistory");
            if (!hasMigrationHistory)
            {
                Console.WriteLine("[DB] Existing tables found without EF migration history. Migrations skipped to avoid recreating tables.");
            }
            else
            {
                var pendingMigrations = (await db.Database.GetPendingMigrationsAsync()).ToList();
                if (pendingMigrations.Count == 0)
                {
                    Console.WriteLine("[DB] No pending migrations.");
                }
                else
                {
                    await db.Database.MigrateAsync();
                    Console.WriteLine($"[DB] Migrations applied ({pendingMigrations.Count} pending).");
                }
            }
        }
        else if (applyMigrationsOnStartup)
        {
            await db.Database.MigrateAsync();
            Console.WriteLine("[DB] Migrations applied to empty database.");
        }
        else if (createSchemaOnStartup && !hasApplicationTables)
        {
            await db.Database.EnsureCreatedAsync();
            Console.WriteLine("[DB] EnsureCreated executed (CREATE_SCHEMA_ON_STARTUP=true).");
        }
        else
        {
            Console.WriteLine("[DB] Schema creation/migrations skipped.");
        }

        if (seedOnStartup)
        {
            var seedService = scope.ServiceProvider.GetRequiredService<SeedService>();
            await seedService.SeedAsync();
            Console.WriteLine("[DB] Seed executed.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine("[DB ERROR] Database initialization failed.");
        Console.WriteLine(ex.Message);

        if (failFastOnError)
            throw;
    }
}

static async Task<IReadOnlyCollection<string>> ObterTabelasDaAplicacaoExistentesAsync(AppDbContext db)
{
    var nomesEsperados = new[]
    {
        "materiais",
        "clientes",
        "produtos",
        "materiais_do_produto",
        "pedidos",
        "itens_do_pedido",
        "movimentacoes"
    };

    var tabelasExistentes = new List<string>();
    var connection = db.Database.GetDbConnection();
    var shouldClose = connection.State == System.Data.ConnectionState.Closed;

    if (shouldClose)
        await connection.OpenAsync();

    try
    {
        using var command = connection.CreateCommand();
        var parameterNames = new List<string>();

        for (var i = 0; i < nomesEsperados.Length; i++)
        {
            var parameterName = $"@table{i}";
            var parameter = command.CreateParameter();
            parameter.ParameterName = parameterName;
            parameter.Value = nomesEsperados[i];
            command.Parameters.Add(parameter);
            parameterNames.Add(parameterName);
        }

        command.CommandText = $"""
            SELECT TABLE_NAME
            FROM INFORMATION_SCHEMA.TABLES
            WHERE TABLE_SCHEMA = DATABASE()
              AND TABLE_NAME IN ({string.Join(", ", parameterNames)})
            ORDER BY TABLE_NAME;
            """;

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            tabelasExistentes.Add(reader.GetString(0));
    }
    finally
    {
        if (shouldClose)
            await connection.CloseAsync();
    }

    return tabelasExistentes;
}

static async Task<bool> TabelaExisteAsync(AppDbContext db, string nomeTabela)
{
    var connection = db.Database.GetDbConnection();
    var shouldClose = connection.State == System.Data.ConnectionState.Closed;

    if (shouldClose)
        await connection.OpenAsync();

    try
    {
        using var command = connection.CreateCommand();
        var parameter = command.CreateParameter();
        parameter.ParameterName = "@table";
        parameter.Value = nomeTabela;
        command.Parameters.Add(parameter);

        command.CommandText = """
            SELECT COUNT(*)
            FROM INFORMATION_SCHEMA.TABLES
            WHERE TABLE_SCHEMA = DATABASE()
              AND TABLE_NAME = @table;
            """;

        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result) > 0;
    }
    finally
    {
        if (shouldClose)
            await connection.CloseAsync();
    }
}
