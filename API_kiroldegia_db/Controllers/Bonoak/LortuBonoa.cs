using API_kiroldegia_db.DTO;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

namespace API_kiroldegia_db.Controllers;

public partial class BonoakKontroladorea
{
    /// <summary>
    /// Email baten bonoa lortu.
    /// </summary>
    [HttpGet("{emaila}")]
    public async Task<IActionResult> LortuBonoa(string emaila)
    {
        await using var kon = await SortuKonexioaAsync();
        using var cmd = new MySqlCommand("SELECT emaila, saioak FROM bonoak WHERE emaila = @email;", kon);
        cmd.Parameters.AddWithValue("@email", emaila);

        using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            return NotFoundError($"Ez da aurkitu '{emaila}' emaila duen bonorik.");

        var bono = new BonoDTO
        {
            Emaila = reader.GetString("emaila"),
            Saioak = reader.GetInt32("saioak")
        };
        return Success(bono, 200, "Bonoa ondo lortu da.");
    }
}

