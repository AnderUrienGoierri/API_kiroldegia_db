using API_kiroldegia_db.DTO;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

namespace API_kiroldegia_db.Controllers;

public partial class BazkideakKontroladorea
{
    /// <summary>
    /// Bazkide zehatz bat ID bidez lortu.
    /// Bidea: GET /api/bazkideak/{id}
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> LortuBazkidea(int id)
    {
        await using var kon = await SortuKonexioaAsync();
        using var cmd = new MySqlCommand(@"
            SELECT b.id, b.izena, b.abizena, b.emaila, COALESCE(bo.saioak, 0) AS bono_saioak
            FROM bazkidea b
            LEFT JOIN bonoak bo ON b.emaila = bo.emaila
            WHERE b.id = @id;", kon);
        cmd.Parameters.AddWithValue("@id", id);

        using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            return NotFoundError($"Ez da aurkitu {id} ID-a duen bazkiderik.");
        }

        var b = new BazkideaDTO
        {
            Id = reader.GetInt32("id"),
            Izena = reader.GetString("izena"),
            Abizena = reader.GetString("abizena"),
            Emaila = reader.IsDBNull(reader.GetOrdinal("emaila")) ? null : reader.GetString("emaila"),
            BonoSaioak = reader.GetInt32("bono_saioak")
        };

        return Success(b, 200, "Bazkidea ondo lortu da.");
    }
}

