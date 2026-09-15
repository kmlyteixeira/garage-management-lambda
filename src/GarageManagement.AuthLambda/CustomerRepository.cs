using Npgsql;

namespace GarageManagement.AuthLambda;

public sealed class CustomerRepository
{
    private readonly string connectionString;

    public CustomerRepository(string connectionString)
    {
        this.connectionString = connectionString;
    }

    public async Task<CustomerAuthenticationRecord?> FindByCpfAsync(string cpf, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = """
            SELECT "Id", "Document", "IsActive"
            FROM "AppCustomers"
            WHERE "Document" = @cpf
            LIMIT 1
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("cpf", cpf);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new CustomerAuthenticationRecord(
            reader.GetGuid(0),
            reader.GetString(1),
            reader.GetBoolean(2));
    }
}
