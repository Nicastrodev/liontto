using Microsoft.Extensions.Configuration;
using MySqlConnector;

namespace LionttoMoveis.Data;

public sealed record MySqlConnectionResolution(
    string ConnectionString,
    string Source,
    string Database);

public static class MySqlConnectionResolver
{
    public static MySqlConnectionResolution Resolve(IConfiguration configuration)
    {
        var candidates = new (string? Value, string Source, bool IsUrl)[]
        {
            (configuration["ConnectionStrings__MySQL"], "ConnectionStrings__MySQL", false),
            (configuration["DB_CONNECTION"], "DB_CONNECTION", false),
            (configuration["MYSQL_URL"], "MYSQL_URL", true),
            (configuration["MYSQL_PUBLIC_URL"], "MYSQL_PUBLIC_URL", true),
            (BuildFromSplitVariables(configuration), "MYSQLHOST/MYSQLPORT/MYSQLUSER/MYSQLPASSWORD/MYSQLDATABASE", false),
            (configuration.GetConnectionString("MySQL"), "ConnectionStrings:MySQL", false)
        };

        foreach (var candidate in candidates)
        {
            if (string.IsNullOrWhiteSpace(candidate.Value))
                continue;

            var connectionString = candidate.IsUrl
                ? ConvertMySqlUrlToConnectionString(candidate.Value)
                : candidate.Value!;

            var builder = new MySqlConnectionStringBuilder(connectionString);
            var database = string.IsNullOrWhiteSpace(builder.Database)
                ? "(not-set)"
                : builder.Database;

            return new MySqlConnectionResolution(connectionString, candidate.Source, database);
        }

        throw new InvalidOperationException(
            "No MySQL connection settings found. Configure ConnectionStrings__MySQL, DB_CONNECTION, MYSQL_URL, MYSQL_PUBLIC_URL, or MYSQLHOST/MYSQLPORT/MYSQLUSER/MYSQLPASSWORD/MYSQLDATABASE.");
    }

    private static string? BuildFromSplitVariables(IConfiguration configuration)
    {
        var host = configuration["MYSQLHOST"];
        var user = configuration["MYSQLUSER"];
        var database = configuration["MYSQLDATABASE"] ?? configuration["MYSQL_DATABASE"];

        if (string.IsNullOrWhiteSpace(host) ||
            string.IsNullOrWhiteSpace(user) ||
            string.IsNullOrWhiteSpace(database))
        {
            return null;
        }

        var portRaw = configuration["MYSQLPORT"];
        var hasPort = uint.TryParse(portRaw, out var parsedPort);

        var builder = new MySqlConnectionStringBuilder
        {
            Server = host,
            Port = hasPort ? parsedPort : 3306,
            Database = database,
            UserID = user,
            Password = configuration["MYSQLPASSWORD"] ?? string.Empty,
            CharacterSet = "utf8mb4",
            SslMode = MySqlSslMode.Preferred,
            AllowPublicKeyRetrieval = true
        };

        return builder.ConnectionString;
    }

    private static string ConvertMySqlUrlToConnectionString(string mysqlUrl)
    {
        if (!Uri.TryCreate(mysqlUrl, UriKind.Absolute, out var uri))
            throw new InvalidOperationException("MYSQL_URL/MYSQL_PUBLIC_URL is invalid.");

        var userInfo = uri.UserInfo.Split(':', 2);
        var user = userInfo.Length > 0 ? Uri.UnescapeDataString(userInfo[0]) : string.Empty;
        var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty;

        var database = string.IsNullOrWhiteSpace(uri.AbsolutePath.Trim('/'))
            ? "railway"
            : Uri.UnescapeDataString(uri.AbsolutePath.Trim('/'));

        var builder = new MySqlConnectionStringBuilder
        {
            Server = uri.Host,
            Port = (uint)(uri.IsDefaultPort ? 3306 : uri.Port),
            Database = database,
            UserID = user,
            Password = password,
            CharacterSet = "utf8mb4",
            SslMode = ParseSslMode(uri.Query),
            AllowPublicKeyRetrieval = true
        };

        return builder.ConnectionString;
    }

    private static MySqlSslMode ParseSslMode(string queryString)
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
}
