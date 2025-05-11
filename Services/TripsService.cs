using Microsoft.Data.SqlClient;
using Tutorial8.Models.DTOs;

namespace Tutorial8.Services;

public class TripsService : ITripsService
{
    private readonly string _connectionString =
        "Server=localhost\\SQLEXPRESS01;Database=APBD;Trusted_Connection=True;TrustServerCertificate=True;";
    
    public async Task<List<TripDTO>> GetTrips()
    {
        var trips = new List<TripDTO>();

        string command = "SELECT IdTrip, Name FROM Trip";
        
        using (SqlConnection conn = new SqlConnection(_connectionString))
        using (SqlCommand cmd = new SqlCommand(command, conn))
        {
            await conn.OpenAsync();

            using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    int idOrdinal = reader.GetOrdinal("IdTrip");
                    trips.Add(new TripDTO()
                    {
                        Id = reader.GetInt32(idOrdinal),
                        Name = reader.GetString(1),
                    });
                }
            }
        }
        

        return trips;
    }

    public async Task<List<ClientTripDTO>> GetClientTrips(int clientId)
    {
        var trips = new List<ClientTripDTO>();

        const string query = @"
        SELECT t.Name, t.Description, t.DateFrom, t.DateTo, ct.RegisteredAt, ct.PaymentDate
        FROM Client_Trip ct
        JOIN Trip t ON ct.IdTrip = t.IdTrip
        WHERE ct.IdClient = @IdClient";

        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@IdClient", clientId);

        await conn.OpenAsync();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            trips.Add(new ClientTripDTO
            {
                TripName = reader["Name"].ToString(),
                Description = reader["Description"].ToString(),
                DateFrom = (DateTime)reader["DateFrom"],
                DateTo = (DateTime)reader["DateTo"],
                RegisteredAt = (DateTime)reader["RegisteredAt"],
                PaymentDate = reader["PaymentDate"] == DBNull.Value ? null : (DateTime?)reader["PaymentDate"]
            });
        }

        return trips;
    }
    
    public async Task<int> AddClient(ClientDTO client)
    {
        const string query = @"
        INSERT INTO Client (FirstName, LastName, Email, Telephone, Pesel)
        OUTPUT INSERTED.IdClient
        VALUES (@FirstName, @LastName, @Email, @Telephone, @Pesel);";

        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand(query, conn);

        cmd.Parameters.AddWithValue("@FirstName", client.FirstName);
        cmd.Parameters.AddWithValue("@LastName", client.LastName);
        cmd.Parameters.AddWithValue("@Email", client.Email);
        cmd.Parameters.AddWithValue("@Telephone", client.Telephone);
        cmd.Parameters.AddWithValue("@Pesel", client.Pesel);

        await conn.OpenAsync();
        var result = await cmd.ExecuteScalarAsync();

        return Convert.ToInt32(result);
    }

    public async Task<bool> RegisterClientForTrip(int clientId, int tripId)
{
    using var conn = new SqlConnection(_connectionString);
    await conn.OpenAsync();

    using var tx = conn.BeginTransaction();

    try
    {
        var checkClient = new SqlCommand("SELECT 1 FROM Client WHERE IdClient = @IdClient", conn, tx);
        checkClient.Parameters.AddWithValue("@IdClient", clientId);
        var clientExists = await checkClient.ExecuteScalarAsync();
        if (clientExists == null) return false;
        
        var checkTrip = new SqlCommand("SELECT MaxPeople FROM Trip WHERE IdTrip = @IdTrip", conn, tx);
        checkTrip.Parameters.AddWithValue("@IdTrip", tripId);
        var maxPeopleObj = await checkTrip.ExecuteScalarAsync();
        if (maxPeopleObj == null) return false;
        int maxPeople = Convert.ToInt32(maxPeopleObj);
        
        var countCommand = new SqlCommand("SELECT COUNT(*) FROM Client_Trip WHERE IdTrip = @IdTrip", conn, tx);
        countCommand.Parameters.AddWithValue("@IdTrip", tripId);
        int currentCount = (int)await countCommand.ExecuteScalarAsync();
        if (currentCount >= maxPeople) return false;
        
        var existsCommand = new SqlCommand("SELECT 1 FROM Client_Trip WHERE IdClient = @IdClient AND IdTrip = @IdTrip", conn, tx);
        existsCommand.Parameters.AddWithValue("@IdClient", clientId);
        existsCommand.Parameters.AddWithValue("@IdTrip", tripId);
        var alreadyExists = await existsCommand.ExecuteScalarAsync();
        if (alreadyExists != null) return false;
        
        var insertCommand = new SqlCommand(@"
            INSERT INTO Client_Trip (IdClient, IdTrip, RegisteredAt)
            VALUES (@IdClient, @IdTrip, @Now)", conn, tx);

        insertCommand.Parameters.AddWithValue("@IdClient", clientId);
        insertCommand.Parameters.AddWithValue("@IdTrip", tripId);
        insertCommand.Parameters.AddWithValue("@Now", DateTime.Now);

        await insertCommand.ExecuteNonQueryAsync();

        await tx.CommitAsync();
        return true;
    }
    catch
    {
        await tx.RollbackAsync();
        return false;
    }
}
    public async Task<bool> DeleteClientFromTrip(int clientId, int tripId)
    {
        using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        const string checkQuery = @"
        SELECT 1 FROM Client_Trip 
        WHERE IdClient = @IdClient AND IdTrip = @IdTrip";

        using var checkCmd = new SqlCommand(checkQuery, conn);
        checkCmd.Parameters.AddWithValue("@IdClient", clientId);
        checkCmd.Parameters.AddWithValue("@IdTrip", tripId);

        var exists = await checkCmd.ExecuteScalarAsync();
        if (exists == null) return false;

        const string deleteQuery = @"
        DELETE FROM Client_Trip 
        WHERE IdClient = @IdClient AND IdTrip = @IdTrip";

        using var deleteCmd = new SqlCommand(deleteQuery, conn);
        deleteCmd.Parameters.AddWithValue("@IdClient", clientId);
        deleteCmd.Parameters.AddWithValue("@IdTrip", tripId);

        await deleteCmd.ExecuteNonQueryAsync();
        return true;
    }



}