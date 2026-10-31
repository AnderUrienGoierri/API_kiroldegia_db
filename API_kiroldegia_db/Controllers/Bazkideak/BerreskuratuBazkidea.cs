using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

namespace API_kiroldegia_db.Controllers;

public partial class BazkideakKontroladorea
{
    /// <summary>
    /// Soft-deleted dagoen bazkide bat berreskuratu.
    /// Bidea: PUT /api/bazkideak/{id}/restore
    /// </summary>
    [HttpPut("{id}/restore")]
    public async Task<IActionResult> BerreskuratuBazkidea(int id)
    {
        await using var kon = await SortuKonexioaAsync();
        await using var tx = await kon.BeginTransactionAsync();

        try
        {
            var cmdCheck = new MySqlCommand("SELECT id, izena, abizena, ezabatua FROM bazkidea WHERE id = @id FOR UPDATE;", kon, tx);
            cmdCheck.Parameters.AddWithValue("@id", id);
            string izena = "";
            bool ezabatua = false;
            using (var reader = await cmdCheck.ExecuteReaderAsync())
            {
                if (!await reader.ReadAsync())
                {
                    await tx.RollbackAsync();
                    return NotFoundError($"Ez da aurkitu {id} ID-a duen bazkiderik.");
                }
                izena = $"{reader.GetString("izena")} {reader.GetString("abizena")}";
                ezabatua = reader.GetBoolean("ezabatua");
            }

            if (!ezabatua)
            {
                await tx.RollbackAsync();
                return BadRequestError($"[{id}] {izena} bazkidea jada aktibo dago, ez dago ezabatuta.");
            }

            var cmdRestore = new MySqlCommand("UPDATE bazkidea SET ezabatua = FALSE, ezabaketa_data = NULL WHERE id = @id;", kon, tx);
            cmdRestore.Parameters.AddWithValue("@id", id);
            await cmdRestore.ExecuteNonQueryAsync();

            await tx.CommitAsync();
            return Success(new { mezua = $"[{id}] {izena} bazkidea arrakastaz berreskuratu da!" }, 200, "Bazkidea berreskuratu da.");
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return ServerError("Errorea bazkidea berreskuratzean: " + ex.Message);
        }
    }
}

