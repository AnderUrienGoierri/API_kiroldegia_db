using System.Data;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

namespace API_kiroldegia_db.Controllers;

public partial class BonoakKontroladorea
{
    /// <summary>
    /// Bono baten saio 1 kontsumitu transakzioan (FOR UPDATE + saldo negatiboa ekiditeko).
    /// </summary>
    [HttpPost("{emaila}/erabili")]
    public async Task<IActionResult> ErabiliSaioa(string emaila)
    {
        await using var kon = await SortuKonexioaAsync();
        await using var tx = await kon.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            // 1. FOR UPDATE: blokeatu eta egiaztatu
            var cmdCheck = new MySqlCommand("SELECT saioak FROM bonoak WHERE emaila = @email FOR UPDATE;", kon, tx);
            cmdCheck.Parameters.AddWithValue("@email", emaila);

            var result = await cmdCheck.ExecuteScalarAsync();
            if (result == null)
            {
                await tx.RollbackAsync();
                return NotFoundError($"Ez da aurkitu '{emaila}' duen bonorik.");
            }

            int saioak = Convert.ToInt32(result);
            if (saioak <= 0)
            {
                await tx.RollbackAsync();
                return ConflictError($"Ez daukazu saio nahikorik bonoan! Egungo saioak: {saioak}");
            }

            // 2. Deskontatu saio 1
            int geratzenDira = saioak - 1;
            var cmdUpdate = new MySqlCommand("UPDATE bonoak SET saioak = @saioak WHERE emaila = @email;", kon, tx);
            cmdUpdate.Parameters.AddWithValue("@saioak", geratzenDira);
            cmdUpdate.Parameters.AddWithValue("@email", emaila);
            await cmdUpdate.ExecuteNonQueryAsync();

            await tx.CommitAsync();

            return Success(new
            {
                emaila,
                saioak = geratzenDira,
                mezua = $"Saio 1 erabili da. {geratzenDira} saio geratzen zaizkizu."
            }, 200, "Saio 1 erabili da.");
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return ServerError("Errorea bonoa erabiltzean: " + ex.Message);
        }
    }
}

