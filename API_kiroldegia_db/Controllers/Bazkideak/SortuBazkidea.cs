using API_kiroldegia_db.DTO;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

namespace API_kiroldegia_db.Controllers;

public partial class BazkideakKontroladorea
{
    /// <summary>
    /// Bazkide berria sortu.
    /// Bidea: POST /api/bazkideak
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> SortuBazkidea([FromBody] BazkideaSortuDTO dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Izena) || string.IsNullOrWhiteSpace(dto.Abizena) || string.IsNullOrWhiteSpace(dto.Emaila))
        {
            return BadRequestError("Izena, abizena eta emaila beharrezkoak dira.");
        }

        await using var kon = await SortuKonexioaAsync();
        await using var tx = await kon.BeginTransactionAsync();

        try
        {
            // Emaila errepikatuta dagoen egiaztatu
            var cmdCheck = new MySqlCommand("SELECT COUNT(*) FROM bazkidea WHERE emaila = @email;", kon, tx);
            cmdCheck.Parameters.AddWithValue("@email", dto.Emaila.Trim().ToLower());
            long exist = Convert.ToInt64(await cmdCheck.ExecuteScalarAsync());
            if (exist > 0)
            {
                await tx.RollbackAsync();
                return ConflictError($"'{dto.Emaila}' emaila duen bazkide bat badago jada erregistratuta.");
            }

            var cmdInsert = new MySqlCommand(@"
                INSERT INTO bazkidea (izena, abizena, emaila)
                VALUES (@izena, @abizena, @email);", kon, tx);
            cmdInsert.Parameters.AddWithValue("@izena", dto.Izena.Trim());
            cmdInsert.Parameters.AddWithValue("@abizena", dto.Abizena.Trim());
            cmdInsert.Parameters.AddWithValue("@email", dto.Emaila.Trim().ToLower());
            await cmdInsert.ExecuteNonQueryAsync();
            long newId = cmdInsert.LastInsertedId;

            // Hasierako bonoa gehitu nahi bada
            if (dto.HasierakoBonoa > 0)
            {
                var cmdBono = new MySqlCommand(@"
                    INSERT INTO bonoak (emaila, saioak)
                    VALUES (@email, @saioak)
                    ON DUPLICATE KEY UPDATE saioak = saioak + @saioak;", kon, tx);
                cmdBono.Parameters.AddWithValue("@email", dto.Emaila.Trim().ToLower());
                cmdBono.Parameters.AddWithValue("@saioak", dto.HasierakoBonoa);
                await cmdBono.ExecuteNonQueryAsync();
            }

            await tx.CommitAsync();

            return Created(new
            {
                id = newId,
                izena = dto.Izena.Trim(),
                abizena = dto.Abizena.Trim(),
                emaila = dto.Emaila.Trim().ToLower(),
                bonoSaioak = dto.HasierakoBonoa,
                mezua = $"Bazkidea arrakastaz sortu da (ID: {newId})!"
            }, 201, "Bazkidea arrakastaz sortu da!");
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return ServerError("Errorea bazkidea sortzean: " + ex.Message);
        }
    }
}

