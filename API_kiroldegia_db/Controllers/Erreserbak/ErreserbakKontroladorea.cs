using API_kiroldegia_db.DTO;
using API_kiroldegia_db.Models;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

namespace API_kiroldegia_db.Controllers;

[ApiController]
[Route("api/erreserbak")]
public partial class ErreserbakKontroladorea : BaseApiKontroladorea
{
    private readonly ILogger<ErreserbakKontroladorea> _logger;

    public ErreserbakKontroladorea(IConfiguration config, ILogger<ErreserbakKontroladorea> logger)
        : base(config)
    {
        _logger = logger;
    }

    // =========================================================================
    // LAGUNTZAILE PRIBATUAK (DTO <-> Datu-basea eta modelo bihurketa)
    // =========================================================================

    /// <summary>
    /// ID edo izen/email hutsuneak betetzen ditu datu-basetik kontsultatuz.
    /// </summary>
    private static async Task BeteDatuakAsync(ErreserbaSortuDto dto, MySqlConnection conn, MySqlTransaction? trans = null)
    {
        // PistaId badago baina Lekua ez, pistaren izena berreskuratu
        if (string.IsNullOrWhiteSpace(dto.Lekua) && dto.PistaId.HasValue && dto.PistaId.Value > 0)
        {
            const string sqlP = "SELECT izena FROM pista WHERE id = @pid";
            using var cmdP = new MySqlCommand(sqlP, conn, trans);
            cmdP.Parameters.AddWithValue("@pid", dto.PistaId.Value);
            var izena = await cmdP.ExecuteScalarAsync();
            if (izena != null) dto.Lekua = izena.ToString() ?? "";
        }
        // Lekua badago baina PistaId ez, pistaren ID-a berreskuratu
        else if (!string.IsNullOrWhiteSpace(dto.Lekua) && (!dto.PistaId.HasValue || dto.PistaId.Value <= 0))
        {
            const string sqlP = "SELECT id FROM pista WHERE izena = @izena LIMIT 1";
            using var cmdP = new MySqlCommand(sqlP, conn, trans);
            cmdP.Parameters.AddWithValue("@izena", dto.Lekua.Trim());
            var pId = await cmdP.ExecuteScalarAsync();
            if (pId != null) dto.PistaId = Convert.ToInt32(pId);
        }

        // BazkideaId badago baina Emaila ez, bazkidearen emaila berreskuratu
        if (string.IsNullOrWhiteSpace(dto.Emaila) && dto.BazkideaId.HasValue && dto.BazkideaId.Value > 0)
        {
            const string sqlB = "SELECT emaila FROM bazkidea WHERE id = @bid";
            using var cmdB = new MySqlCommand(sqlB, conn, trans);
            cmdB.Parameters.AddWithValue("@bid", dto.BazkideaId.Value);
            var email = await cmdB.ExecuteScalarAsync();
            if (email != null) dto.Emaila = email.ToString() ?? "";
        }
        // Emaila badago baina BazkideaId ez, bazkidearen ID-a berreskuratu
        else if (!string.IsNullOrWhiteSpace(dto.Emaila) && (!dto.BazkideaId.HasValue || dto.BazkideaId.Value <= 0))
        {
            const string sqlB = "SELECT id FROM bazkidea WHERE emaila = @email LIMIT 1";
            using var cmdB = new MySqlCommand(sqlB, conn, trans);
            cmdB.Parameters.AddWithValue("@email", dto.Emaila.Trim());
            var bId = await cmdB.ExecuteScalarAsync();
            if (bId != null) dto.BazkideaId = Convert.ToInt32(bId);
        }

        dto.Lekua = dto.Lekua?.Trim() ?? "";
        dto.Emaila = dto.Emaila?.Trim() ?? "";
    }

    /// <summary>
    /// DTOtik abiatuta Erreserba domeinu-eredua eraikitzen du.
    /// </summary>
    private static Erreserba SortuErreserbaObjektua(int id, ErreserbaSortuDto dto) => new()
    {
        Id = id,
        PistaId = dto.PistaId ?? 0,
        Lekua = dto.Lekua ?? "",
        BazkideaId = dto.BazkideaId ?? 0,
        Emaila = dto.Emaila ?? "",
        Data = dto.Data,
        Ordua = dto.Ordua,
        Ezabatua = false,
        EzabaketaData = null,
        Sortua = DateTime.Now
    };

    /// <summary>
    /// SQL komandoari erreserbaren parametro guztiak eransten dizkio.
    /// </summary>
    private static void GehituErreserbaParametroak(MySqlCommand cmd, ErreserbaSortuDto dto)
    {
        cmd.Parameters.AddWithValue("@pista", dto.PistaId ?? 0);
        cmd.Parameters.AddWithValue("@pista_id", dto.PistaId ?? 0);
        cmd.Parameters.AddWithValue("@bazkidea", dto.BazkideaId ?? 0);
        cmd.Parameters.AddWithValue("@bazkidea_id", dto.BazkideaId ?? 0);
        cmd.Parameters.AddWithValue("@lekua", dto.Lekua ?? "");
        cmd.Parameters.AddWithValue("@data", dto.Data.ToDateTime(TimeOnly.MinValue));
        cmd.Parameters.AddWithValue("@ordua", dto.Ordua.ToTimeSpan());
        cmd.Parameters.AddWithValue("@emaila", dto.Emaila ?? "");
    }
}

