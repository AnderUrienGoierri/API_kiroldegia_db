using System.Data;
using API_kiroldegia_db.DTO;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

namespace API_kiroldegia_db.Controllers;

public partial class BonoakKontroladorea
{
    /// <summary>
    /// Bono berria sortu (edo erosi).
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> SortuBonoa([FromBody] BonoDTO dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Emaila) || dto.Saioak < 0)
            return BadRequestError("Emaila eta saio kopuru egokia (>=0) beharrezkoak dira.");

        await using var kon = await SortuKonexioaAsync();
        await using var tx = await kon.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            var cmd = new MySqlCommand(@"
                INSERT INTO bonoak (emaila, saioak) 
                VALUES (@email, @saioak)
                ON DUPLICATE KEY UPDATE saioak = saioak + @saioak;", kon, tx);

            cmd.Parameters.AddWithValue("@email", dto.Emaila.Trim().ToLower());
            cmd.Parameters.AddWithValue("@saioak", dto.Saioak);

            await cmd.ExecuteNonQueryAsync();
            await tx.CommitAsync();

            return Created(new { mezua = $"Bonoa ondo eguneratu/sortu da ({dto.Emaila}: {dto.Saioak} saio)." }, 201, "Bonoa ondo sortu/eguneratu da.");
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return ServerError("Errorea bonoa sortzean: " + ex.Message);
        }
    }
}

