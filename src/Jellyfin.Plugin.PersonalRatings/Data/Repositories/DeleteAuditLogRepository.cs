using System.Globalization;
using Jellyfin.Plugin.PersonalRatings.Models.Entities;
using Microsoft.Data.Sqlite;

namespace Jellyfin.Plugin.PersonalRatings.Data.Repositories;

internal sealed class DeleteAuditLogRepository : IDeleteAuditLogRepository
{
    private readonly ISqliteConnectionFactory _connectionFactory;

    public DeleteAuditLogRepository(ISqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
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
}
