using LionttoMoveis.Data;
using LionttoMoveis.Helpers;
using LionttoMoveis.Repository;
using LionttoMoveis.Services;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

builder.Services.AddControllersWithViews(options =>
{
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

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseHsts();
    app.UseMiddleware<GlobalExceptionMiddleware>();
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
    var applyMigrationsOnStartup = configuration.GetValue("APPLY_MIGRATIONS_ON_STARTUP", true);
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

        if (applyMigrationsOnStartup)
        {
            var knownMigrations = db.Database.GetMigrations().ToList();

            if (knownMigrations.Count > 0)
            {
                await db.Database.MigrateAsync();
                Console.WriteLine($"[DB] Migrations applied ({knownMigrations.Count} available). ");
            }
            else
            {
                await db.Database.EnsureCreatedAsync();
                Console.WriteLine("[DB] No migrations found. EnsureCreated executed as fallback.");
            }
        }
        else
        {
            await db.Database.EnsureCreatedAsync();
            Console.WriteLine("[DB] EnsureCreated executed (migrations disabled). ");
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
