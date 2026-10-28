using System.ComponentModel.DataAnnotations;
using API_kiroldegia_db.Models;
using MySqlConnector;

namespace API_kiroldegia_db.DTO
{
    /// <summary>
    /// POST bidez erreserba berria sortzean jasotzen diren datuak (DTO).
    /// Pista eta Bazkidea bai ID bidez bai izen/email bidez zehaztea onartzen du.
    /// </summary>
    public class ErreserbaSortuDto
    {
        public string? Lekua { get; set; }
        public int? PistaId { get; set; }

        [Required]
        public DateOnly Data { get; set; }

        [Required]
        public TimeOnly Ordua { get; set; }

        public string? Emaila { get; set; }
        public int? BazkideaId { get; set; }

        public bool ErabiliBonoa { get; set; } = false;
        public int Itxaron { get; set; } = 0; // ms (konkurrentzia eta blokeoa simulatzeko)

        /// <summary>
        /// ID edo izen/email hutsuneak betetzen ditu datu-basetik kontsultatuz.
        /// </summary>
        public async Task BeteDatuakAsync(MySqlConnection conn, MySqlTransaction? trans = null)
        {
            // PistaId badago baina Lekua ez, pistaren izena berreskuratu
            if (string.IsNullOrWhiteSpace(Lekua) && PistaId.HasValue && PistaId.Value > 0)
            {
                const string sqlP = "SELECT izena FROM pista WHERE id = @pid";
                using var cmdP = new MySqlCommand(sqlP, conn, trans);
                cmdP.Parameters.AddWithValue("@pid", PistaId.Value);
                var izena = await cmdP.ExecuteScalarAsync();
                if (izena != null) Lekua = izena.ToString() ?? "";
            }
            // Lekua badago baina PistaId ez, pistaren ID-a berreskuratu
            else if (!string.IsNullOrWhiteSpace(Lekua) && (!PistaId.HasValue || PistaId.Value <= 0))
            {
                const string sqlP = "SELECT id FROM pista WHERE izena = @izena LIMIT 1";
                using var cmdP = new MySqlCommand(sqlP, conn, trans);
                cmdP.Parameters.AddWithValue("@izena", Lekua.Trim());
                var pId = await cmdP.ExecuteScalarAsync();
                if (pId != null) PistaId = Convert.ToInt32(pId);
            }

            // BazkideaId badago baina Emaila ez, bazkidearen emaila berreskuratu
            if (string.IsNullOrWhiteSpace(Emaila) && BazkideaId.HasValue && BazkideaId.Value > 0)
            {
                const string sqlB = "SELECT emaila FROM bazkidea WHERE id = @bid";
                using var cmdB = new MySqlCommand(sqlB, conn, trans);
                cmdB.Parameters.AddWithValue("@bid", BazkideaId.Value);
                var email = await cmdB.ExecuteScalarAsync();
                if (email != null) Emaila = email.ToString() ?? "";
            }
            // Emaila badago baina BazkideaId ez, bazkidearen ID-a berreskuratu
            else if (!string.IsNullOrWhiteSpace(Emaila) && (!BazkideaId.HasValue || BazkideaId.Value <= 0))
            {
                const string sqlB = "SELECT id FROM bazkidea WHERE emaila = @email LIMIT 1";
                using var cmdB = new MySqlCommand(sqlB, conn, trans);
                cmdB.Parameters.AddWithValue("@email", Emaila.Trim());
                var bId = await cmdB.ExecuteScalarAsync();
                if (bId != null) BazkideaId = Convert.ToInt32(bId);
            }

            Lekua = Lekua?.Trim() ?? "";
            Emaila = Emaila?.Trim() ?? "";
        }

        /// <summary>
        /// DTOtik abiatuta Erreserba domeinu-objektua eraikitzen du.
        /// </summary>
        public static Erreserba SortuErreserba(int id, ErreserbaSortuDto dto) => new()
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

        // ---- Laguntzaileak ----
        /// <summary>
        /// SQL komandoari erreserbaren parametro guztiak eransten dizkio.
        /// </summary>
        public static void GehituParametroak(MySqlCommand cmd, ErreserbaSortuDto dto)
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
}