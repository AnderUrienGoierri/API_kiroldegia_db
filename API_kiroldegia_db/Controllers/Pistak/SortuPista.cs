using API_kiroldegia_db.DTO;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

namespace API_kiroldegia_db.Controllers;

public partial class PistakKontroladorea
{
    /// <summary>
    /// Pista berria sortu.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> SortuPista([FromBody] PistaSortuDTO dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Izena) || string.IsNullOrWhiteSpace(dto.Kirola))
            return BadRequestError("Izena eta kirola beharrezkoak dira.");

        await using var kon = await SortuKonexioaAsync();
        var cmd = new MySqlCommand("INSERT INTO pista (izena, kirola, ezabatua) VALUES (@izena, @kirola, FALSE);", kon);
        cmd.Parameters.AddWithValue("@izena", dto.Izena);
        cmd.Parameters.AddWithValue("@kirola", dto.Kirola);
        await cmd.ExecuteNonQueryAsync();

        return Created(new
        {
            id = cmd.LastInsertedId,
            mezua = "Pista ondo sortu da!",
            datuak = dto
        }, 201, "Pista arrakastaz sortu da!");
    }
}

