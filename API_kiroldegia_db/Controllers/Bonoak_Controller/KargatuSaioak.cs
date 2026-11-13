using System.Data;
using API_kiroldegia_db.DTO;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

namespace API_kiroldegia_db.Controllers;

public partial class BonoakKontroladorea
{
    /// <summary>
    /// Bonoari saioak kargatu transakzio seguru batean (FOR UPDATE bidez).
    /// </summary>
    [HttpPut("{emaila}/kargatu")]
    public async Task<IActionResult> KargatuSaioak(string emaila, [FromBody] BonoKargatuDTO dto)
    {
        if (dto.Saioak <= 0)
            return BadRequestError("Saio kopuruak positiboa izan behar du (>0).");

        await using var kon = await SortuKonexioaAsync();
        await using var tx = await kon.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            // 1. Erregistroa blokeatu transakzioan FOR UPDATE bidez
            var cmdCheck = new MySqlCommand("SELECT saioak FROM bonoak WHERE emaila = @email FOR UPDATE;", kon, tx);
            cmdCheck.Parameters.AddWithValue("@email", emaila);

            var result = await cmdCheck.ExecuteScalarAsync();
            if (result == null)
            {
                await tx.RollbackAsync();
                return NotFoundError($"Ez da aurkitu '{emaila}' emaila duen bonorik.");
            }

            int egungoSaioak = Convert.ToInt32(result);
            int saioBerriak = egungoSaioak + dto.Saioak;

            // 2. Eguneratu
            var cmdUpdate = new MySqlCommand("UPDATE bonoak SET saioak = @saioak WHERE emaila = @email;", kon, tx);
            cmdUpdate.Parameters.AddWithValue("@saioak", saioBerriak);
            cmdUpdate.Parameters.AddWithValue("@email", emaila);
            await cmdUpdate.ExecuteNonQueryAsync();

            await tx.CommitAsync();

            return Success(new
            {
                emaila,
                aurrekoSaioak = egungoSaioak,
                gehitutakoSaioak = dto.Saioak,
                saioak = saioBerriak,
                mezua = $"Bonoa arrakastaz kargatu da! Saio berriak: {saioBerriak}"
            }, 200, "Bonoa arrakastaz kargatu da.");
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return ServerError("Errorea bonoa kargatzean: " + ex.Message);
        }
    }
}

