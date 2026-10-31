using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

namespace API_kiroldegia_db.Controllers;

public partial class BonoakKontroladorea
{
    /// <summary>
    /// Soft-Delete bidez ezabatutako bonoa berreskuratu (RESTORE).
    /// Bidea: PUT /api/bonoak/{emaila}/restore
    /// </summary>
    [HttpPut("{emaila}/restore")]
    public async Task<IActionResult> BerreskuratuBonoa(string emaila)
    {
        await using var kon = await SortuKonexioaAsync();
        await using var tx = await kon.BeginTransactionAsync();

        try
        {
            var cmdGet = new MySqlCommand("SELECT emaila, saioak, ezabatua FROM bonoak WHERE emaila = @email FOR UPDATE;", kon, tx);
            cmdGet.Parameters.AddWithValue("@email", emaila);

            bool ezabatua = false;
            using (var reader = await cmdGet.ExecuteReaderAsync())
            {
                if (!await reader.ReadAsync())
                {
                    await tx.RollbackAsync();
                    return NotFoundError($"Ez da aurkitu '{emaila}' emaila duen bonorik.");
                }
                ezabatua = reader.GetBoolean("ezabatua");
            }

            if (!ezabatua)
            {
                await tx.RollbackAsync();
                return BadRequestError($"'{emaila}' bonoa ez dago Soft-Delete eginda (aktibo dago dagoeneko).");
            }

            var cmdRestore = new MySqlCommand("UPDATE bonoak SET ezabatua = FALSE, ezabaketa_data = NULL WHERE emaila = @email;", kon, tx);
            cmdRestore.Parameters.AddWithValue("@email", emaila);
            await cmdRestore.ExecuteNonQueryAsync();

            await tx.CommitAsync();
            return Success(new { mezua = $"'{emaila}' bonoa arrakastaz berreskuratu da." }, 200, "Bonoa berreskuratu da.");
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return StatusCode(500, new { errorea = true, mezua = $"Errorea bonoa berreskuratzean: {ex.Message}" });
        }
    }
}
