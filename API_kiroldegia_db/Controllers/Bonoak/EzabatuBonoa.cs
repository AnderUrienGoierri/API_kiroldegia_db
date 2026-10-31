using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

namespace API_kiroldegia_db.Controllers;

public partial class BonoakKontroladorea
{
    /// <summary>
    /// Bonoa ezabatu (Soft Delete edo Hard Delete).
    /// Bidea: DELETE /api/bonoak/{emaila}?hard=false
    /// </summary>
    [HttpDelete("{emaila}")]
    public async Task<IActionResult> EzabatuBonoa(string emaila, [FromQuery] bool hard = false)
    {
        await using var kon = await SortuKonexioaAsync();
        await using var tx = await kon.BeginTransactionAsync();

        try
        {
            // Bonoa existitzen den egiaztatu
            var cmdGet = new MySqlCommand("SELECT emaila, saioak, ezabatua FROM bonoak WHERE emaila = @email FOR UPDATE;", kon, tx);
            cmdGet.Parameters.AddWithValue("@email", emaila);

            bool jadaEzabatua = false;
            int saldoa = 0;
            using (var reader = await cmdGet.ExecuteReaderAsync())
            {
                if (!await reader.ReadAsync())
                {
                    await tx.RollbackAsync();
                    return NotFoundError($"Ez da aurkitu '{emaila}' emaila duen bonorik.");
                }
                saldoa = reader.GetInt32("saioak");
                jadaEzabatua = reader.GetBoolean("ezabatua");
            }

            if (!hard)
            {
                // SOFT DELETE:
                if (jadaEzabatua)
                {
                    await tx.RollbackAsync();
                    return BadRequestError($"'{emaila}' bonoa lehendik ere Soft-Delete eginda dago.");
                }

                var cmdSoft = new MySqlCommand("UPDATE bonoak SET ezabatua = TRUE, ezabaketa_data = NOW() WHERE emaila = @email;", kon, tx);
                cmdSoft.Parameters.AddWithValue("@email", emaila);
                await cmdSoft.ExecuteNonQueryAsync();

                await tx.CommitAsync();
                return Success(new { mezua = $"'{emaila}' bonoa logikoki ezabatu da (Soft Delete)." }, 200, "Bonoa logikoki ezabatu da.");
            }
            else
            {
                // HARD DELETE:
                var cmdHard = new MySqlCommand("DELETE FROM bonoak WHERE emaila = @email;", kon, tx);
                cmdHard.Parameters.AddWithValue("@email", emaila);
                await cmdHard.ExecuteNonQueryAsync();

                await tx.CommitAsync();
                return Success(new { mezua = $"'{emaila}' bonoa behin betiko ezabatu da datu-basetik (Hard Delete)." }, 200, "Bonoa behin betiko ezabatu da.");
            }
        }
        catch (MySqlException ex)
        {
            await tx.RollbackAsync();
            if (ex.Number == 1451)
            {
                return ConflictError($"Ezin da '{emaila}' bonoa fisikoki ezabatu, datu-baseko beste erregistro bati lotuta dagoelako (ON DELETE RESTRICT). Erabili Soft Delete.");
            }
            return StatusCode(500, new { errorea = true, mezua = $"MySQL errorea: {ex.Message}" });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return StatusCode(500, new { errorea = true, mezua = $"Errore ezezaguna: {ex.Message}" });
        }
    }
}
