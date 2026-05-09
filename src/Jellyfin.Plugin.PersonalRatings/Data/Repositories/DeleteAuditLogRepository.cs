using System.Globalization;
using Jellyfin.Plugin.PersonalRatings.Data;
using Jellyfin.Plugin.PersonalRatings.Models.Entities;
using Jellyfin.Plugin.PersonalRatings.Models.Requests;
using Microsoft.Data.Sqlite;

namespace Jellyfin.Plugin.PersonalRatings.Data.Repositories;

internal sealed class DeleteAuditLogRepository : IDeleteAuditLogRepository
{
    private readonly ISqliteConnectionFactory _connectionFactory;

    public DeleteAuditLogRepository(ISqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public Task AddAsync(DeleteAuditLog auditLog, CancellationToken cancellationToken)
    {
        DeleteAuditLog[] auditLogs = [auditLog];
        return AddRangeAsync(auditLogs, cancellationToken);
    }

    public async Task AddRangeAsync(IReadOnlyList<DeleteAuditLog> auditLogs, CancellationToken cancellationToken)
    {
        if (auditLogs.Count == 0)
        {
            return;
        }

        await using SqliteConnection connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using SqliteTransaction transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        foreach (DeleteAuditLog auditLog in auditLogs)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await using SqliteCommand command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO delete_audit_logs (
                    operator_user_id,
                    item_id,
                    item_name,
                    action,
                    result,
                    message,
                    created_at
                )
                VALUES (
                    @operatorUserId,
                    @itemId,
                    @itemName,
                    @action,
                    @result,
                    @message,
                    @createdAt
                );
                """;
            command.Parameters.AddWithValue("@operatorUserId", auditLog.OperatorUserId.ToString("D", CultureInfo.InvariantCulture));
            command.Parameters.AddWithValue("@itemId", auditLog.ItemId.ToString("D", CultureInfo.InvariantCulture));
            command.Parameters.AddWithValue("@itemName", (object?)auditLog.ItemName ?? DBNull.Value);
            command.Parameters.AddWithValue("@action", auditLog.Action);
            command.Parameters.AddWithValue("@result", auditLog.Result);
            command.Parameters.AddWithValue("@message", (object?)auditLog.Message ?? DBNull.Value);
            command.Parameters.AddWithValue("@createdAt", auditLog.CreatedAt.ToString("O", CultureInfo.InvariantCulture));

            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<PagedQueryResult<DeleteAuditLog>> QueryPageAsync(AuditLogQueryRequest request, CancellationToken cancellationToken)
    {
        List<SqliteParameter> parameters = [];
        string whereClause = BuildWhereClause(request, parameters);
        int offset = (request.PageNumber - 1) * request.PageSize;

        await using SqliteConnection connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using SqliteCommand countCommand = connection.CreateCommand();
        countCommand.CommandText = "SELECT COUNT(1) FROM delete_audit_logs " + whereClause + ";";
        AddParameters(countCommand, parameters);
        object? countValue = await countCommand.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);

        await using SqliteCommand itemsCommand = connection.CreateCommand();
        itemsCommand.CommandText = $"""
            SELECT id, operator_user_id, item_id, item_name, action, result, message, created_at
            FROM delete_audit_logs
            {whereClause}
            ORDER BY created_at DESC, id DESC
            LIMIT @limit OFFSET @offset;
            """;
        AddParameters(itemsCommand, parameters);
        itemsCommand.Parameters.AddWithValue("@limit", request.PageSize);
        itemsCommand.Parameters.AddWithValue("@offset", offset);

        List<DeleteAuditLog> items = [];
        await using SqliteDataReader reader = await itemsCommand.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            items.Add(ReadAuditLog(reader));
        }

        return new PagedQueryResult<DeleteAuditLog>
        {
            Items = items,
            TotalCount = Convert.ToInt32(countValue, CultureInfo.InvariantCulture)
        };
    }

    private static string BuildWhereClause(AuditLogQueryRequest request, List<SqliteParameter> parameters)
    {
        List<string> clauses = [];

        if (request.CreatedAfterUtc.HasValue)
        {
            clauses.Add("created_at >= @createdAfterUtc");
            parameters.Add(new SqliteParameter("@createdAfterUtc", request.CreatedAfterUtc.Value.ToString("O", CultureInfo.InvariantCulture)));
        }

        if (request.CreatedBeforeUtc.HasValue)
        {
            clauses.Add("created_at <= @createdBeforeUtc");
            parameters.Add(new SqliteParameter("@createdBeforeUtc", request.CreatedBeforeUtc.Value.ToString("O", CultureInfo.InvariantCulture)));
        }

        if (!string.IsNullOrWhiteSpace(request.Result))
        {
            clauses.Add("result = @result");
            parameters.Add(new SqliteParameter("@result", request.Result.Trim()));
        }

        if (!string.IsNullOrWhiteSpace(request.ItemId))
        {
            clauses.Add("item_id = @itemId");
            parameters.Add(new SqliteParameter("@itemId", request.ItemId.Trim()));
        }

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            clauses.Add("(item_name LIKE @keyword OR message LIKE @keyword OR action LIKE @keyword)");
            parameters.Add(new SqliteParameter("@keyword", "%" + request.Keyword.Trim() + "%"));
        }

        if (clauses.Count == 0)
        {
            return string.Empty;
        }

        return "WHERE " + string.Join(" AND ", clauses);
    }

    private static void AddParameters(SqliteCommand command, IEnumerable<SqliteParameter> parameters)
    {
        foreach (SqliteParameter parameter in parameters)
        {
            SqliteParameter clone = new(parameter.ParameterName, parameter.Value);
            command.Parameters.Add(clone);
        }
    }

    private static DeleteAuditLog ReadAuditLog(SqliteDataReader reader)
    {
        return new DeleteAuditLog
        {
            Id = reader.GetInt64(0),
            OperatorUserId = Guid.Parse(reader.GetString(1)),
            ItemId = Guid.Parse(reader.GetString(2)),
            ItemName = reader.IsDBNull(3) ? null : reader.GetString(3),
            Action = reader.GetString(4),
            Result = reader.GetString(5),
            Message = reader.IsDBNull(6) ? null : reader.GetString(6),
            CreatedAt = DateTimeOffset.Parse(reader.GetString(7), CultureInfo.InvariantCulture)
        };
    }
}
