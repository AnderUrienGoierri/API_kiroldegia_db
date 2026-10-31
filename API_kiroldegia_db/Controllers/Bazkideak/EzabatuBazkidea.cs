using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

namespace API_kiroldegia_db.Controllers;

public partial class BazkideakKontroladorea
{
    /// <summary>
    /// Bazkidea ezabatu (Soft Delete edo Hard Delete).
    /// Bidea: DELETE /api/bazkideak/{id}?hard=false
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> EzabatuBazkidea(int id, [FromQuery] bool hard = false)
    {
        await using var kon = await SortuKonexioaAsync();
        await using var tx = await kon.BeginTransactionAsync();

        try
        {
            // Bazkidea existitzen den egiaztatu
            var cmdGet = new MySqlCommand("SELECT id, izena, abizena, emaila, ezabatua FROM bazkidea WHERE id = @id FOR UPDATE;", kon, tx);
            cmdGet.Parameters.AddWithValue("@id", id);
            string izena = "";
            bool jadaEzabatua = false;
            using (var reader = await cmdGet.ExecuteReaderAsync())
            {
                if (!await reader.ReadAsync())
                {
                    await tx.RollbackAsync();
                    return NotFoundError($"Ez da aurkitu {id} ID-a duen bazkiderik.");
                }
                izena = $"{reader.GetString("izena")} {reader.GetString("abizena")}";
                jadaEzabatua = reader.GetBoolean("ezabatua");
            }

            if (!hard)
            {
                // SOFT DELETE:
                if (jadaEzabatua)
                {
                    await tx.RollbackAsync();
                    return BadRequestError($"[{id}] {izena} bazkidea lehendik ere Soft-Delete eginda dago.");
                }

                var cmdSoft = new MySqlCommand("UPDATE bazkidea SET ezabatua = TRUE, ezabaketa_data = NOW() WHERE id = @id;", kon, tx);
                cmdSoft.Parameters.AddWithValue("@id", id);
                await cmdSoft.ExecuteNonQueryAsync();

                await tx.CommitAsync();
                return Success(new { mezua = $"[{id}] {izena} bazkidea logikoki ezabatu da (Soft Delete)." }, 200, "Bazkidea logikoki ezabatu da.");
            }
            else
            {
                // HARD DELETE:
                // Erreserbarik baduen egiaztatu
                var cmdRes = new MySqlCommand("SELECT COUNT(*) FROM erreserba WHERE bazkidea_id = @id;", kon, tx);
                cmdRes.Parameters.AddWithValue("@id", id);
                long resCount = Convert.ToInt64(await cmdRes.ExecuteScalarAsync());
                if (resCount > 0)
                {
                    await tx.RollbackAsync();
                    return ConflictError($"Ezin da [{id}] bazkidea behin betiko ezabatu: {resCount} erreserba ditu lotuta!");
                }

                // Bazkidea fisikoki ezabatu
                var cmdDel = new MySqlCommand("DELETE FROM bazkidea WHERE id = @id;", kon, tx);
                cmdDel.Parameters.AddWithValue("@id", id);
                await cmdDel.ExecuteNonQueryAsync();

                await tx.CommitAsync();
                return Success(new { mezua = $"[{id}] {izena} bazkidea behin betiko ezabatu da (Hard Delete)." }, 200, "Bazkidea fisikoki ezabatu da.");
            }
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return ServerError("Errorea bazkidea ezabatzean: " + ex.Message);
        }
    }
}

