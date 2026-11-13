using API_kiroldegia_db.DTO;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

namespace API_kiroldegia_db.Controllers;

public partial class BonoakKontroladorea
{
    /// <summary>
    /// Bono guztiak zerrendatu (aukeran ezabatuak barne edo kanpo).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> LortuBonoak([FromQuery] bool barneEzabatuak = true)
    {
        await using var kon = await SortuKonexioaAsync();
        string sql = barneEzabatuak
            ? "SELECT emaila, saioak, ezabatua, ezabaketa_data FROM bonoak ORDER BY emaila;"
            : "SELECT emaila, saioak, ezabatua, ezabaketa_data FROM bonoak WHERE ezabatua = FALSE ORDER BY emaila;";

        using var cmd = new MySqlCommand(sql, kon);
        var zerrenda = new List<BonoDTO>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            zerrenda.Add(new BonoDTO
            {
                Emaila = reader.GetString("emaila"),
                Saioak = reader.GetInt32("saioak"),
                Ezabatua = reader.GetBoolean("ezabatua"),
                EzabaketaData = reader.IsDBNull(reader.GetOrdinal("ezabaketa_data"))
                    ? null
                    : reader.GetDateTime("ezabaketa_data")
            });
        }
        return SuccessList(zerrenda, 200, "Bono guztiak ondo lortu dira.");
    }
}
