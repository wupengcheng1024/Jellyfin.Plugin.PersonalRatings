using System.Globalization;
using Jellyfin.Plugin.PersonalRatings.Models.Entities;
using Microsoft.Data.Sqlite;

namespace Jellyfin.Plugin.PersonalRatings.Data.Repositories;

internal sealed class TagRepository : ITagRepository
{
    private readonly ISqliteConnectionFactory _connectionFactory;

    public TagRepository(ISqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<TagDefinition>> ListDefinitionsAsync(bool includeDisabled, CancellationToken cancellationToken)
    {
        await using SqliteConnection connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText = includeDisabled
            ? """
                SELECT id, name, color, sort_order, is_enabled, created_at, updated_at
                FROM tag_definitions
                ORDER BY sort_order ASC, name COLLATE NOCASE ASC, id ASC;
                """
            : """
                SELECT id, name, color, sort_order, is_enabled, created_at, updated_at
                FROM tag_definitions
                WHERE is_enabled = 1
                ORDER BY sort_order ASC, name COLLATE NOCASE ASC, id ASC;
                """;

        List<TagDefinition> items = [];
        await using SqliteDataReader reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            items.Add(ReadDefinition(reader));
        }

        return items;
    }

    public async Task<IReadOnlyList<TagDefinition>> GetDefinitionsByIdsAsync(IReadOnlyList<long> tagIds, CancellationToken cancellationToken)
    {
        if (tagIds.Count == 0)
        {
            return Array.Empty<TagDefinition>();
        }

        await using SqliteConnection connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using SqliteCommand command = connection.CreateCommand();
        List<string> parameterNames = [];

        for (int index = 0; index < tagIds.Count; index++)
        {
            string parameterName = "@tagId" + index.ToString(CultureInfo.InvariantCulture);
            parameterNames.Add(parameterName);
            command.Parameters.AddWithValue(parameterName, tagIds[index]);
        }

        command.CommandText = $"""
            SELECT id, name, color, sort_order, is_enabled, created_at, updated_at
            FROM tag_definitions
            WHERE id IN ({string.Join(", ", parameterNames)})
            ORDER BY sort_order ASC, name COLLATE NOCASE ASC, id ASC;
            """;

        List<TagDefinition> items = [];
        await using SqliteDataReader reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            items.Add(ReadDefinition(reader));
        }

