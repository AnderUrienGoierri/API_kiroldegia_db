using System.Data;
using API_kiroldegia_db.DTO;
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
    public async Task<IActionResult> LortuBonoak()
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
        return SuccessList(zerrenda, 200, "Bono guztiak ondo lortu dira.");
    }

    /// <summary>
    /// Email baten bonoa lortu.
    /// </summary>
    [HttpGet("{emaila}")]
    public async Task<IActionResult> LortuBonoa(string emaila)
    {
        await using var kon = await SortuKonexioaAsync();
        using var cmd = new MySqlCommand("SELECT emaila, saioak FROM bonoak WHERE emaila = @email;", kon);
        cmd.Parameters.AddWithValue("@email", emaila);

        using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            return NotFoundError($"Ez da aurkitu '{emaila}' emaila duen bonorik.");

        var bono = new BonoDTO
        {
            Emaila = reader.GetString("emaila"),
            Saioak = reader.GetInt32("saioak")
        };
        return Success(bono, 200, "Bonoa ondo lortu da.");
    }

    /// <summary>
    /// Bono berria sortu (edo erosi).
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> SortuBonoa([FromBody] BonoDTO dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Emaila) || dto.Saioak < 0)
            return BadRequestError("Emaila eta saio kopuru egokia (>=0) beharrezkoak dira.");

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

            return Created(new { mezua = $"Bonoa ondo eguneratu/sortu da ({dto.Emaila}: {dto.Saioak} saio)." }, 201, "Bonoa ondo sortu/eguneratu da.");
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return ServerError("Errorea bonoa sortzean: " + ex.Message);
        }
    }

    /// <summary>
    /// Bonoari saioak kargatu transakzio seguru batean (FOR UPDATE bidez).
    /// </summary>
    [HttpPut("{emaila}/kargatu")]
    public async Task<IActionResult> KargatuSaioak(string emaila, [FromBody] BonoKargatuDTO dto)
    {
        if (dto.Saioak <= 0)
            return BadRequestError("Saio kopuruak positiboa izan behar du (>0).");

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
                return NotFoundError($"Ez da aurkitu '{emaila}' emaila duen bonorik.");
            }

            int egungoSaioak = Convert.ToInt32(result);
            int saioBerriak = egungoSaioak + dto.Saioak;

            // 2. Eguneratu
            var cmdUpdate = new MySqlCommand("UPDATE bonoak SET saioak = @saioak WHERE emaila = @email;", kon, tx);
            cmdUpdate.Parameters.AddWithValue("@saioak", saioBerriak);
            cmdUpdate.Parameters.AddWithValue("@email", emaila);
            await cmdUpdate.ExecuteNonQueryAsync();

            await tx.CommitAsync();

            return Success(new
            {
                emaila,
                aurrekoSaioak = egungoSaioak,
                gehitutakoSaioak = dto.Saioak,
                saioak = saioBerriak,
                mezua = $"Bonoa arrakastaz kargatu da! Saio berriak: {saioBerriak}"
            }, 200, "Bonoa arrakastaz kargatu da.");
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return ServerError("Errorea bonoa kargatzean: " + ex.Message);
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
                return NotFoundError($"Ez da aurkitu '{emaila}' duen bonorik.");
            }

            int saioak = Convert.ToInt32(result);
            if (saioak <= 0)
            {
                await tx.RollbackAsync();
                return ConflictError($"Ez daukazu saio nahikorik bonoan! Egungo saioak: {saioak}");
            }

            // 2. Deskontatu saio 1
            int geratzenDira = saioak - 1;
            var cmdUpdate = new MySqlCommand("UPDATE bonoak SET saioak = @saioak WHERE emaila = @email;", kon, tx);
            cmdUpdate.Parameters.AddWithValue("@saioak", geratzenDira);
            cmdUpdate.Parameters.AddWithValue("@email", emaila);
            await cmdUpdate.ExecuteNonQueryAsync();

            await tx.CommitAsync();

            return Success(new
            {
                emaila,
                saioak = geratzenDira,
                mezua = $"Saio 1 erabili da. {geratzenDira} saio geratzen zaizkizu."
            }, 200, "Saio 1 erabili da.");
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return ServerError("Errorea bonoa erabiltzean: " + ex.Message);
        }
    }
}
