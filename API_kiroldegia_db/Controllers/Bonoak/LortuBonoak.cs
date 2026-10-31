using API_kiroldegia_db.DTO;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

namespace API_kiroldegia_db.Controllers;

public partial class BonoakKontroladorea
{
    /// <summary>
    /// Bono guztiak zerrendatu.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> LortuBonoak()
    {
        await using var kon = await SortuKonexioaAsync();
        using var cmd = new MySqlCommand("SELECT emaila, saioak FROM bonoak ORDER BY emaila;", kon);
        var zerrenda = new List<BonoDTO>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            zerrenda.Add(new BonoDTO
            {
                Emaila = reader.GetString("emaila"),
                Saioak = reader.GetInt32("saioak")
            });
        }
        return SuccessList(zerrenda, 200, "Bono guztiak ondo lortu dira.");
    }
}

