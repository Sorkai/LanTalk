using LanTalk.Core.Constants;
using Microsoft.Data.Sqlite;

namespace LanTalk.Storage.Database;

public sealed class SqliteConnectionFactory
{
    public string DatabasePath { get; }

    public SqliteConnectionFactory(string? databasePath = null)
    {
        DatabasePath = databasePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            NetworkConstants.ApplicationFolderName,
            NetworkConstants.DatabaseFileName);
    }

    public SqliteConnection CreateConnection()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(DatabasePath)!);
        return new SqliteConnection($"Data Source={DatabasePath}");
    }
}

