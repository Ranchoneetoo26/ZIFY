using System.Data.Common;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Logging;

namespace Zyft.Infrastructure.Persistence;

/// <summary>
/// Atualização aditiva do esquema em SQLite (só desenvolvimento). <c>EnsureCreated</c> não altera um banco que já
/// existe e as migrations são do PostgreSQL; então, depois dele, criamos as tabelas e os índices que faltam e
/// adicionamos as colunas novas (com valor padrão quando obrigatórias). Nunca remove nem altera nada existente.
/// </summary>
internal static partial class SqliteSchemaUpgrader
{
    [GeneratedRegex(@";\s*(?:\r?\n|$)")]
    private static partial Regex StatementEnd();

    [GeneratedRegex(@"^CREATE TABLE ""(?<t>[^""]+)""", RegexOptions.IgnoreCase)]
    private static partial Regex CreateTable();

    [GeneratedRegex(@"^CREATE (?<u>UNIQUE )?INDEX """, RegexOptions.IgnoreCase)]
    private static partial Regex CreateIndex();

    public static async Task UpgradeAsync(ZyftDbContext db, ILogger logger, CancellationToken ct)
    {
        await db.Database.OpenConnectionAsync(ct);
        try
        {
            var conn = db.Database.GetDbConnection();
            var tables = await ReadNamesAsync(conn, "SELECT name FROM sqlite_master WHERE type = 'table'", ct);
            var changes = 0;

            var statements = StatementEnd().Split(db.Database.GenerateCreateScript()).Select(s => s.Trim()).Where(s => s.Length > 0).ToList();
            foreach (var sql in statements)
            {
                var table = CreateTable().Match(sql);
                if (table.Success && !tables.Contains(table.Groups["t"].Value))
                {
                    await ExecAsync(conn, sql, ct);
                    changes++;
                }
            }

            foreach (var entity in db.Model.GetEntityTypes())
            {
                var tableName = entity.GetTableName();
                if (tableName is null || !tables.Contains(tableName)) continue;
                var store = StoreObjectIdentifier.Table(tableName, entity.GetSchema());
                var columns = await ReadNamesAsync(conn, $"SELECT name FROM pragma_table_info('{tableName}')", ct);
                foreach (var p in entity.GetProperties())
                {
                    var column = p.GetColumnName(store);
                    if (column is null || columns.Contains(column)) continue;
                    var type = p.GetColumnType();
                    var nullability = p.IsColumnNullable(store) ? "NULL" : $"NOT NULL DEFAULT {DefaultFor(p)}";
                    await ExecAsync(conn, $"ALTER TABLE \"{tableName}\" ADD COLUMN \"{column}\" {type} {nullability}", ct);
                    columns.Add(column);
                    changes++;
                }
            }

            // Índices depois das colunas (um índice novo pode usar uma coluna nova).
            foreach (var sql in statements.Where(s => CreateIndex().IsMatch(s)))
                await ExecAsync(conn, CreateIndex().Replace(sql, m => $"CREATE {m.Groups["u"].Value}INDEX IF NOT EXISTS \"", 1), ct);

            if (changes > 0) logger.LogInformation("Esquema SQLite atualizado ({Changes} alterações aditivas)", changes);
        }
        finally
        {
            await db.Database.CloseConnectionAsync();
        }
    }

    private static string DefaultFor(IProperty p)
    {
        var clr = Nullable.GetUnderlyingType(p.ClrType) ?? p.ClrType;
        if (clr == typeof(string) || clr.IsEnum && p.GetProviderClrType() == typeof(string)) return "''";
        if (clr == typeof(DateTime)) return "'0001-01-01 00:00:00'";
        if (clr == typeof(Guid)) return "'00000000-0000-0000-0000-000000000000'";
        return "0";
    }

    private static async Task<HashSet<string>> ReadNamesAsync(DbConnection conn, string sql, CancellationToken ct)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct)) names.Add(reader.GetString(0));
        return names;
    }

    private static async Task ExecAsync(DbConnection conn, string sql, CancellationToken ct)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        await cmd.ExecuteNonQueryAsync(ct);
    }
}
