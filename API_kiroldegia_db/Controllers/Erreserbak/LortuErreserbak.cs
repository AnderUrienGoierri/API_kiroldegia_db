using API_kiroldegia_db.DTO;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

namespace API_kiroldegia_db.Controllers;

public partial class ErreserbakKontroladorea
{
    /// <summary>
    /// Erreserba guztiak lortu.
    /// barneEzabatuak: true bada, Soft-Delete bidez ezabatutakoak ere erakutsiko ditu.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> LortuErreserbak([FromQuery] bool barneEzabatuak = true)
    {
        await using var kon = await SortuKonexioaAsync();
        string sql = @"
            SELECT e.id, e.pista_id, p.izena AS pista_izena, p.kirola,
                   e.bazkidea_id, CONCAT(b.izena, ' ', b.abizena) AS bazkidea_izena, b.emaila AS bazkidea_emaila,
                   e.data, e.ordua, e.ezabatua, e.ezabaketa_data, e.sortua
            FROM erreserba e
            JOIN pista p ON e.pista_id = p.id
            JOIN bazkidea b ON e.bazkidea_id = b.id
            WHERE (@barne = TRUE OR e.ezabatua = FALSE)
            ORDER BY e.data DESC, e.ordua DESC, e.id DESC;";

        using var cmd = new MySqlCommand(sql, kon);
        cmd.Parameters.AddWithValue("@barne", barneEzabatuak);

        var emaitzak = new List<ErreserbaDTO>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            emaitzak.Add(new ErreserbaDTO
            {
                Id = reader.GetInt32("id"),
                PistaId = reader.GetInt32("pista_id"),
                PistaIzena = reader.GetString("pista_izena"),
                Kirola = reader.GetString("kirola"),
                BazkideaId = reader.GetInt32("bazkidea_id"),
                BazkideaIzena = reader.GetString("bazkidea_izena"),
                BazkideaEmaila = reader.IsDBNull(reader.GetOrdinal("bazkidea_emaila")) ? null : reader.GetString("bazkidea_emaila"),
                Data = Convert.ToDateTime(reader["data"]).ToString("yyyy-MM-dd"),
                Ordua = reader["ordua"].ToString() ?? "",
                Ezabatua = reader.GetBoolean("ezabatua"),
                EzabaketaData = reader.IsDBNull(reader.GetOrdinal("ezabaketa_data"))
                    ? null
                    : reader.GetDateTime("ezabaketa_data"),
                Sortua = reader.GetDateTime("sortua")
            });
        }

        return SuccessList(emaitzak, 200, "Erreserbak ondo lortu dira.");
    }
}

