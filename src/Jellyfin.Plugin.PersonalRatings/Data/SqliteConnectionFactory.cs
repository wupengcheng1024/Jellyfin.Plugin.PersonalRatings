using Jellyfin.Plugin.PersonalRatings.Configuration;
using MediaBrowser.Common.Configuration;
using Microsoft.Data.Sqlite;

namespace Jellyfin.Plugin.PersonalRatings.Data;

internal sealed class SqliteConnectionFactory : ISqliteConnectionFactory
{
    private readonly IApplicationPaths _applicationPaths;

    public SqliteConnectionFactory(IApplicationPaths applicationPaths)
    {
        _applicationPaths = applicationPaths;
    }

    public string DatabasePath => Path.Combine(GetDatabaseDirectoryPath(), GetDatabaseFileName());

    public SqliteConnection CreateConnection()
    {
        SqliteConnectionStringBuilder connectionStringBuilder = new()
        {
            DataSource = DatabasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared
        };

        return new SqliteConnection(connectionStringBuilder.ToString());
    }

    public void EnsureDatabaseDirectory()
    {
        Directory.CreateDirectory(GetDatabaseDirectoryPath());
    }

    private string GetDatabaseDirectoryPath()
    {
        return Path.Combine(_applicationPaths.DataPath, "plugins", "Jellyfin.PersonalRatings");
    }

    private static string GetDatabaseFileName()
    {
        PluginConfiguration? configuration = Plugin.Instance?.Configuration;
        if (configuration is not null && !string.IsNullOrWhiteSpace(configuration.DatabaseFileName))
        {
            return configuration.DatabaseFileName;
        }

        return PluginConfiguration.DefaultDatabaseFileName;
    }
}
