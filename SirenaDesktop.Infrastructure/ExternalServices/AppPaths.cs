using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Text;

namespace SirenaDesktop.Infrastructure.ExternalServices;
public static class AppPaths
{
    public static string DataDir { get; } = Initialize();

    public static string BuildConnectionString(string? configured)
    {
        var builder = new SqliteConnectionStringBuilder(configured ?? "Data Source=sirena_local.db");
        builder.DataSource = Path.Combine(DataDir, Path.GetFileName(builder.DataSource));
        return builder.ToString();
    }

    private static string Initialize()
    {
        var path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SirenaDesktop");
        Directory.CreateDirectory(path);
        return path;
    }
}