using System.Data;
using API_kiroldegia_db.Models;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

namespace API_kiroldegia_db.Controllers;

[ApiController]
[Route("api/bazkideak")]
public class BazkideakKontroladorea : BaseApiKontroladorea
{
    private readonly ILogger<BazkideakKontroladorea> _logger;

    public BazkideakKontroladorea(IConfiguration config, ILogger<BazkideakKontroladorea> logger)
        : base(config)
    {
        _logger = logger;
    }

    /// <summary>
    /// Sistemako bazkideak zerrendatu (bono saldoarekin batera).
    /// Bidea: GET /api/bazkideak
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<BazkideaDTO>>> LortuBazkideak([FromQuery] bool barneEzabatuak = true)
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
        return Ok(bazkideak);
    }

    /// <summary>
    /// Bazkide zehatz bat ID bidez lortu.
    /// Bidea: GET /api/bazkideak/{id}
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<BazkideaDTO>> LortuBazkidea(int id)
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
            return NotFound(new { mezua = $"Ez da aurkitu {id} ID-a duen bazkiderik." });
        }

        var b = new BazkideaDTO
        {
            Id = reader.GetInt32("id"),
            Izena = reader.GetString("izena"),
            Abizena = reader.GetString("abizena"),
            Emaila = reader.IsDBNull(reader.GetOrdinal("emaila")) ? null : reader.GetString("emaila"),
            BonoSaioak = reader.GetInt32("bono_saioak")
        };

        return Ok(b);
    }

    /// <summary>
    /// Bazkide berria sortu.
    /// Bidea: POST /api/bazkideak
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> SortuBazkidea([FromBody] BazkideaSortuDTO dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Izena) || string.IsNullOrWhiteSpace(dto.Abizena) || string.IsNullOrWhiteSpace(dto.Emaila))
        {
            return BadRequest(new { mezua = "Izena, abizena eta emaila beharrezkoak dira." });
        }

        await using var kon = await SortuKonexioaAsync();
        await using var tx = await kon.BeginTransactionAsync();

        try
        {
            // Emaila errepikatuta dagoen egiaztatu
            var cmdCheck = new MySqlCommand("SELECT COUNT(*) FROM bazkidea WHERE emaila = @email;", kon, tx);
            cmdCheck.Parameters.AddWithValue("@email", dto.Emaila.Trim().ToLower());
            long exist = Convert.ToInt64(await cmdCheck.ExecuteScalarAsync());
            if (exist > 0)
            {
                await tx.RollbackAsync();
                return Conflict(new { mezua = $"'{dto.Emaila}' emaila duen bazkide bat badago jada erregistratuta." });
            }

            var cmdInsert = new MySqlCommand(@"
                INSERT INTO bazkidea (izena, abizena, emaila)
                VALUES (@izena, @abizena, @email);", kon, tx);
            cmdInsert.Parameters.AddWithValue("@izena", dto.Izena.Trim());
            cmdInsert.Parameters.AddWithValue("@abizena", dto.Abizena.Trim());
            cmdInsert.Parameters.AddWithValue("@email", dto.Emaila.Trim().ToLower());
            await cmdInsert.ExecuteNonQueryAsync();
            long newId = cmdInsert.LastInsertedId;

            // Hasierako bonoa gehitu nahi bada
            if (dto.HasierakoBonoa > 0)
            {
                var cmdBono = new MySqlCommand(@"
                    INSERT INTO bonoak (emaila, saioak)
                    VALUES (@email, @saioak)
                    ON DUPLICATE KEY UPDATE saioak = saioak + @saioak;", kon, tx);
                cmdBono.Parameters.AddWithValue("@email", dto.Emaila.Trim().ToLower());
                cmdBono.Parameters.AddWithValue("@saioak", dto.HasierakoBonoa);
                await cmdBono.ExecuteNonQueryAsync();
            }

            await tx.CommitAsync();

            return StatusCode(StatusCodes.Status201Created, new
            {
                id = newId,
                izena = dto.Izena.Trim(),
                abizena = dto.Abizena.Trim(),
                emaila = dto.Emaila.Trim().ToLower(),
                bonoSaioak = dto.HasierakoBonoa,
                mezua = $"Bazkidea arrakastaz sortu da (ID: {newId})!"
            });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return StatusCode(StatusCodes.Status500InternalServerError, new { mezua = "Errorea bazkidea sortzean: " + ex.Message });
        }
    }

    /// <summary>
    /// Bazkidea ezabatu (Soft Delete edo Hard Delete).
    /// Bidea: DELETE /api/bazkideak/{id}?hard=false
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> EzabatuBazkidea(int id, [FromQuery] bool hard = false)
    {
        await using var kon = await SortuKonexioaAsync();
        await using var tx = await kon.BeginTransactionAsync();

        try
        {
            // Bazkidea existitzen den egiaztatu
            var cmdGet = new MySqlCommand("SELECT id, izena, abizena, emaila, ezabatua FROM bazkidea WHERE id = @id FOR UPDATE;", kon, tx);
            cmdGet.Parameters.AddWithValue("@id", id);
            string izena = "";
            bool jadaEzabatua = false;
            using (var reader = await cmdGet.ExecuteReaderAsync())
            {
                if (!await reader.ReadAsync())
                {
                    await tx.RollbackAsync();
                    return NotFound(new { mezua = $"Ez da aurkitu {id} ID-a duen bazkiderik." });
                }
                izena = $"{reader.GetString("izena")} {reader.GetString("abizena")}";
                jadaEzabatua = reader.GetBoolean("ezabatua");
            }

            if (!hard)
            {
                // SOFT DELETE:
                if (jadaEzabatua)
                {
                    await tx.RollbackAsync();
                    return BadRequest(new { mezua = $"[{id}] {izena} bazkidea lehendik ere Soft-Delete eginda dago." });
                }

                var cmdSoft = new MySqlCommand("UPDATE bazkidea SET ezabatua = TRUE, ezabaketa_data = NOW() WHERE id = @id;", kon, tx);
                cmdSoft.Parameters.AddWithValue("@id", id);
                await cmdSoft.ExecuteNonQueryAsync();

                await tx.CommitAsync();
                return Ok(new { mezua = $"[{id}] {izena} bazkidea logikoki ezabatu da (Soft Delete)." });
            }
            else
            {
                // HARD DELETE:
                // Erreserbarik baduen egiaztatu
                var cmdRes = new MySqlCommand("SELECT COUNT(*) FROM erreserba WHERE bazkidea_id = @id;", kon, tx);
                cmdRes.Parameters.AddWithValue("@id", id);
                long resCount = Convert.ToInt64(await cmdRes.ExecuteScalarAsync());
                if (resCount > 0)
                {
                    await tx.RollbackAsync();
                    return Conflict(new { mezua = $"Ezin da [{id}] bazkidea behin betiko ezabatu: {resCount} erreserba ditu lotuta!" });
                }

                // Bazkidea fisikoki ezabatu
                var cmdDel = new MySqlCommand("DELETE FROM bazkidea WHERE id = @id;", kon, tx);
                cmdDel.Parameters.AddWithValue("@id", id);
                await cmdDel.ExecuteNonQueryAsync();

                await tx.CommitAsync();
                return Ok(new { mezua = $"[{id}] {izena} bazkidea behin betiko ezabatu da (Hard Delete)." });
            }
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return StatusCode(StatusCodes.Status500InternalServerError, new { mezua = "Errorea bazkidea ezabatzean: " + ex.Message });
        }
    }

    /// <summary>
    /// Soft-deleted dagoen bazkide bat berreskuratu.
    /// Bidea: PUT /api/bazkideak/{id}/restore
    /// </summary>
    [HttpPut("{id}/restore")]
    public async Task<IActionResult> BerreskuratuBazkidea(int id)
    {
        await using var kon = await SortuKonexioaAsync();
        await using var tx = await kon.BeginTransactionAsync();

        try
        {
            var cmdCheck = new MySqlCommand("SELECT id, izena, abizena, ezabatua FROM bazkidea WHERE id = @id FOR UPDATE;", kon, tx);
            cmdCheck.Parameters.AddWithValue("@id", id);
            string izena = "";
            bool ezabatua = false;
            using (var reader = await cmdCheck.ExecuteReaderAsync())
            {
                if (!await reader.ReadAsync())
                {
                    await tx.RollbackAsync();
                    return NotFound(new { mezua = $"Ez da aurkitu {id} ID-a duen bazkiderik." });
                }
                izena = $"{reader.GetString("izena")} {reader.GetString("abizena")}";
                ezabatua = reader.GetBoolean("ezabatua");
            }

            if (!ezabatua)
            {
                await tx.RollbackAsync();
                return BadRequest(new { mezua = $"[{id}] {izena} bazkidea jada aktibo dago, ez dago ezabatuta." });
            }

            var cmdRestore = new MySqlCommand("UPDATE bazkidea SET ezabatua = FALSE, ezabaketa_data = NULL WHERE id = @id;", kon, tx);
            cmdRestore.Parameters.AddWithValue("@id", id);
            await cmdRestore.ExecuteNonQueryAsync();

            await tx.CommitAsync();
            return Ok(new { mezua = $"[{id}] {izena} bazkidea arrakastaz berreskuratu da!" });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return StatusCode(StatusCodes.Status500InternalServerError, new { mezua = "Errorea bazkidea berreskuratzean: " + ex.Message });
        }
    }
}

