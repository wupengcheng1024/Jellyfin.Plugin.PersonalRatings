using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.PersonalRatings.Data;

internal sealed class DatabaseInitializer : IDatabaseInitializer
{
    private const string InitializationSql = """
        CREATE TABLE IF NOT EXISTS user_item_ratings (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            user_id TEXT NOT NULL,
            item_id TEXT NOT NULL,
            score INTEGER NOT NULL DEFAULT 0,
            is_pending_delete INTEGER NOT NULL DEFAULT 0,
            last_played_at TEXT NULL,
            rated_at TEXT NULL,
            updated_at TEXT NOT NULL,
            created_at TEXT NOT NULL,
            UNIQUE(user_id, item_id)
        );

        CREATE INDEX IF NOT EXISTS idx_ratings_user_score ON user_item_ratings(user_id, score);
        CREATE INDEX IF NOT EXISTS idx_ratings_user_pending ON user_item_ratings(user_id, is_pending_delete);
        CREATE INDEX IF NOT EXISTS idx_ratings_user_updated ON user_item_ratings(user_id, updated_at DESC);

        CREATE TABLE IF NOT EXISTS delete_audit_logs (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            operator_user_id TEXT NOT NULL,
            item_id TEXT NOT NULL,
            item_name TEXT NULL,
            action TEXT NOT NULL,
            result TEXT NOT NULL,
            message TEXT NULL,
            created_at TEXT NOT NULL
        );
        """;

    private readonly ISqliteConnectionFactory _connectionFactory;
    private readonly ILogger<DatabaseInitializer> _logger;

    public DatabaseInitializer(ISqliteConnectionFactory connectionFactory, ILogger<DatabaseInitializer> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        _connectionFactory.EnsureDatabaseDirectory();

        await using SqliteConnection connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText = InitializationSql;

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Initialized Personal Ratings SQLite database at {DatabasePath}", _connectionFactory.DatabasePath);
    }
}
