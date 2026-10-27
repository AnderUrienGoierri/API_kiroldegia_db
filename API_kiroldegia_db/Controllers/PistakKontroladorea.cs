using System.Data;
using API_kiroldegia_db.Models;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

namespace API_kiroldegia_db.Controllers;

[ApiController]
[Route("api/pistak")]
public class PistakKontroladorea : BaseApiKontroladorea
{
    private readonly ILogger<PistakKontroladorea> _logger;

    public PistakKontroladorea(IConfiguration config, ILogger<PistakKontroladorea> logger)
        : base(config)
    {
        _logger = logger;
    }

    /// <summary>
    /// Pistak zerrendatu.
    /// barneEzabatuak: true bada, Soft-Delete bidez ezabatutakoak ere erakutsiko ditu.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PistaDTO>>> LortuPistak([FromQuery] bool barneEzabatuak = true)
    {
        await using var kon = await SortuKonexioaAsync();
        string sql = @"
            SELECT id, izena, kirola, ezabatua, ezabaketa_data 
            FROM pista 
            WHERE (@barne = TRUE OR ezabatua = FALSE)
            ORDER BY id;";

        using var cmd = new MySqlCommand(sql, kon);
        cmd.Parameters.AddWithValue("@barne", barneEzabatuak);

        var pistak = new List<PistaDTO>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            pistak.Add(new PistaDTO
            {
                Id = reader.GetInt32("id"),
                Izena = reader.GetString("izena"),
                Kirola = reader.GetString("kirola"),
                Ezabatua = reader.GetBoolean("ezabatua"),
                EzabaketaData = reader.IsDBNull(reader.GetOrdinal("ezabaketa_data"))
                    ? null
                    : reader.GetDateTime("ezabaketa_data")
            });
        }
        return Ok(pistak);
    }

    /// <summary>
    /// Pista bakarra ID bidez lortu.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<PistaDTO>> LortuPista(int id)
    {
        await using var kon = await SortuKonexioaAsync();
        using var cmd = new MySqlCommand("SELECT id, izena, kirola, ezabatua, ezabaketa_data FROM pista WHERE id = @id;", kon);
        cmd.Parameters.AddWithValue("@id", id);

        using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            return NotFound(new { mezua = $"Ez da aurkitu {id} ID-a duen pistarik." });

        return Ok(new PistaDTO
        {
            Id = reader.GetInt32("id"),
            Izena = reader.GetString("izena"),
            Kirola = reader.GetString("kirola"),
            Ezabatua = reader.GetBoolean("ezabatua"),
            EzabaketaData = reader.IsDBNull(reader.GetOrdinal("ezabaketa_data"))
                ? null
                : reader.GetDateTime("ezabaketa_data")
        });
    }

    /// <summary>
    /// Pista berria sortu.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> SortuPista([FromBody] PistaSortuDTO dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Izena) || string.IsNullOrWhiteSpace(dto.Kirola))
            return BadRequest(new { mezua = "Izena eta kirola beharrezkoak dira." });

        await using var kon = await SortuKonexioaAsync();
        var cmd = new MySqlCommand("INSERT INTO pista (izena, kirola, ezabatua) VALUES (@izena, @kirola, FALSE);", kon);
        cmd.Parameters.AddWithValue("@izena", dto.Izena);
        cmd.Parameters.AddWithValue("@kirola", dto.Kirola);
        await cmd.ExecuteNonQueryAsync();

        return StatusCode(StatusCodes.Status201Created, new
        {
            id = cmd.LastInsertedId,
            mezua = "Pista ondo sortu da!",
            datuak = dto
        });
    }

    /// <summary>
    /// Pista bat ezabatu (Soft Delete edo Hard Delete).
    /// ARIKETA 2-API.pdf eskakizuna: Soft delete egitean erlazionatutako erreserbak desgaitu egin behar dira!
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> EzabatuPista(int id, [FromQuery] bool hard = false)
    {
        await using var kon = await SortuKonexioaAsync();
        await using var tx = await kon.BeginTransactionAsync(IsolationLevel.ReadCommitted);

        try
        {
            // 1. FOR UPDATE: Pista egiaztatu eta blokeatu
            var cmdCheck = new MySqlCommand("SELECT id, izena, ezabatua FROM pista WHERE id = @id FOR UPDATE;", kon, tx);
            cmdCheck.Parameters.AddWithValue("@id", id);
            string pistaIzena = "";
            using (var reader = await cmdCheck.ExecuteReaderAsync())
            {
                if (!await reader.ReadAsync())
                {
                    await tx.RollbackAsync();
                    return NotFound(new { mezua = $"Ez da aurkitu {id} ID-a duen pistarik." });
                }

                pistaIzena = reader.GetString("izena");
                bool jadaEzabatua = reader.GetBoolean("ezabatua");
                if (!hard && jadaEzabatua)
                {
                    await tx.RollbackAsync();
                    return BadRequest(new { mezua = $"[{id}] {pistaIzena} pista lehendik ere Soft-Delete eginda dago." });
                }
            }

            if (hard)
            {
                // HARD DELETE:
                // Lehenengo pistaren erreserbak fisikoki ezabatu
                var cmdErreserbakDel = new MySqlCommand("DELETE FROM erreserba WHERE pista_id = @id;", kon, tx);
                cmdErreserbakDel.Parameters.AddWithValue("@id", id);
                int erreserbaKop = await cmdErreserbakDel.ExecuteNonQueryAsync();

                // Ondoren pista bera ezabatu
                var cmdPistaDel = new MySqlCommand("DELETE FROM pista WHERE id = @id;", kon, tx);
                cmdPistaDel.Parameters.AddWithValue("@id", id);
                await cmdPistaDel.ExecuteNonQueryAsync();

                await tx.CommitAsync();

                return Ok(new
                {
                    id,
                    mota = "HARD_DELETE",
                    mezua = $"[{id}] {pistaIzena} pista eta bere {erreserbaKop} erreserba behin betiko ezabatu dira datu-basetik (Hard Delete)."
                });
            }
            else
            {
                // SOFT DELETE:
                // 1) Pista ezabatuta markatu
                var cmdSoftPista = new MySqlCommand(@"
                    UPDATE pista 
                    SET ezabatua = TRUE, ezabaketa_data = NOW() 
                    WHERE id = @id;", kon, tx);
                cmdSoftPista.Parameters.AddWithValue("@id", id);
                await cmdSoftPista.ExecuteNonQueryAsync();

                // 2) Erlazionatutako erreserbak desgaituta (ezabatua=TRUE) bezala geratu behar dira
                var cmdSoftErreserbak = new MySqlCommand(@"
                    UPDATE erreserba 
                    SET ezabatua = TRUE, ezabaketa_data = NOW() 
                    WHERE pista_id = @id AND ezabatua = FALSE;", kon, tx);
                cmdSoftErreserbak.Parameters.AddWithValue("@id", id);
                int desgaituKop = await cmdSoftErreserbak.ExecuteNonQueryAsync();

                await tx.CommitAsync();

                return Ok(new
                {
                    id,
                    mota = "SOFT_DELETE",
                    desgaitutakoErreserbak = desgaituKop,
                    mezua = $"[{id}] {pistaIzena} pista logikoki ezabatu da (Soft Delete), eta erlazionatutako {desgaituKop} erreserba desgaitu dira."
                });
            }
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return StatusCode(StatusCodes.Status500InternalServerError, new { mezua = "Errorea pista ezabatzean: " + ex.Message });
        }
    }

    /// <summary>
    /// Soft Delete egindako pista berreskuratu (RESTORE).
    /// </summary>
    [HttpPut("{id}/restore")]
    public async Task<IActionResult> BerreskuratuPista(int id)
    {
        await using var kon = await SortuKonexioaAsync();
        await using var tx = await kon.BeginTransactionAsync(IsolationLevel.ReadCommitted);

        try
        {
            var cmdCheck = new MySqlCommand("SELECT id, izena, ezabatua FROM pista WHERE id = @id FOR UPDATE;", kon, tx);
            cmdCheck.Parameters.AddWithValue("@id", id);
            string pistaIzena = "";
            using (var reader = await cmdCheck.ExecuteReaderAsync())
            {
                if (!await reader.ReadAsync())
                {
                    await tx.RollbackAsync();
                    return NotFound(new { mezua = $"Ez da aurkitu {id} ID-a duen pistarik." });
                }

                pistaIzena = reader.GetString("izena");
                bool ezabatua = reader.GetBoolean("ezabatua");
                if (!ezabatua)
                {
                    await tx.RollbackAsync();
                    return BadRequest(new { mezua = "Pista hau aktibo dago jada, ez dago ezabatuta." });
                }
            }

            var cmdRestore = new MySqlCommand("UPDATE pista SET ezabatua = FALSE, ezabaketa_data = NULL WHERE id = @id;", kon, tx);
            cmdRestore.Parameters.AddWithValue("@id", id);
            await cmdRestore.ExecuteNonQueryAsync();

            await tx.CommitAsync();
            return Ok(new { id, mezua = $"[{id}] {pistaIzena} pista arrakastaz berreskuratu da!" });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return StatusCode(StatusCodes.Status500InternalServerError, new { mezua = "Errorea pista berreskuratzean: " + ex.Message });
        }
    }
}
