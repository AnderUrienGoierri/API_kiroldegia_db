using System.Data;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

namespace API_kiroldegia_db.Controllers;

public partial class PistakKontroladorea
{
    /// <summary>
    /// Pista bat ezabatu (Soft Delete edo Hard Delete).
    /// Soft delete egitean erlazionatutako erreserbak desgaitu egin behar dira.
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> EzabatuPista(int id, [FromQuery] bool hard = false)
    {
        await using var kon = await SortuKonexioaAsync();
        await using var tx = await kon.BeginTransactionAsync(IsolationLevel.ReadCommitted);

        try
        {
            // 1. FOR UPDATE: Pista egiaztatu eta blokeatu
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
                bool jadaEzabatua = reader.GetBoolean("ezabatua");
                if (!hard && jadaEzabatua)
                {
                    await tx.RollbackAsync();
                    return BadRequestError($"[{id}] {pistaIzena} pista lehendik ere Soft-Delete eginda dago.");
                }
            }

            if (hard)
            {
                // HARD DELETE:
                // Lehenengo pistaren erreserbak fisikoki ezabatu
                var cmdErreserbakDel = new MySqlCommand("DELETE FROM erreserba WHERE pista_id = @id;", kon, tx);
                cmdErreserbakDel.Parameters.AddWithValue("@id", id);
                int erreserbaKop = await cmdErreserbakDel.ExecuteNonQueryAsync();

                // Ondoren pista bera ezabatu
                var cmdPistaDel = new MySqlCommand("DELETE FROM pista WHERE id = @id;", kon, tx);
                cmdPistaDel.Parameters.AddWithValue("@id", id);
                await cmdPistaDel.ExecuteNonQueryAsync();

                await tx.CommitAsync();

                return Success(new
                {
                    id,
                    mota = "HARD_DELETE",
                    mezua = $"[{id}] {pistaIzena} pista eta bere {erreserbaKop} erreserba behin betiko ezabatu dira datu-basetik (Hard Delete)."
                }, 200, "Pista fisikoki ezabatu da.");
            }
            else
            {
                // SOFT DELETE:
                // 1) Pista ezabatuta markatu
                var cmdSoftPista = new MySqlCommand(@"
                    UPDATE pista 
                    SET ezabatua = TRUE, ezabaketa_data = NOW() 
                    WHERE id = @id;", kon, tx);
                cmdSoftPista.Parameters.AddWithValue("@id", id);
                await cmdSoftPista.ExecuteNonQueryAsync();

                // 2) Erlazionatutako erreserbak desgaituta (ezabatua=TRUE) bezala geratu behar dira
                var cmdSoftErreserbak = new MySqlCommand(@"
                    UPDATE erreserba 
                    SET ezabatua = TRUE, ezabaketa_data = NOW() 
                    WHERE pista_id = @id AND ezabatua = FALSE;", kon, tx);
                cmdSoftErreserbak.Parameters.AddWithValue("@id", id);
                int desgaituKop = await cmdSoftErreserbak.ExecuteNonQueryAsync();

                await tx.CommitAsync();

                return Success(new
                {
                    id,
                    mota = "SOFT_DELETE",
                    desgaitutakoErreserbak = desgaituKop,
                    mezua = $"[{id}] {pistaIzena} pista logikoki ezabatu da (Soft Delete), eta erlazionatutako {desgaituKop} erreserba desgaitu dira."
                }, 200, "Pista logikoki ezabatu da.");
            }
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return ServerError("Errorea pista ezabatzean: " + ex.Message);
        }
    }
}

