using API_kiroldegia_db.DTO;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

namespace API_kiroldegia_db.Controllers;

public partial class PistakKontroladorea
{
    /// <summary>
    /// Pistak zerrendatu.
    /// barneEzabatuak: true bada, Soft-Delete bidez ezabatutakoak ere erakutsiko ditu.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> LortuPistak([FromQuery] bool barneEzabatuak = true)
    {
        await using var kon = await SortuKonexioaAsync();
        string sql = @"
            SELECT id, izena, kirola, ezabatua, ezabaketa_data 
            FROM pista 
            WHERE (@barne = TRUE OR ezabatua = FALSE)
            ORDER BY id;";

        using var cmd = new MySqlCommand(sql, kon);
        cmd.Parameters.AddWithValue("@barne", barneEzabatuak);

        var pistak = new List<PistaDTO>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            pistak.Add(new PistaDTO
            {
                Id = reader.GetInt32("id"),
                Izena = reader.GetString("izena"),
                Kirola = reader.GetString("kirola"),
                Ezabatua = reader.GetBoolean("ezabatua"),
                EzabaketaData = reader.IsDBNull(reader.GetOrdinal("ezabaketa_data"))
                    ? null
                    : reader.GetDateTime("ezabaketa_data")
            });
        }
        return SuccessList(pistak, 200, "Pistak ondo lortu dira.");
    }
}

