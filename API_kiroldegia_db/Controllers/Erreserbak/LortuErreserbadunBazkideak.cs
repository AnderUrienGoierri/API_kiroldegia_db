using API_kiroldegia_db.DTO;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

namespace API_kiroldegia_db.Controllers;

public partial class ErreserbakKontroladorea
{
    /// <summary>
    /// Soilik gutxienez erreserba bat duten bazkideak lortu.
    /// Bidea: GET /api/erreserbak/bazkideak
    /// barneEzabatuak: true bada, ezabatutako erreserbak dituztenak ere barne hartzen ditu.
    /// </summary>
    [HttpGet("bazkideak")]
    public async Task<IActionResult> LortuErreserbadunBazkideak([FromQuery] bool barneEzabatuak = true)
    {
        await using var kon = await SortuKonexioaAsync();
        string sql = @"
            SELECT DISTINCT b.id, b.izena, b.abizena, b.emaila 
            FROM bazkidea b 
            JOIN erreserba e ON b.id = e.bazkidea_id 
            WHERE (@barne = TRUE OR e.ezabatua = FALSE)
            ORDER BY b.id;";

        using var cmd = new MySqlCommand(sql, kon);
        cmd.Parameters.AddWithValue("@barne", barneEzabatuak);

        var bazkideak = new List<BazkideaDTO>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            bazkideak.Add(new BazkideaDTO
            {
                Id = reader.GetInt32("id"),
                Izena = reader.GetString("izena"),
                Abizena = reader.GetString("abizena"),
                Emaila = reader.IsDBNull(reader.GetOrdinal("emaila")) ? null : reader.GetString("emaila")
            });
        }
        return SuccessList(bazkideak, 200, "Erreserbadun bazkideak ondo lortu dira.");
    }
}

