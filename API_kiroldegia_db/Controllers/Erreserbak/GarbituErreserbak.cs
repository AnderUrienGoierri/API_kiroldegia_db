using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

namespace API_kiroldegia_db.Controllers;

public partial class ErreserbakKontroladorea
{
    /// <summary>
    /// Erreserba taula guztiz garbitu probak hutsetik hasteko.
    /// </summary>
    [HttpDelete("garbitu")]
    public async Task<IActionResult> GarbituErreserbak()
    {
        await using var kon = await SortuKonexioaAsync();
        var cmd = new MySqlCommand("DELETE FROM erreserba;", kon);
        int kopurua = await cmd.ExecuteNonQueryAsync();
        return Success(new { mezua = $"Erreserba guztiak ezabatu dira ({kopurua} errenkada)." }, 200, "Erreserbak garbitu dira.");
    }
}

