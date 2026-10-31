using System.Data;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

namespace API_kiroldegia_db.Controllers;

public partial class ErreserbakKontroladorea
{
    /// <summary>
    /// Soft Delete egindako erreserba bat berrezarri / desegin ezabaketa (RESTORE).
    /// </summary>
    [HttpPut("{id}/restore")]
    public async Task<IActionResult> BerreskuratuErreserba(int id)
    {
        await using var kon = await SortuKonexioaAsync();
        await using var tx = await kon.BeginTransactionAsync(IsolationLevel.ReadCommitted);

        try
        {
            int pistaId;
            string data;
            string ordua;
            bool ezabatua;

            var cmdGet = new MySqlCommand("SELECT pista_id, data, ordua, ezabatua FROM erreserba WHERE id = @id FOR UPDATE;", kon, tx);
            cmdGet.Parameters.AddWithValue("@id", id);
            using (var reader = await cmdGet.ExecuteReaderAsync())
            {
                if (!await reader.ReadAsync())
                {
                    await tx.RollbackAsync();
                    return NotFoundError($"Ez da aurkitu {id} ID-a duen erreserbarik.");
                }
                ezabatua = reader.GetBoolean("ezabatua");
                if (!ezabatua)
                {
                    await tx.RollbackAsync();
                    return BadRequestError("Erreserba hau aktibo dago jada, ez dago ezabatuta.");
                }
                pistaId = reader.GetInt32("pista_id");
                data = Convert.ToDateTime(reader["data"]).ToString("yyyy-MM-dd");
                ordua = reader["ordua"].ToString() ?? "";
            }

            // Pista bera ezabatuta ez dagoela egiaztatu
            var cmdPista = new MySqlCommand("SELECT ezabatua FROM pista WHERE id = @p FOR UPDATE;", kon, tx);
            cmdPista.Parameters.AddWithValue("@p", pistaId);
            bool pistaEzabatua = Convert.ToBoolean(await cmdPista.ExecuteScalarAsync());
            if (pistaEzabatua)
            {
                await tx.RollbackAsync();
                return ConflictError("Ezin da erreserba berreskuratu: pista bera desgaituta (Soft Delete) dago.");
            }

            // Hutsunea tartean hartuta ote dagoen begiratu
            var cmdCheck = new MySqlCommand(@"
                SELECT COUNT(*) FROM erreserba 
                WHERE pista_id = @p AND data = @d AND ordua = @o AND ezabatua = FALSE AND id != @id
                FOR UPDATE;", kon, tx);
            cmdCheck.Parameters.AddWithValue("@p", pistaId);
            cmdCheck.Parameters.AddWithValue("@d", data);
            cmdCheck.Parameters.AddWithValue("@o", ordua);
            cmdCheck.Parameters.AddWithValue("@id", id);

            long okupatua = Convert.ToInt64(await cmdCheck.ExecuteScalarAsync());
            if (okupatua > 0)
            {
                await tx.RollbackAsync();
                return ConflictError("Ezin da berreskuratu: beste erreserba aktibo bat dago pista eta ordu berean.");
            }

            // Restore egin
            var cmdRestore = new MySqlCommand("UPDATE erreserba SET ezabatua = FALSE, ezabaketa_data = NULL WHERE id = @id;", kon, tx);
            cmdRestore.Parameters.AddWithValue("@id", id);
            await cmdRestore.ExecuteNonQueryAsync();

            await tx.CommitAsync();
            return Success(new { id, mezua = $"[{id}] erreserba arrakastaz berreskuratu da!" }, 200, "Erreserba berreskuratu da.");
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return ServerError("Errorea berreskuratzean: " + ex.Message);
        }
    }
}

