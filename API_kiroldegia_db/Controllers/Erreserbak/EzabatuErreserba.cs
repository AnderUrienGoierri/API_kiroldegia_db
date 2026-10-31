using System.Data;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

namespace API_kiroldegia_db.Controllers;

public partial class ErreserbakKontroladorea
{
    /// <summary>
    /// Erreserba bat ezabatu.
    /// hard = false: Soft Delete (ezabatua=TRUE).
    /// hard = true: Hard Delete (fisikoki DELETE FROM).
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> EzabatuErreserba(int id, [FromQuery] bool hard = false)
    {
        await using var kon = await SortuKonexioaAsync();
        await using var tx = await kon.BeginTransactionAsync(IsolationLevel.ReadCommitted);

        try
        {
            // 1. FOR UPDATE: Erregistroa blokeatu transakzioan
            var cmdCheck = new MySqlCommand("SELECT id, ezabatua FROM erreserba WHERE id = @id FOR UPDATE;", kon, tx);
            cmdCheck.Parameters.AddWithValue("@id", id);
            using (var reader = await cmdCheck.ExecuteReaderAsync())
            {
                if (!await reader.ReadAsync())
                {
                    await tx.RollbackAsync();
                    return NotFoundError($"Ez da aurkitu {id} ID-a duen erreserbarik.");
                }

                bool jadaEzabatua = reader.GetBoolean("ezabatua");
                if (!hard && jadaEzabatua)
                {
                    await tx.RollbackAsync();
                    return BadRequestError($"[{id}] erreserba lehendik ere Soft-Delete eginda dago.");
                }
            }

            if (hard)
            {
                var cmdHard = new MySqlCommand("DELETE FROM erreserba WHERE id = @id;", kon, tx);
                cmdHard.Parameters.AddWithValue("@id", id);
                await cmdHard.ExecuteNonQueryAsync();

                await tx.CommitAsync();
                return Success(new
                {
                    id,
                    mota = "HARD_DELETE",
                    mezua = $"[{id}] erreserba behin betiko ezabatu da (Hard Delete) datu-basetik."
                }, 200, "Erreserba fisikoki ezabatu da.");
            }
            else
            {
                var cmdSoft = new MySqlCommand(@"
                    UPDATE erreserba 
                    SET ezabatua = TRUE, ezabaketa_data = NOW() 
                    WHERE id = @id;", kon, tx);
                cmdSoft.Parameters.AddWithValue("@id", id);
                await cmdSoft.ExecuteNonQueryAsync();

                await tx.CommitAsync();
                return Success(new
                {
                    id,
                    mota = "SOFT_DELETE",
                    mezua = $"[{id}] erreserba logikoki ezabatu da (Soft Delete). Pista eta ordu hori libre geratu da besteentzat."
                }, 200, "Erreserba logikoki ezabatu da.");
            }
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return ServerError("Errorea ezabatzean: " + ex.Message);
        }
    }
}

