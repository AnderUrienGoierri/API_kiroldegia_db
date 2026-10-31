using System.Data;
using API_kiroldegia_db.DTO;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

namespace API_kiroldegia_db.Controllers;

public partial class ErreserbakKontroladorea
{
    /// <summary>
    /// Erreserba berria txertatu transakzioen teoria aplikatuz:
    /// - Pista aktibo dagoen egiaztatu (SELECT ezabatua FROM pista FOR UPDATE)
    /// - Bonoa egiaztatu eta saio 1 kendu (SELECT saioak FROM bonoak FOR UPDATE + UPDATE)
    /// - Hutsunea libre dagoen egiaztatu (SELECT COUNT(*) FOR UPDATE)
    /// - Txertatu (UNIQUE 1062 eta Deadlock 1213 salbuespenak kontrolatuz)
    /// </summary>
    [HttpPost]
    [HttpPost("transakzioa")]
    public async Task<IActionResult> SortuErreserba([FromBody] ErreserbaSortuDto dto, [FromQuery] int itxaron = 0)
    {
        int itxaronMs = itxaron > 0 ? itxaron : dto.Itxaron;
        await using var kon = await SortuKonexioaAsync();

        await BeteDatuakAsync(dto, kon);

        if ((dto.PistaId ?? 0) <= 0 || (dto.BazkideaId ?? 0) <= 0)
            return BadRequestError("Datu guztiak beharrezkoak dira (pistaId/lekua eta bazkideaId/emaila zehaztu behar dira).");

        await using var tx = await kon.BeginTransactionAsync(IsolationLevel.ReadCommitted);

        try
        {
            // 1. PISTA EGIAZTATU & BLOKEATU (Soft delete eginda badago, ezin da erreserbatu!)
            var cmdPista = new MySqlCommand("SELECT izena, ezabatua FROM pista WHERE id = @pista FOR UPDATE;", kon, tx);
            cmdPista.Parameters.AddWithValue("@pista", dto.PistaId!.Value);
            string pistaIzena = "";
            using (var readerP = await cmdPista.ExecuteReaderAsync())
            {
                if (!await readerP.ReadAsync())
                {
                    await tx.RollbackAsync();
                    return NotFoundError($"Ez da aurkitu {dto.PistaId} ID-a duen pistarik.");
                }
                pistaIzena = readerP.GetString("izena");
                bool ezabatua = readerP.GetBoolean("ezabatua");
                if (ezabatua)
                {
                    await tx.RollbackAsync();
                    return ConflictError($"[{dto.PistaId}] '{pistaIzena}' pista desgaituta (Soft-Delete) dago! Ezin da erreserbarik egin bertan.");
                }
            }

            // 2. BONOA KONTROLATU (Nahi bada edo bazkideak bonoa badu)
            int geratzenDirenSaioak = -1;
            string bazkideEmaila = "";
            if (dto.ErabiliBonoa)
            {
                // Bazkidearen emaila bilatu
                var cmdBazk = new MySqlCommand("SELECT emaila FROM bazkidea WHERE id = @bazkidea FOR UPDATE;", kon, tx);
                cmdBazk.Parameters.AddWithValue("@bazkidea", dto.BazkideaId!.Value);
                var emailObj = await cmdBazk.ExecuteScalarAsync();
                if (emailObj == null || emailObj == DBNull.Value || string.IsNullOrWhiteSpace(emailObj.ToString()))
                {
                    await tx.RollbackAsync();
                    return BadRequestError("Bazkide honek ez dauka emailarik erregistratuta bonoa erabiltzeko.");
                }
                bazkideEmaila = emailObj.ToString()!;

                // Bonoaren saldoa blokeatu eta egiaztatu
                var cmdBono = new MySqlCommand("SELECT saioak FROM bonoak WHERE emaila = @email FOR UPDATE;", kon, tx);
                cmdBono.Parameters.AddWithValue("@email", bazkideEmaila);
                var saioakObj = await cmdBono.ExecuteScalarAsync();
                if (saioakObj == null)
                {
                    await tx.RollbackAsync();
                    return NotFoundError($"'{bazkideEmaila}' emailarekin ez dago bonorik erregistratuta.");
                }

                int saioak = Convert.ToInt32(saioakObj);
                if (saioak <= 0)
                {
                    await tx.RollbackAsync();
                    return ConflictError($"Ez daukazu saio nahikorik bonoan! Egungo saioak: {saioak}. Mesedez kargatu bonoa lehenago.");
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

            GehituErreserbaParametroak(cmdCheck, dto);

            long kopurua = Convert.ToInt64(await cmdCheck.ExecuteScalarAsync());
            if (kopurua > 0)
            {
                await tx.RollbackAsync();
                return ConflictError($"Pista [{dto.PistaId}] eta ordu horretan ({dto.Data} {dto.Ordua}) jada erreserba aktibo bat eginda dago! Ezin da erreserbatu.");
            }

            // 4. Libre badago: erreserba txertatu transakzio barruan
            var cmdInsert = new MySqlCommand(@"
                INSERT INTO erreserba (pista_id, bazkidea_id, data, ordua, ezabatua) 
                VALUES (@pista, @bazkidea, @data, @ordua, FALSE);", kon, tx);

            GehituErreserbaParametroak(cmdInsert, dto);

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

            var sortutakoErreserba = SortuErreserbaObjektua((int)idBerria, dto);

            return Created(new
            {
                id = idBerria,
                mezua = "Erreserba arrakastaz sortu da!" + simulazioMezua + bonoMezua,
                bonoaErabilita = dto.ErabiliBonoa,
                bonoSaioak = geratzenDirenSaioak,
                blokeoaSimulatua = itxaronMs > 0,
                itxaronMs = itxaronMs,
                datuak = sortutakoErreserba
            }, 201, "Erreserba arrakastaz sortu da!");
        }
        catch (MySqlException ex) when (ex.Number == 1062) // UNIQUE constraint apurtu bada
        {
            await tx.RollbackAsync();
            return ConflictError("Gatazka (UNIQUE 1062): Pista, data eta ordu berean jada badago erreserba aktibo bat.");
        }
        catch (MySqlException ex) when (ex.Number == 1213) // Deadlock gertatu bada
        {
            await tx.RollbackAsync();
            return ConflictError("Deadlock (1213): Aldi bereko eragiketen arteko blokeo-gatazka gertatu da. Mesedez saiatu berriro.");
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return ServerError("Errorea transakzioan: " + ex.Message);
        }
    }
}

