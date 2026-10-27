using System.Data;
using API_kiroldegia_db.Models;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

namespace API_kiroldegia_db.Controllers;

[ApiController]
[Route("api/bonoak")]
[Route("api/[controller]")]
public class BonoakKontroladorea : BaseApiKontroladorea
{
    private readonly ILogger<BonoakKontroladorea> _logger;

    public BonoakKontroladorea(IConfiguration config, ILogger<BonoakKontroladorea> logger)
        : base(config)
    {
        _logger = logger;
    }

    /// <summary>
    /// Bono guztiak zerrendatu.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<BonoDTO>>> LortuBonoak()
    {
        await using var kon = await SortuKonexioaAsync();
        using var cmd = new MySqlCommand("SELECT emaila, saioak FROM bonoak ORDER BY emaila;", kon);
        var zerrenda = new List<BonoDTO>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            zerrenda.Add(new BonoDTO
            {
                Emaila = reader.GetString("emaila"),
                Saioak = reader.GetInt32("saioak")
            });
        }
        return Ok(zerrenda);
    }

    /// <summary>
    /// Email baten bonoa lortu.
    /// </summary>
    [HttpGet("{emaila}")]
    public async Task<ActionResult<BonoDTO>> LortuBonoa(string emaila)
    {
        await using var kon = await SortuKonexioaAsync();
        using var cmd = new MySqlCommand("SELECT emaila, saioak FROM bonoak WHERE emaila = @email;", kon);
        cmd.Parameters.AddWithValue("@email", emaila);

        using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            return NotFound(new { mezua = $"Ez da aurkitu '{emaila}' emaila duen bonorik." });

        return Ok(new BonoDTO
        {
            Emaila = reader.GetString("emaila"),
            Saioak = reader.GetInt32("saioak")
        });
    }

    /// <summary>
    /// Bono berria sortu (edo erosi).
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> SortuBonoa([FromBody] BonoDTO dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Emaila) || dto.Saioak < 0)
            return BadRequest(new { mezua = "Emaila eta saio kopuru egokia (>=0) beharrezkoak dira." });

        await using var kon = await SortuKonexioaAsync();
        await using var tx = await kon.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            var cmd = new MySqlCommand(@"
                INSERT INTO bonoak (emaila, saioak) 
                VALUES (@email, @saioak)
                ON DUPLICATE KEY UPDATE saioak = saioak + @saioak;", kon, tx);

            cmd.Parameters.AddWithValue("@email", dto.Emaila.Trim().ToLower());
            cmd.Parameters.AddWithValue("@saioak", dto.Saioak);

            await cmd.ExecuteNonQueryAsync();
            await tx.CommitAsync();

            return Ok(new { mezua = $"Bonoa ondo eguneratu/sortu da ({dto.Emaila}: {dto.Saioak} saio)." });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return StatusCode(StatusCodes.Status500InternalServerError, new { mezua = "Errorea bonoa sortzean: " + ex.Message });
        }
    }

    /// <summary>
    /// Bonoari saioak kargatu transakzio seguru batean (FOR UPDATE bidez).
    /// </summary>
    [HttpPut("{emaila}/kargatu")]
    public async Task<IActionResult> KargatuSaioak(string emaila, [FromBody] BonoKargatuDTO dto)
    {
        if (dto.Saioak <= 0)
            return BadRequest(new { mezua = "Saio kopuruak positiboa izan behar du (>0)." });

        await using var kon = await SortuKonexioaAsync();
        await using var tx = await kon.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            // 1. Erregistroa blokeatu transakzioan FOR UPDATE bidez
            var cmdCheck = new MySqlCommand("SELECT saioak FROM bonoak WHERE emaila = @email FOR UPDATE;", kon, tx);
            cmdCheck.Parameters.AddWithValue("@email", emaila);

            var result = await cmdCheck.ExecuteScalarAsync();
            if (result == null)
            {
                await tx.RollbackAsync();
                return NotFound(new { mezua = $"Ez da aurkitu '{emaila}' emaila duen bonorik." });
            }

            int egungoSaioak = Convert.ToInt32(result);
            int saioBerriak = egungoSaioak + dto.Saioak;

            // 2. Eguneratu
            var cmdUpdate = new MySqlCommand("UPDATE bonoak SET saioak = @saioak WHERE emaila = @email;", kon, tx);
            cmdUpdate.Parameters.AddWithValue("@saioak", saioBerriak);
            cmdUpdate.Parameters.AddWithValue("@email", emaila);
            await cmdUpdate.ExecuteNonQueryAsync();

            await tx.CommitAsync();

            return Ok(new
            {
                emaila,
                aurrekoSaioak = egungoSaioak,
                gehitutakoSaioak = dto.Saioak,
                saioak = saioBerriak,
                mezua = $"Bonoa arrakastaz kargatu da! Saio berriak: {saioBerriak}"
            });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return StatusCode(StatusCodes.Status500InternalServerError, new { mezua = "Errorea bonoa kargatzean: " + ex.Message });
        }
    }

    /// <summary>
    /// Bono baten saio 1 kontsumitu transakzioan (FOR UPDATE + saldo negatiboa ekiditeko).
    /// </summary>
    [HttpPost("{emaila}/erabili")]
    public async Task<IActionResult> ErabiliSaioa(string emaila)
    {
        await using var kon = await SortuKonexioaAsync();
        await using var tx = await kon.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            // 1. FOR UPDATE: blokeatu eta egiaztatu
            var cmdCheck = new MySqlCommand("SELECT saioak FROM bonoak WHERE emaila = @email FOR UPDATE;", kon, tx);
            cmdCheck.Parameters.AddWithValue("@email", emaila);

            var result = await cmdCheck.ExecuteScalarAsync();
            if (result == null)
            {
                await tx.RollbackAsync();
                return NotFound(new { mezua = $"Ez da aurkitu '{emaila}' duen bonorik." });
            }

            int saioak = Convert.ToInt32(result);
            if (saioak <= 0)
            {
                await tx.RollbackAsync();
                return Conflict(new { mezua = $"Ez daukazu saio nahikorik bonoan! Egungo saioak: {saioak}" });
            }

            // 2. Deskontatu saio 1
            int geratzenDira = saioak - 1;
            var cmdUpdate = new MySqlCommand("UPDATE bonoak SET saioak = @saioak WHERE emaila = @email;", kon, tx);
            cmdUpdate.Parameters.AddWithValue("@saioak", geratzenDira);
            cmdUpdate.Parameters.AddWithValue("@email", emaila);
            await cmdUpdate.ExecuteNonQueryAsync();

            await tx.CommitAsync();

            return Ok(new
            {
                emaila,
                saioak = geratzenDira,
                mezua = $"Saio 1 erabili da. {geratzenDira} saio geratzen zaizkizu."
            });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return StatusCode(StatusCodes.Status500InternalServerError, new { mezua = "Errorea bonoa erabiltzean: " + ex.Message });
        }
    }
}
