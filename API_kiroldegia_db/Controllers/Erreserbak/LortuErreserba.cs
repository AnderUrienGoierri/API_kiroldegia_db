using API_kiroldegia_db.DTO;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

namespace API_kiroldegia_db.Controllers;

public partial class ErreserbakKontroladorea
{
    /// <summary>
    /// Erreserba bakarra ID bidez lortu.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> LortuErreserba(int id)
    {
        await using var kon = await SortuKonexioaAsync();
        string sql = @"
            SELECT e.id, e.pista_id, p.izena AS pista_izena, p.kirola,
                   e.bazkidea_id, CONCAT(b.izena, ' ', b.abizena) AS bazkidea_izena, b.emaila AS bazkidea_emaila,
                   e.data, e.ordua, e.ezabatua, e.ezabaketa_data, e.sortua
            FROM erreserba e
            JOIN pista p ON e.pista_id = p.id
            JOIN bazkidea b ON e.bazkidea_id = b.id
            WHERE e.id = @id;";

        using var cmd = new MySqlCommand(sql, kon);
        cmd.Parameters.AddWithValue("@id", id);
        using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            return NotFoundError($"Ez da aurkitu {id} ID-a duen erreserbarik.");

        var erreserba = new ErreserbaDTO
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
        };

        return Success(erreserba, 200, "Erreserba ondo lortu da.");
    }
}