        return items;
    }

    public async Task<TagDefinition?> GetDefinitionAsync(long tagId, CancellationToken cancellationToken)
    {
        await using SqliteConnection connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT id, name, color, sort_order, is_enabled, created_at, updated_at
            FROM tag_definitions
            WHERE id = @tagId
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("@tagId", tagId);

        await using SqliteDataReader reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            return null;
        }

        return ReadDefinition(reader);
    }

    public async Task<TagDefinition?> GetDefinitionByNameAsync(string name, CancellationToken cancellationToken)
    {
        await using SqliteConnection connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT id, name, color, sort_order, is_enabled, created_at, updated_at
            FROM tag_definitions
            WHERE name = @name COLLATE NOCASE
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("@name", name);

        await using SqliteDataReader reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            return null;
        }

        return ReadDefinition(reader);
    }

    public async Task<TagDefinition> CreateDefinitionAsync(TagDefinition definition, CancellationToken cancellationToken)
    {
        await using SqliteConnection connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO tag_definitions (
                name,
                color,
                sort_order,
                is_enabled,
                created_at,
                updated_at
            )
            VALUES (
                @name,
                @color,
                @sortOrder,
                @isEnabled,
                @createdAt,
                @updatedAt
            );

            SELECT last_insert_rowid();
            """;
        command.Parameters.AddWithValue("@name", definition.Name);
        command.Parameters.AddWithValue("@color", definition.Color);
        command.Parameters.AddWithValue("@sortOrder", definition.SortOrder);
        command.Parameters.AddWithValue("@isEnabled", definition.IsEnabled ? 1 : 0);
        command.Parameters.AddWithValue("@createdAt", FormatDate(definition.CreatedAt));
        command.Parameters.AddWithValue("@updatedAt", FormatDate(definition.UpdatedAt));

        object? scalar = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        long createdId = Convert.ToInt64(scalar, CultureInfo.InvariantCulture);
        TagDefinition? created = await GetDefinitionAsync(createdId, cancellationToken).ConfigureAwait(false);
        return created ?? throw new InvalidOperationException("Failed to load tag definition after creation.");
    }

    public async Task<TagDefinition?> UpdateDefinitionAsync(TagDefinition definition, CancellationToken cancellationToken)
    {
        await using SqliteConnection connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            UPDATE tag_definitions
            SET
                name = @name,
                color = @color,
                sort_order = @sortOrder,
                is_enabled = @isEnabled,
                updated_at = @updatedAt
            WHERE id = @tagId;
            """;
        command.Parameters.AddWithValue("@tagId", definition.Id);
        command.Parameters.AddWithValue("@name", definition.Name);
        command.Parameters.AddWithValue("@color", definition.Color);
        command.Parameters.AddWithValue("@sortOrder", definition.SortOrder);
        command.Parameters.AddWithValue("@isEnabled", definition.IsEnabled ? 1 : 0);
        command.Parameters.AddWithValue("@updatedAt", FormatDate(definition.UpdatedAt));

        int affectedRows = await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        if (affectedRows <= 0)
        {
            return null;
        }

        return await GetDefinitionAsync(definition.Id, cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> DeleteDefinitionAsync(long tagId, CancellationToken cancellationToken)
    {
        await using SqliteConnection connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using SqliteTransaction transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        await using (SqliteCommand deleteRelationsCommand = connection.CreateCommand())
        {
            deleteRelationsCommand.Transaction = transaction;
            deleteRelationsCommand.CommandText = """
                DELETE FROM user_item_tags
                WHERE tag_id = @tagId;
                """;
            deleteRelationsCommand.Parameters.AddWithValue("@tagId", tagId);
            await deleteRelationsCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        int affectedRows;
        await using (SqliteCommand deleteDefinitionCommand = connection.CreateCommand())
        {
            deleteDefinitionCommand.Transaction = transaction;
            deleteDefinitionCommand.CommandText = """
                DELETE FROM tag_definitions
                WHERE id = @tagId;
                """;
            deleteDefinitionCommand.Parameters.AddWithValue("@tagId", tagId);
            affectedRows = await deleteDefinitionCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        return affectedRows > 0;
    }

    public async Task<IReadOnlyList<TagDefinition>> GetItemTagsAsync(Guid userId, Guid itemId, CancellationToken cancellationToken)
    {
        IReadOnlyDictionary<Guid, IReadOnlyList<TagDefinition>> map = await GetItemTagsMapAsync(
            userId,
            [itemId],
            cancellationToken).ConfigureAwait(false);

        if (!map.TryGetValue(itemId, out IReadOnlyList<TagDefinition>? tags))
        {
            return Array.Empty<TagDefinition>();
        }

        return tags;
    }

    public async Task<IReadOnlyDictionary<Guid, IReadOnlyList<TagDefinition>>> GetItemTagsMapAsync(
        Guid userId,
        IReadOnlyList<Guid> itemIds,
        CancellationToken cancellationToken)
    {
        Dictionary<Guid, IReadOnlyList<TagDefinition>> empty = [];
        if (itemIds.Count == 0)
        {
            return empty;
        }

        await using SqliteConnection connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using SqliteCommand command = connection.CreateCommand();
        List<string> parameterNames = [];

        for (int index = 0; index < itemIds.Count; index++)
        {
            string parameterName = "@itemId" + index.ToString(CultureInfo.InvariantCulture);
            parameterNames.Add(parameterName);
            command.Parameters.AddWithValue(parameterName, itemIds[index].ToString("D", CultureInfo.InvariantCulture));
        }

        command.Parameters.AddWithValue("@userId", userId.ToString("D", CultureInfo.InvariantCulture));
        command.CommandText = $"""
            SELECT
                uit.item_id,
                td.id,
                td.name,
                td.color,
                td.sort_order,
                td.is_enabled,
                td.created_at,
                td.updated_at
            FROM user_item_tags uit
            INNER JOIN tag_definitions td
                ON td.id = uit.tag_id
            WHERE uit.user_id = @userId
                AND uit.item_id IN ({string.Join(", ", parameterNames)})
            ORDER BY td.sort_order ASC, td.name COLLATE NOCASE ASC, td.id ASC;
            """;

        Dictionary<Guid, List<TagDefinition>> map = [];
        await using SqliteDataReader reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            Guid itemId = Guid.Parse(reader.GetString(0));
            if (!map.TryGetValue(itemId, out List<TagDefinition>? items))
            {
                items = [];
                map[itemId] = items;
            }

            items.Add(new TagDefinition
            {
                Id = reader.GetInt64(1),
                Name = reader.GetString(2),
                Color = reader.GetString(3),
                SortOrder = reader.GetInt32(4),
                IsEnabled = reader.GetInt32(5) == 1,
                CreatedAt = ParseDate(reader.GetString(6)),
                UpdatedAt = ParseDate(reader.GetString(7))
            });
        }

        Dictionary<Guid, IReadOnlyList<TagDefinition>> result = [];
        foreach ((Guid itemId, List<TagDefinition> items) in map)
        {
            result[itemId] = items;
        }

        return result;
    }

    public async Task ReplaceItemTagsAsync(Guid userId, Guid itemId, IReadOnlyList<long> tagIds, CancellationToken cancellationToken)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        string nowText = FormatDate(now);

        await using SqliteConnection connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using SqliteTransaction transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        await using (SqliteCommand deleteCommand = connection.CreateCommand())
        {
            deleteCommand.Transaction = transaction;
            deleteCommand.CommandText = """
                DELETE FROM user_item_tags
                WHERE user_id = @userId AND item_id = @itemId;
                """;
            deleteCommand.Parameters.AddWithValue("@userId", userId.ToString("D", CultureInfo.InvariantCulture));
            deleteCommand.Parameters.AddWithValue("@itemId", itemId.ToString("D", CultureInfo.InvariantCulture));
            await deleteCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        foreach (long tagId in tagIds)
        {
            await using SqliteCommand insertCommand = connection.CreateCommand();
            insertCommand.Transaction = transaction;
            insertCommand.CommandText = """
                INSERT INTO user_item_tags (
                    user_id,
                    item_id,
                    tag_id,
                    created_at,
                    updated_at
                )
                VALUES (
                    @userId,
                    @itemId,
                    @tagId,
                    @createdAt,
                    @updatedAt
                );
                """;
            insertCommand.Parameters.AddWithValue("@userId", userId.ToString("D", CultureInfo.InvariantCulture));
            insertCommand.Parameters.AddWithValue("@itemId", itemId.ToString("D", CultureInfo.InvariantCulture));
            insertCommand.Parameters.AddWithValue("@tagId", tagId);
            insertCommand.Parameters.AddWithValue("@createdAt", nowText);
            insertCommand.Parameters.AddWithValue("@updatedAt", nowText);
            await insertCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task AddTagsAsync(Guid userId, IReadOnlyList<Guid> itemIds, IReadOnlyList<long> tagIds, CancellationToken cancellationToken)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        string nowText = FormatDate(now);

        await using SqliteConnection connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using SqliteTransaction transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        foreach (Guid itemId in itemIds)
        {
            foreach (long tagId in tagIds)
            {
                await using SqliteCommand command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = """
                    INSERT INTO user_item_tags (
                        user_id,
                        item_id,
                        tag_id,
                        created_at,
                        updated_at
                    )
                    VALUES (
                        @userId,
                        @itemId,
                        @tagId,
                        @createdAt,
                        @updatedAt
                    )
                    ON CONFLICT(user_id, item_id, tag_id) DO UPDATE SET
                        updated_at = excluded.updated_at;
                    """;
                command.Parameters.AddWithValue("@userId", userId.ToString("D", CultureInfo.InvariantCulture));
                command.Parameters.AddWithValue("@itemId", itemId.ToString("D", CultureInfo.InvariantCulture));
                command.Parameters.AddWithValue("@tagId", tagId);
                command.Parameters.AddWithValue("@createdAt", nowText);
                command.Parameters.AddWithValue("@updatedAt", nowText);
                await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task RemoveTagsAsync(Guid userId, IReadOnlyList<Guid> itemIds, IReadOnlyList<long> tagIds, CancellationToken cancellationToken)
    {
        if (itemIds.Count == 0 || tagIds.Count == 0)
        {
            return;
        }

        await using SqliteConnection connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using SqliteCommand command = connection.CreateCommand();
        List<string> itemParameterNames = [];
        List<string> tagParameterNames = [];

        for (int index = 0; index < itemIds.Count; index++)
        {
            string parameterName = "@itemId" + index.ToString(CultureInfo.InvariantCulture);
            itemParameterNames.Add(parameterName);
            command.Parameters.AddWithValue(parameterName, itemIds[index].ToString("D", CultureInfo.InvariantCulture));
        }

        for (int index = 0; index < tagIds.Count; index++)
        {
            string parameterName = "@tagId" + index.ToString(CultureInfo.InvariantCulture);
            tagParameterNames.Add(parameterName);
            command.Parameters.AddWithValue(parameterName, tagIds[index]);
        }

        command.Parameters.AddWithValue("@userId", userId.ToString("D", CultureInfo.InvariantCulture));
        command.CommandText = $"""
            DELETE FROM user_item_tags
            WHERE user_id = @userId
                AND item_id IN ({string.Join(", ", itemParameterNames)})
                AND tag_id IN ({string.Join(", ", tagParameterNames)});
            """;

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static TagDefinition ReadDefinition(SqliteDataReader reader)
    {
        return new TagDefinition
        {
            Id = reader.GetInt64(0),
            Name = reader.GetString(1),
            Color = reader.GetString(2),
            SortOrder = reader.GetInt32(3),
            IsEnabled = reader.GetInt32(4) == 1,
            CreatedAt = ParseDate(reader.GetString(5)),
            UpdatedAt = ParseDate(reader.GetString(6))
        };
    }

    private static string FormatDate(DateTimeOffset value)
    {
        return value.ToString("O", CultureInfo.InvariantCulture);
    }

    private static DateTimeOffset ParseDate(string value)
    {
        return DateTimeOffset.ParseExact(value, "O", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
    }
}
