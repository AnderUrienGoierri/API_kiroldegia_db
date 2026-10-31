using System.Data;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

namespace API_kiroldegia_db.Controllers;

public partial class PistakKontroladorea
{
    /// <summary>
    /// Soft Delete egindako pista berreskuratu (RESTORE).
    /// </summary>
    [HttpPut("{id}/restore")]
    public async Task<IActionResult> BerreskuratuPista(int id)
    {
        await using var kon = await SortuKonexioaAsync();
        await using var tx = await kon.BeginTransactionAsync(IsolationLevel.ReadCommitted);

        try
        {
            var cmdCheck = new MySqlCommand("SELECT id, izena, ezabatua FROM pista WHERE id = @id FOR UPDATE;", kon, tx);
            cmdCheck.Parameters.AddWithValue("@id", id);
            string pistaIzena = "";
            using (var reader = await cmdCheck.ExecuteReaderAsync())
            {
                if (!await reader.ReadAsync())
                {
                    await tx.RollbackAsync();
                    return NotFoundError($"Ez da aurkitu {id} ID-a duen pistarik.");
                }

                pistaIzena = reader.GetString("izena");
                bool ezabatua = reader.GetBoolean("ezabatua");
                if (!ezabatua)
                {
                    await tx.RollbackAsync();
                    return BadRequestError("Pista hau aktibo dago jada, ez dago ezabatuta.");
                }
            }

            var cmdRestore = new MySqlCommand("UPDATE pista SET ezabatua = FALSE, ezabaketa_data = NULL WHERE id = @id;", kon, tx);
            cmdRestore.Parameters.AddWithValue("@id", id);
            await cmdRestore.ExecuteNonQueryAsync();

            await tx.CommitAsync();
            return Success(new { id, mezua = $"[{id}] {pistaIzena} pista arrakastaz berreskuratu da!" }, 200, "Pista berreskuratu da.");
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return ServerError("Errorea pista berreskuratzean: " + ex.Message);
        }
    }
}

