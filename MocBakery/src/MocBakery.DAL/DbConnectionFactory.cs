using System.Data;
using System.Data.Common;
using MocBakery.Models;
namespace MocBakery.DAL;
public sealed class DbConnectionFactory(string connectionString)
{
    public DbConnection Open()
    {
        // Resolve the configured Microsoft provider without coupling business tests to NuGet.
        var type = Type.GetType("Microsoft.Data.SqlClient.SqlClientFactory, Microsoft.Data.SqlClient")
            ?? throw new InvalidOperationException("Thiếu Microsoft.Data.SqlClient. Chạy dotnet restore khi có mạng.");
        var factory = (DbProviderFactory?)type.GetField("Instance")?.GetValue(null)
            ?? throw new InvalidOperationException("Không nạp được Microsoft.Data.SqlClient.");
        var connection = factory.CreateConnection() ?? throw new InvalidOperationException("Không tạo được kết nối.");
        connection.ConnectionString = connectionString;
        try { connection.Open(); return connection; }
        catch { connection.Dispose(); throw; }
    }
}
internal static class Db
{
    public static DbCommand Command(DbConnection connection, string sql, DbTransaction? transaction = null)
    {
        var command = connection.CreateCommand();
        command.CommandText = sql; command.Transaction = transaction;
        command.CommandTimeout = 15;
        return command;
    }
    public static void Param(this DbCommand command, string name, object? value, DbType type, int size = 0)
    {
        var p = command.CreateParameter(); p.ParameterName = name; p.DbType = type;
        if (size != 0) p.Size = size;
        p.Value = value ?? DBNull.Value; command.Parameters.Add(p);
    }
    public static bool IsDuplicate(DbException ex) => ex.GetType().GetProperty("Number")?.GetValue(ex) is int number && number is 2601 or 2627;
}
