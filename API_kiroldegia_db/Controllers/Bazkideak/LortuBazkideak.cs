using API_kiroldegia_db.DTO;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

namespace API_kiroldegia_db.Controllers;

public partial class BazkideakKontroladorea
{
    /// <summary>
    /// Sistemako bazkideak zerrendatu (bono saldoarekin batera).
    /// Bidea: GET /api/bazkideak
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> LortuBazkideak([FromQuery] bool barneEzabatuak = true)
    {
        await using var kon = await SortuKonexioaAsync();
        string sql = @"
            SELECT b.id, b.izena, b.abizena, b.emaila, b.ezabatua, b.ezabaketa_data, COALESCE(bo.saioak, 0) AS bono_saioak
            FROM bazkidea b
            LEFT JOIN bonoak bo ON b.emaila = bo.emaila";

        if (!barneEzabatuak)
        {
            sql += " WHERE b.ezabatua = FALSE";
        }
        sql += " ORDER BY b.id;";

        using var cmd = new MySqlCommand(sql, kon);

        var bazkideak = new List<BazkideaDTO>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            bazkideak.Add(new BazkideaDTO
            {
                Id = reader.GetInt32("id"),
                Izena = reader.GetString("izena"),
                Abizena = reader.GetString("abizena"),
                Emaila = reader.IsDBNull(reader.GetOrdinal("emaila")) ? null : reader.GetString("emaila"),
                BonoSaioak = reader.GetInt32("bono_saioak"),
                Ezabatua = reader.GetBoolean("ezabatua"),
                EzabaketaData = reader.IsDBNull(reader.GetOrdinal("ezabaketa_data")) ? null : reader.GetDateTime("ezabaketa_data")
            });
        }
        return SuccessList(bazkideak, 200, "Bazkideak ondo lortu dira.");
    }
}

