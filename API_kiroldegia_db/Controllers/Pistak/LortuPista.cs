using API_kiroldegia_db.DTO;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

namespace API_kiroldegia_db.Controllers;

public partial class PistakKontroladorea
{
    /// <summary>
    /// Pista bakarra ID bidez lortu.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> LortuPista(int id)
    {
        await using var kon = await SortuKonexioaAsync();
        using var cmd = new MySqlCommand("SELECT id, izena, kirola, ezabatua, ezabaketa_data FROM pista WHERE id = @id;", kon);
        cmd.Parameters.AddWithValue("@id", id);

        using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            return NotFoundError($"Ez da aurkitu {id} ID-a duen pistarik.");

        var p = new PistaDTO
        {
            Id = reader.GetInt32("id"),
            Izena = reader.GetString("izena"),
            Kirola = reader.GetString("kirola"),
            Ezabatua = reader.GetBoolean("ezabatua"),
            EzabaketaData = reader.IsDBNull(reader.GetOrdinal("ezabaketa_data"))
                ? null
                : reader.GetDateTime("ezabaketa_data")
        };
        return Success(p, 200, "Pista ondo lortu da.");
    }
}

