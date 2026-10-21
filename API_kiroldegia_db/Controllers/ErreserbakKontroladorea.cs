using System.Data;
using API_kiroldegia_db.Models;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

namespace API_kiroldegia_db.Controllers;

[ApiController]
[Route("api/erreserbak")]
public class ErreserbakKontroladorea : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly ILogger<ErreserbakKontroladorea> _logger;

    public ErreserbakKontroladorea(IConfiguration config, ILogger<ErreserbakKontroladorea> logger)
    {
        _config = config;
        _logger = logger;
    }

    private async Task<MySqlConnection> SortuKonexioaAsync()
    {
        string? katea = _config.GetConnectionString("Kiroldegia");
        try
        {
            var kon = new MySqlConnection(katea);
            await kon.OpenAsync();
            return kon;
        }
        catch
        {
            string? rootKatea = _config.GetConnectionString("KiroldegiaRoot");
            var kon = new MySqlConnection(rootKatea);
            await kon.OpenAsync();
            return kon;
        }
    }

    /// <summary>
    /// Erreserba guztiak lortu.
    /// barneEzabatuak: true bada, Soft-Delete bidez ezabatutakoak ere erakutsiko ditu.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ErreserbaDTO>>> LortuErreserbak([FromQuery] bool barneEzabatuak = true)
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

        return Ok(emaitzak);
    }

    /// <summary>
    /// Erreserba bakarra ID bidez lortu.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ErreserbaDTO>> LortuErreserba(int id)
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
            return NotFound(new { mezua = $"Ez da aurkitu {id} ID-a duen erreserbarik." });

        return Ok(new ErreserbaDTO
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

    /// <summary>
    /// Soilik gutxienez erreserba bat duten bazkideak lortu.
    /// Bidea: GET /api/erreserbak/bazkideak
    /// barneEzabatuak: true bada, ezabatutako erreserbak dituztenak ere barne hartzen ditu.
    /// </summary>
    [HttpGet("bazkideak")]
    public async Task<ActionResult<IEnumerable<BazkideaDTO>>> LortuErreserbadunBazkideak([FromQuery] bool barneEzabatuak = true)
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
        return Ok(bazkideak);
    }

    /// <summary>
    /// Erreserba berria txertatu transakzioen teoria aplikatuz:
    /// - Pista aktibo dagoen egiaztatu (SELECT ezabatua FROM pista FOR UPDATE)
    /// - Bonoa egiaztatu eta saio 1 kendu (SELECT saioak FROM bonoak FOR UPDATE + UPDATE)
    /// - Hutsunea libre dagoen egiaztatu (SELECT COUNT(*) FOR UPDATE)
    /// - Txertatu (UNIQUE 1062 eta Deadlock 1213 salbuespenak kontrolatuz)
    /// </summary>
    [HttpPost]
    [HttpPost("transakzioa")]
    public async Task<IActionResult> SortuErreserba([FromBody] ErreserbaSortuDTO dto, [FromQuery] int itxaron = 0)
    {
        int itxaronMs = itxaron > 0 ? itxaron : dto.Itxaron;
        await using var kon = await SortuKonexioaAsync();

        if (dto.PistaId <= 0 && !string.IsNullOrWhiteSpace(dto.Lekua))
        {
            using var cmdP = new MySqlCommand("SELECT id FROM pista WHERE izena = @izena LIMIT 1;", kon);
            cmdP.Parameters.AddWithValue("@izena", dto.Lekua.Trim());
            var pId = await cmdP.ExecuteScalarAsync();
            if (pId != null) dto.PistaId = Convert.ToInt32(pId);
        }

        if (dto.BazkideaId <= 0 && !string.IsNullOrWhiteSpace(dto.Emaila))
        {
            using var cmdB = new MySqlCommand("SELECT id FROM bazkidea WHERE emaila = @email LIMIT 1;", kon);
            cmdB.Parameters.AddWithValue("@email", dto.Emaila.Trim());
            var bId = await cmdB.ExecuteScalarAsync();
            if (bId != null) dto.BazkideaId = Convert.ToInt32(bId);
        }

        if (dto.PistaId <= 0 || dto.BazkideaId <= 0 || string.IsNullOrWhiteSpace(dto.Data) || string.IsNullOrWhiteSpace(dto.Ordua))
            return BadRequest(new { mezua = "Datu guztiak beharrezkoak dira (pistaId, bazkideaId, data, ordua)." });

        await using var tx = await kon.BeginTransactionAsync(IsolationLevel.ReadCommitted);

        try
        {
            // 1. PISTA EGIAZTATU & BLOKEATU (Soft delete eginda badago, ezin da erreserbatu!)
            var cmdPista = new MySqlCommand("SELECT izena, ezabatua FROM pista WHERE id = @pista FOR UPDATE;", kon, tx);
            cmdPista.Parameters.AddWithValue("@pista", dto.PistaId);
            string pistaIzena = "";
            using (var readerP = await cmdPista.ExecuteReaderAsync())
            {
                if (!await readerP.ReadAsync())
                {
                    await tx.RollbackAsync();
                    return NotFound(new { mezua = $"Ez da aurkitu {dto.PistaId} ID-a duen pistarik." });
                }
                pistaIzena = readerP.GetString("izena");
                bool ezabatua = readerP.GetBoolean("ezabatua");
                if (ezabatua)
                {
                    await tx.RollbackAsync();
                    return Conflict(new { mota = "PISTA_DESGAITUTA", mezua = $"[{dto.PistaId}] '{pistaIzena}' pista desgaituta (Soft-Delete) dago! Ezin da erreserbarik egin bertan." });
                }
            }

            // 2. BONOA KONTROLATU (Nahi bada edo bazkideak bonoa badu)
            int geratzenDirenSaioak = -1;
            string bazkideEmaila = "";
            if (dto.ErabiliBonoa)
            {
                // Bazkidearen emaila bilatu
                var cmdBazk = new MySqlCommand("SELECT emaila FROM bazkidea WHERE id = @bazkidea FOR UPDATE;", kon, tx);
                cmdBazk.Parameters.AddWithValue("@bazkidea", dto.BazkideaId);
                var emailObj = await cmdBazk.ExecuteScalarAsync();
                if (emailObj == null || emailObj == DBNull.Value || string.IsNullOrWhiteSpace(emailObj.ToString()))
                {
                    await tx.RollbackAsync();
                    return BadRequest(new { mezua = "Bazkide honek ez dauka emailarik erregistratuta bonoa erabiltzeko." });
                }
                bazkideEmaila = emailObj.ToString()!;

                // Bonoaren saldoa blokeatu eta egiaztatu
                var cmdBono = new MySqlCommand("SELECT saioak FROM bonoak WHERE emaila = @email FOR UPDATE;", kon, tx);
                cmdBono.Parameters.AddWithValue("@email", bazkideEmaila);
                var saioakObj = await cmdBono.ExecuteScalarAsync();
                if (saioakObj == null)
                {
                    await tx.RollbackAsync();
                    return BadRequest(new { mezua = $"'{bazkideEmaila}' emailarekin ez dago bonorik erregistratuta." });
                }

                int saioak = Convert.ToInt32(saioakObj);
                if (saioak <= 0)
                {
                    await tx.RollbackAsync();
                    return Conflict(new { mota = "BONO_SALDOA", mezua = $"Ez daukazu saio nahikorik bonoan! Egungo saioak: {saioak}. Mesedez kargatu bonoa lehenago." });
                }

                // Saio 1 deskontatu
                geratzenDirenSaioak = saioak - 1;
                var cmdUpdateBono = new MySqlCommand("UPDATE bonoak SET saioak = @saioak WHERE emaila = @email;", kon, tx);
                cmdUpdateBono.Parameters.AddWithValue("@saioak", geratzenDirenSaioak);
                cmdUpdateBono.Parameters.AddWithValue("@email", bazkideEmaila);
                await cmdUpdateBono.ExecuteNonQueryAsync();
            }

            // 3. FOR UPDATE: Erreserba eginda dagoen begiratu transakzio barruan (soilik ezabatua ez bada)
            var cmdCheck = new MySqlCommand(@"
                SELECT COUNT(*) FROM erreserba 
                WHERE pista_id = @pista AND data = @data AND ordua = @ordua AND ezabatua = FALSE 
                FOR UPDATE;", kon, tx);

            cmdCheck.Parameters.AddWithValue("@pista", dto.PistaId);
            cmdCheck.Parameters.AddWithValue("@data", dto.Data);
            cmdCheck.Parameters.AddWithValue("@ordua", dto.Ordua);

            long kopurua = Convert.ToInt64(await cmdCheck.ExecuteScalarAsync());
            if (kopurua > 0)
            {
                await tx.RollbackAsync();
                return Conflict(new { mota = "PISTA_OKUPATUTA", mezua = $"Pista [{dto.PistaId}] eta ordu horretan ({dto.Data} {dto.Ordua}) jada erreserba aktibo bat eginda dago! Ezin da erreserbatu." });
            }

            // 4. Libre badago: erreserba txertatu transakzio barruan
            var cmdInsert = new MySqlCommand(@"
                INSERT INTO erreserba (pista_id, bazkidea_id, data, ordua, ezabatua) 
                VALUES (@pista, @bazkidea, @data, @ordua, FALSE);", kon, tx);

            cmdInsert.Parameters.AddWithValue("@pista", dto.PistaId);
            cmdInsert.Parameters.AddWithValue("@bazkidea", dto.BazkideaId);
            cmdInsert.Parameters.AddWithValue("@data", dto.Data);
            cmdInsert.Parameters.AddWithValue("@ordua", dto.Ordua);

            await cmdInsert.ExecuteNonQueryAsync();
            long idBerria = cmdInsert.LastInsertedId;

            // SIMULAZIOA: Konkurrentzia eta blokeoa (Lock) frogatzeko geldialdi artifiziala
            if (itxaronMs > 0)
            {
                await Task.Delay(itxaronMs);
            }

            await tx.CommitAsync();

            string bonoMezua = dto.ErabiliBonoa 
                ? $" Bonoaren saio 1 erabili da ({bazkideEmaila}, {geratzenDirenSaioak} saio geratzen dira)." 
                : "";

            string simulazioMezua = itxaronMs > 0 
                ? $" ({itxaronMs / 1000}s-ko blokeoa simulatu da)." 
                : "";

            return StatusCode(StatusCodes.Status201Created, new
            {
                id = idBerria,
                mezua = "Erreserba arrakastaz sortu da!" + simulazioMezua + bonoMezua,
                bonoaErabilita = dto.ErabiliBonoa,
                bonoSaioak = geratzenDirenSaioak,
                blokeoaSimulatua = itxaronMs > 0,
                itxaronMs = itxaronMs,
                datuak = dto
            });
        }
        catch (MySqlException ex) when (ex.Number == 1062) // UNIQUE constraint apurtu bada
        {
            await tx.RollbackAsync();
            return Conflict(new { mota = "KONKURRENTZIA", mezua = "Konkurrentzia-gatazka (MySQL 1062): Beste erabiltzaile batek aldi berean pista eta ordu bera erreserbatu berri du." });
        }
        catch (MySqlException ex) when (ex.Number == 1213) // Deadlock
        {
            await tx.RollbackAsync();
            return Conflict(new { mota = "KONKURRENTZIA", mezua = "Konkurrentzia-blokeoa (MySQL 1213): Deadlock (elkar-blokeoa) gertatu da aldi bereko transakzioen artean. Saiatu berriro." });
        }
        catch (MySqlException ex) when (ex.Number == 1205) // Lock wait timeout
        {
            await tx.RollbackAsync();
            return Conflict(new { mota = "KONKURRENTZIA", mezua = "Konkurrentzia-blokeoa (MySQL 1205): Blokeoaren denbora-muga gainditu da (Lock wait timeout exceeded). Beste transakzio batek pista blokeatuta mantentzen du." });
        }
        catch (Exception ex)

        {
            await tx.RollbackAsync();
            return StatusCode(StatusCodes.Status500InternalServerError, new { mezua = "Errorea transakzioan: " + ex.Message });
        }
    }

    /// <summary>
    /// Erreserba bat ezabatu.
    /// hard = false: Soft Delete (ezabatua=TRUE).
    /// hard = true: Hard Delete (fisikoki DELETE FROM).
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> EzabatuErreserba(int id, [FromQuery] bool hard = false)
    {
        await using var kon = await SortuKonexioaAsync();
        await using var tx = await kon.BeginTransactionAsync(IsolationLevel.ReadCommitted);

        try
        {
            // 1. FOR UPDATE: Erregistroa blokeatu transakzioan
            var cmdCheck = new MySqlCommand("SELECT id, ezabatua FROM erreserba WHERE id = @id FOR UPDATE;", kon, tx);
            cmdCheck.Parameters.AddWithValue("@id", id);
            using (var reader = await cmdCheck.ExecuteReaderAsync())
            {
                if (!await reader.ReadAsync())
                {
                    await tx.RollbackAsync();
                    return NotFound(new { mezua = $"Ez da aurkitu {id} ID-a duen erreserbarik." });
                }

                bool jadaEzabatua = reader.GetBoolean("ezabatua");
                if (!hard && jadaEzabatua)
                {
                    await tx.RollbackAsync();
                    return BadRequest(new { mezua = $"[{id}] erreserba lehendik ere Soft-Delete eginda dago." });
                }
            }

            if (hard)
            {
                var cmdHard = new MySqlCommand("DELETE FROM erreserba WHERE id = @id;", kon, tx);
                cmdHard.Parameters.AddWithValue("@id", id);
                await cmdHard.ExecuteNonQueryAsync();

                await tx.CommitAsync();
                return Ok(new
                {
                    id,
                    mota = "HARD_DELETE",
                    mezua = $"[{id}] erreserba behin betiko ezabatu da (Hard Delete) datu-basetik."
                });
            }
            else
            {
                var cmdSoft = new MySqlCommand(@"
                    UPDATE erreserba 
                    SET ezabatua = TRUE, ezabaketa_data = NOW() 
                    WHERE id = @id;", kon, tx);
                cmdSoft.Parameters.AddWithValue("@id", id);
                await cmdSoft.ExecuteNonQueryAsync();

                await tx.CommitAsync();
                return Ok(new
                {
                    id,
                    mota = "SOFT_DELETE",
                    mezua = $"[{id}] erreserba logikoki ezabatu da (Soft Delete). Pista eta ordu hori libre geratu da besteentzat."
                });
            }
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return StatusCode(StatusCodes.Status500InternalServerError, new { mezua = "Errorea ezabatzean: " + ex.Message });
        }
    }

    /// <summary>
    /// Soft Delete egindako erreserba bat berrezarri / desegin ezabaketa (RESTORE).
    /// </summary>
    [HttpPut("{id}/restore")]
    public async Task<IActionResult> BerreskuratuErreserba(int id)
    {
        await using var kon = await SortuKonexioaAsync();
        await using var tx = await kon.BeginTransactionAsync(IsolationLevel.ReadCommitted);

        try
        {
            int pistaId;
            string data;
            string ordua;
            bool ezabatua;

            var cmdGet = new MySqlCommand("SELECT pista_id, data, ordua, ezabatua FROM erreserba WHERE id = @id FOR UPDATE;", kon, tx);
            cmdGet.Parameters.AddWithValue("@id", id);
            using (var reader = await cmdGet.ExecuteReaderAsync())
            {
                if (!await reader.ReadAsync())
                {
                    await tx.RollbackAsync();
                    return NotFound(new { mezua = $"Ez da aurkitu {id} ID-a duen erreserbarik." });
                }
                ezabatua = reader.GetBoolean("ezabatua");
                if (!ezabatua)
                {
                    await tx.RollbackAsync();
                    return BadRequest(new { mezua = "Erreserba hau aktibo dago jada, ez dago ezabatuta." });
                }
                pistaId = reader.GetInt32("pista_id");
                data = Convert.ToDateTime(reader["data"]).ToString("yyyy-MM-dd");
                ordua = reader["ordua"].ToString() ?? "";
            }

            // Pista bera ezabatuta ez dagoela egiaztatu
            var cmdPista = new MySqlCommand("SELECT ezabatua FROM pista WHERE id = @p FOR UPDATE;", kon, tx);
            cmdPista.Parameters.AddWithValue("@p", pistaId);
            bool pistaEzabatua = Convert.ToBoolean(await cmdPista.ExecuteScalarAsync());
            if (pistaEzabatua)
            {
                await tx.RollbackAsync();
                return Conflict(new { mezua = "Ezin da erreserba berreskuratu: pista bera desgaituta (Soft Delete) dago." });
            }

            // Hutsunea tartean hartuta ote dagoen begiratu
            var cmdCheck = new MySqlCommand(@"
                SELECT COUNT(*) FROM erreserba 
                WHERE pista_id = @p AND data = @d AND ordua = @o AND ezabatua = FALSE AND id != @id
                FOR UPDATE;", kon, tx);
            cmdCheck.Parameters.AddWithValue("@p", pistaId);
            cmdCheck.Parameters.AddWithValue("@d", data);
            cmdCheck.Parameters.AddWithValue("@o", ordua);
            cmdCheck.Parameters.AddWithValue("@id", id);

            long okupatua = Convert.ToInt64(await cmdCheck.ExecuteScalarAsync());
            if (okupatua > 0)
            {
                await tx.RollbackAsync();
                return Conflict(new { mezua = "Ezin da berreskuratu: beste erreserba aktibo bat dago pista eta ordu berean." });
            }

            // Restore egin
            var cmdRestore = new MySqlCommand("UPDATE erreserba SET ezabatua = FALSE, ezabaketa_data = NULL WHERE id = @id;", kon, tx);
            cmdRestore.Parameters.AddWithValue("@id", id);
            await cmdRestore.ExecuteNonQueryAsync();

            await tx.CommitAsync();
            return Ok(new { id, mezua = $"[{id}] erreserba arrakastaz berreskuratu da!" });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return StatusCode(StatusCodes.Status500InternalServerError, new { mezua = "Errorea berreskuratzean: " + ex.Message });
        }
    }

    /// <summary>
    /// Erreserba taula guztiz garbitu probak hutsetik hasteko.
    /// </summary>
    [HttpDelete("garbitu")]
    public async Task<IActionResult> GarbituErreserbak()
    {
        await using var kon = await SortuKonexioaAsync();
        var cmd = new MySqlCommand("DELETE FROM erreserba;", kon);
        int kopurua = await cmd.ExecuteNonQueryAsync();
        return Ok(new { mezua = $"Erreserba guztiak ezabatu dira ({kopurua} errenkada)." });
    }
}
