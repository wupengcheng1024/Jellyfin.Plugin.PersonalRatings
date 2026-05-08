using System.Globalization;
using System.Text;
using Jellyfin.Plugin.PersonalRatings.Models.Entities;
using Jellyfin.Plugin.PersonalRatings.Models.Requests;
using Microsoft.Data.Sqlite;

namespace Jellyfin.Plugin.PersonalRatings.Data.Repositories;

internal sealed class RatingRepository : IRatingRepository
{
    private readonly ISqliteConnectionFactory _connectionFactory;

    public RatingRepository(ISqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<UserItemRating?> GetAsync(Guid userId, Guid itemId, CancellationToken cancellationToken)
    {
        await using SqliteConnection connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT id, user_id, item_id, score, is_pending_delete, last_played_at, rated_at, updated_at, created_at
            FROM user_item_ratings
            WHERE user_id = @userId AND item_id = @itemId
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("@userId", userId.ToString("D", CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("@itemId", itemId.ToString("D", CultureInfo.InvariantCulture));

        await using SqliteDataReader reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            return null;
        }

        return ReadRating(reader);
    }

    public async Task<UserItemRating> UpsertScoreAsync(Guid userId, Guid itemId, int score, CancellationToken cancellationToken)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        string nowText = FormatDate(now);

        await using SqliteConnection connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO user_item_ratings (
                user_id,
                item_id,
                score,
                is_pending_delete,
                last_played_at,
                rated_at,
                updated_at,
                created_at
            )
            VALUES (
                @userId,
                @itemId,
                @score,
                0,
                NULL,
                @ratedAt,
                @updatedAt,
                @createdAt
            )
            ON CONFLICT(user_id, item_id) DO UPDATE SET
                score = excluded.score,
                rated_at = excluded.rated_at,
                updated_at = excluded.updated_at;
            """;
        command.Parameters.AddWithValue("@userId", userId.ToString("D", CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("@itemId", itemId.ToString("D", CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("@score", score);
        command.Parameters.AddWithValue("@ratedAt", nowText);
        command.Parameters.AddWithValue("@updatedAt", nowText);
        command.Parameters.AddWithValue("@createdAt", nowText);

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

        UserItemRating? rating = await GetAsync(userId, itemId, cancellationToken).ConfigureAwait(false);
        return rating ?? throw new InvalidOperationException("Failed to load rating after upsert.");
    }

    public async Task<IReadOnlyList<UserItemRating>> UpsertScoresAsync(Guid userId, IReadOnlyList<Guid> itemIds, int score, CancellationToken cancellationToken)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        string nowText = FormatDate(now);

        await using SqliteConnection connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using SqliteTransaction transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        foreach (Guid itemId in itemIds)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await using SqliteCommand command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO user_item_ratings (
                    user_id,
                    item_id,
                    score,
                    is_pending_delete,
                    last_played_at,
                    rated_at,
                    updated_at,
                    created_at
                )
                VALUES (
                    @userId,
                    @itemId,
                    @score,
                    0,
                    NULL,
                    @ratedAt,
                    @updatedAt,
                    @createdAt
                )
                ON CONFLICT(user_id, item_id) DO UPDATE SET
                    score = excluded.score,
                    rated_at = excluded.rated_at,
                    updated_at = excluded.updated_at;
                """;
            command.Parameters.AddWithValue("@userId", userId.ToString("D", CultureInfo.InvariantCulture));
            command.Parameters.AddWithValue("@itemId", itemId.ToString("D", CultureInfo.InvariantCulture));
            command.Parameters.AddWithValue("@score", score);
            command.Parameters.AddWithValue("@ratedAt", nowText);
            command.Parameters.AddWithValue("@updatedAt", nowText);
            command.Parameters.AddWithValue("@createdAt", nowText);

            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        return await GetManyAsync(userId, itemIds, cancellationToken).ConfigureAwait(false);
    }

    public async Task<UserItemRating> ClearScoreAsync(Guid userId, Guid itemId, CancellationToken cancellationToken)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        string nowText = FormatDate(now);

        await using SqliteConnection connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO user_item_ratings (
                user_id,
                item_id,
                score,
                is_pending_delete,
                last_played_at,
                rated_at,
                updated_at,
                created_at
            )
            VALUES (
                @userId,
                @itemId,
                0,
                0,
                NULL,
                NULL,
                @updatedAt,
                @createdAt
            )
            ON CONFLICT(user_id, item_id) DO UPDATE SET
                score = 0,
                rated_at = NULL,
                updated_at = excluded.updated_at;
            """;
        command.Parameters.AddWithValue("@userId", userId.ToString("D", CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("@itemId", itemId.ToString("D", CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("@updatedAt", nowText);
        command.Parameters.AddWithValue("@createdAt", nowText);

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

        UserItemRating? rating = await GetAsync(userId, itemId, cancellationToken).ConfigureAwait(false);
        return rating ?? throw new InvalidOperationException("Failed to load rating after clear.");
    }

    public async Task<IReadOnlyList<UserItemRating>> ClearScoresAsync(Guid userId, IReadOnlyList<Guid> itemIds, CancellationToken cancellationToken)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        string nowText = FormatDate(now);

        await using SqliteConnection connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using SqliteTransaction transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        foreach (Guid itemId in itemIds)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await using SqliteCommand command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO user_item_ratings (
                    user_id,
                    item_id,
                    score,
                    is_pending_delete,
                    last_played_at,
                    rated_at,
                    updated_at,
                    created_at
                )
                VALUES (
                    @userId,
                    @itemId,
                    0,
                    0,
                    NULL,
                    NULL,
                    @updatedAt,
                    @createdAt
                )
                ON CONFLICT(user_id, item_id) DO UPDATE SET
                    score = 0,
                    rated_at = NULL,
                    updated_at = excluded.updated_at;
                """;
            command.Parameters.AddWithValue("@userId", userId.ToString("D", CultureInfo.InvariantCulture));
            command.Parameters.AddWithValue("@itemId", itemId.ToString("D", CultureInfo.InvariantCulture));
            command.Parameters.AddWithValue("@updatedAt", nowText);
            command.Parameters.AddWithValue("@createdAt", nowText);

            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        return await GetManyAsync(userId, itemIds, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<UserItemRating>> SetPendingDeleteAsync(Guid userId, IReadOnlyList<Guid> itemIds, bool isPendingDelete, CancellationToken cancellationToken)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        string nowText = FormatDate(now);

        await using SqliteConnection connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using SqliteTransaction transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        foreach (Guid itemId in itemIds)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await using SqliteCommand command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO user_item_ratings (
                    user_id,
                    item_id,
                    score,
                    is_pending_delete,
                    last_played_at,
                    rated_at,
                    updated_at,
                    created_at
                )
                VALUES (
                    @userId,
                    @itemId,
                    0,
                    @isPendingDelete,
                    NULL,
                    NULL,
                    @updatedAt,
                    @createdAt
                )
                ON CONFLICT(user_id, item_id) DO UPDATE SET
                    is_pending_delete = excluded.is_pending_delete,
                    updated_at = excluded.updated_at;
                """;
            command.Parameters.AddWithValue("@userId", userId.ToString("D", CultureInfo.InvariantCulture));
            command.Parameters.AddWithValue("@itemId", itemId.ToString("D", CultureInfo.InvariantCulture));
            command.Parameters.AddWithValue("@isPendingDelete", isPendingDelete ? 1 : 0);
            command.Parameters.AddWithValue("@updatedAt", nowText);
            command.Parameters.AddWithValue("@createdAt", nowText);

            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        return await GetManyAsync(userId, itemIds, cancellationToken).ConfigureAwait(false);
    }

    public async Task<PagedQueryResult<UserItemRating>> QueryPageAsync(Guid userId, RatingQueryRequest request, CancellationToken cancellationToken)
    {
        List<SqliteParameter> parameters = [];
        string whereClause = BuildWhereClause(userId, request, parameters);
        string sortColumn = ResolveSortColumn(request.SortBy);
        string sortDirection = ResolveSortDirection(request.SortOrder);
        int offset = (request.PageNumber - 1) * request.PageSize;

        await using SqliteConnection connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using SqliteCommand countCommand = connection.CreateCommand();
        countCommand.CommandText = "SELECT COUNT(1) FROM user_item_ratings " + whereClause + ";";
        AddParameters(countCommand, parameters);
        object? countValue = await countCommand.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);

        await using SqliteCommand itemsCommand = connection.CreateCommand();
        itemsCommand.CommandText = $"""
            SELECT id, user_id, item_id, score, is_pending_delete, last_played_at, rated_at, updated_at, created_at
            FROM user_item_ratings
            {whereClause}
            ORDER BY {sortColumn} {sortDirection}, id DESC
            LIMIT @limit OFFSET @offset;
            """;
        AddParameters(itemsCommand, parameters);
        itemsCommand.Parameters.AddWithValue("@limit", request.PageSize);
        itemsCommand.Parameters.AddWithValue("@offset", offset);

        List<UserItemRating> items = [];
        await using SqliteDataReader reader = await itemsCommand.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            items.Add(ReadRating(reader));
        }

        return new PagedQueryResult<UserItemRating>
        {
            Items = items,
            TotalCount = Convert.ToInt32(countValue, CultureInfo.InvariantCulture)
        };
    }

    public async Task<IReadOnlyList<UserItemRating>> ListAsync(Guid userId, RatingQueryRequest request, CancellationToken cancellationToken)
    {
        List<SqliteParameter> parameters = [];
        string whereClause = BuildWhereClause(userId, request, parameters);
        string sortColumn = ResolveSortColumn(request.SortBy);
        string sortDirection = ResolveSortDirection(request.SortOrder);

        await using SqliteConnection connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText = $"""
            SELECT id, user_id, item_id, score, is_pending_delete, last_played_at, rated_at, updated_at, created_at
            FROM user_item_ratings
            {whereClause}
            ORDER BY {sortColumn} {sortDirection}, id DESC;
            """;
        AddParameters(command, parameters);

        List<UserItemRating> items = [];
        await using SqliteDataReader reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            items.Add(ReadRating(reader));
        }

        return items;
    }

    private static void AddParameters(SqliteCommand command, IEnumerable<SqliteParameter> parameters)
    {
        foreach (SqliteParameter parameter in parameters)
        {
            command.Parameters.AddWithValue(parameter.ParameterName, parameter.Value ?? DBNull.Value);
        }
    }

    private static string BuildWhereClause(Guid userId, RatingQueryRequest request, List<SqliteParameter> parameters)
    {
        StringBuilder builder = new();
        builder.Append("WHERE user_id = @userId");
        parameters.Add(new SqliteParameter("@userId", userId.ToString("D", CultureInfo.InvariantCulture)));

        if (request.Score.HasValue)
        {
            builder.Append(" AND score = @score");
            parameters.Add(new SqliteParameter("@score", request.Score.Value));
        }

        if (request.IsRated.HasValue)
        {
            builder.Append(request.IsRated.Value ? " AND score > 0" : " AND score = 0");
        }

        if (request.IsPendingDelete.HasValue)
        {
            builder.Append(" AND is_pending_delete = @isPendingDelete");
            parameters.Add(new SqliteParameter("@isPendingDelete", request.IsPendingDelete.Value ? 1 : 0));
        }

        if (request.RatedAfterUtc.HasValue)
        {
            builder.Append(" AND rated_at IS NOT NULL AND rated_at >= @ratedAfterUtc");
            parameters.Add(new SqliteParameter("@ratedAfterUtc", FormatDate(request.RatedAfterUtc.Value)));
        }

        if (request.RatedBeforeUtc.HasValue)
        {
            builder.Append(" AND rated_at IS NOT NULL AND rated_at <= @ratedBeforeUtc");
            parameters.Add(new SqliteParameter("@ratedBeforeUtc", FormatDate(request.RatedBeforeUtc.Value)));
        }

        return builder.ToString();
    }

    private static string ResolveSortColumn(string? sortBy)
    {
        string normalized = sortBy?.Trim().Replace("_", string.Empty, StringComparison.Ordinal).ToLowerInvariant() ?? "updatedat";
        return normalized switch
        {
            "score" => "score",
            "createdat" => "created_at",
            "ratedat" => "rated_at",
            "lastplayedat" => "last_played_at",
            _ => "updated_at"
        };
    }

    private static string ResolveSortDirection(string? sortOrder)
    {
        return string.Equals(sortOrder, "asc", StringComparison.OrdinalIgnoreCase) ? "ASC" : "DESC";
    }

    private static UserItemRating ReadRating(SqliteDataReader reader)
    {
        return new UserItemRating
        {
            Id = reader.GetInt64(0),
            UserId = Guid.Parse(reader.GetString(1)),
            ItemId = Guid.Parse(reader.GetString(2)),
            Score = reader.GetInt32(3),
            IsPendingDelete = reader.GetInt32(4) == 1,
            LastPlayedAt = ReadNullableDateTimeOffset(reader, 5),
            RatedAt = ReadNullableDateTimeOffset(reader, 6),
            UpdatedAt = ReadRequiredDateTimeOffset(reader, 7),
            CreatedAt = ReadRequiredDateTimeOffset(reader, 8)
        };
    }

    private static DateTimeOffset? ReadNullableDateTimeOffset(SqliteDataReader reader, int ordinal)
    {
        if (reader.IsDBNull(ordinal))
        {
            return null;
        }

        string value = reader.GetString(ordinal);
        return ParseDate(value);
    }

    private static DateTimeOffset ReadRequiredDateTimeOffset(SqliteDataReader reader, int ordinal)
    {
        string value = reader.GetString(ordinal);
        return ParseDate(value);
    }

    private static DateTimeOffset ParseDate(string value)
    {
        return DateTimeOffset.ParseExact(value, "O", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
    }

    private static string FormatDate(DateTimeOffset value)
    {
        return value.ToString("O", CultureInfo.InvariantCulture);
    }

    private async Task<IReadOnlyList<UserItemRating>> GetManyAsync(Guid userId, IReadOnlyList<Guid> itemIds, CancellationToken cancellationToken)
    {
        if (itemIds.Count == 0)
        {
            return Array.Empty<UserItemRating>();
        }

        List<UserItemRating> items = [];

        await using SqliteConnection connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        foreach (Guid itemId in itemIds)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await using SqliteCommand command = connection.CreateCommand();
            command.CommandText = """
                SELECT id, user_id, item_id, score, is_pending_delete, last_played_at, rated_at, updated_at, created_at
                FROM user_item_ratings
                WHERE user_id = @userId AND item_id = @itemId
                LIMIT 1;
                """;
            command.Parameters.AddWithValue("@userId", userId.ToString("D", CultureInfo.InvariantCulture));
            command.Parameters.AddWithValue("@itemId", itemId.ToString("D", CultureInfo.InvariantCulture));

            await using SqliteDataReader reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                items.Add(ReadRating(reader));
            }
        }

        return items;
    }
}
