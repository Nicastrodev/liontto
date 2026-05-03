using LionttoMoveis.Data;
using LionttoMoveis.Repository;
using LionttoMoveis.Services;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;

var builder = WebApplication.CreateBuilder(args);

// MVC
builder.Services.AddControllersWithViews(options =>
{
    options.ModelBindingMessageProvider.SetValueMustNotBeNullAccessor(_ => "Preencha este campo.");
    options.ModelBindingMessageProvider.SetValueIsInvalidAccessor(_ => "Valor inválido para o campo informado.");
    options.ModelBindingMessageProvider.SetAttemptedValueIsInvalidAccessor((_, campo) => $"Preencha o campo {campo}.");
});

// MYSQL
var rawMysqlUrl = builder.Configuration["MYSQL_URL"];

if (string.IsNullOrWhiteSpace(rawMysqlUrl))
    throw new Exception("MYSQL_URL não encontrada.");

var connectionString = ConvertMySqlUrlToConnectionString(rawMysqlUrl);

// DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(
        connectionString,
        ServerVersion.AutoDetect(connectionString),
        mySqlOptions => mySqlOptions.EnableRetryOnFailure()
    )
);

// DI
builder.Services.AddScoped<MaterialRepository>();
builder.Services.AddScoped<ClienteRepository>();
builder.Services.AddScoped<ProdutoRepository>();
builder.Services.AddScoped<PedidoRepository>();
builder.Services.AddScoped<MovimentacaoRepository>();

builder.Services.AddScoped<EstoqueService>();
builder.Services.AddScoped<SeedService>();

builder.Services.AddSession();

var app = builder.Build();

// Middleware
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseStaticFiles();
app.UseRouting();
app.UseSession();

// Healthcheck Railway
app.MapGet("/", () => Results.Ok("OK"));

// Rotas MVC
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Init DB (não quebra deploy)
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
        Console.WriteLine($"[DB ERROR] {ex.Message}");
    }
}

// Railway port fix (CORRETO)
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
app.Urls.Clear();
app.Urls.Add($"http://0.0.0.0:{port}");

Console.WriteLine($"🚀 Rodando na porta {port}");

app.Run();


// HELPER
static string ConvertMySqlUrlToConnectionString(string mysqlUrl)
{
    if (!Uri.TryCreate(mysqlUrl, UriKind.Absolute, out var uri))
        throw new Exception("MYSQL_URL inválida");

    var userInfo = uri.UserInfo.Split(':', 2);

    var user = userInfo[0];
    var password = userInfo.Length > 1 ? userInfo[1] : "";

    var database = string.IsNullOrWhiteSpace(uri.AbsolutePath.Trim('/'))
        ? "railway"
        : uri.AbsolutePath.Trim('/');

    var builder = new MySqlConnectionStringBuilder
    {
        Server = uri.Host,
        Port = (uint)(uri.IsDefaultPort ? 3306 : uri.Port),
        UserID = user,
        Password = password,
        Database = database,
        CharacterSet = "utf8mb4",

        SslMode = MySqlSslMode.None,
        AllowPublicKeyRetrieval = true
    };

    return builder.ConnectionString;
}