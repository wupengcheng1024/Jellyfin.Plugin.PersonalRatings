using Microsoft.Data.Sqlite;

namespace Jellyfin.Plugin.PersonalRatings.Data;

internal interface ISqliteConnectionFactory
{
    string DatabasePath { get; }

    SqliteConnection CreateConnection();

    void EnsureDatabaseDirectory();
}
