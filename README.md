# Ordiziako Kiroldegia - REST API (`API_kiroldegia_db`)

ASP.NET Core Web API proiektua, MySQL datu-basearekin (`kiroldegia`), transakzio atomikoekin (`SELECT ... FOR UPDATE`), Soft/Hard Delete eta bonoen kudeaketarekin.

- **Biltegia:** [https://github.com/AnderUrienGoierri/API_kiroldegia_db](https://github.com/AnderUrienGoierri/API_kiroldegia_db)

---

## Ezaugarriak
- **Transakzio sendoak:** `SELECT ... FOR UPDATE` blokeoak erreserbak egitean hutsuneak libre daudela ziurtatzeko eta bonoen saldoak deskontatzeko.
- **Salbuespenen kontrola:** MySQL 1062 (UNIQUE constraint) eta 1213 (Deadlock) erroreak kudeatzen dira.
- **Ezabaketa bikoitza:**
  - **Soft Delete:** `ezabatua = TRUE`, pista libre uzten du baina datuak gordeta mantentzen ditu.
  - **Hard Delete:** Datu-basetik fisikoki ezabatzen du (`DELETE FROM`).
  - **Restore:** Soft Delete egindako erreserbak eta pistak berreskuratzeko aukera.
- **Bonoen sistema:** Bazkide bakoitzak saioak dituen bonoa erabil dezake erreserbak egiteko.

---

## Bideak (Endpoints)

| Metodoa | Bidea | Deskribapena |
|---|---|---|
| **GET** | `/` | APIaren egoera eta bideen zerrenda |
| **GET** | `/api/pistak?barneEzabatuak=true` | Pista guztiak zerrendatu |
| **GET** | `/api/erreserbak?barneEzabatuak=true` | Erreserba guztiak zerrendatu |
| **GET** | `/api/erreserbak/bazkideak` | Bazkideen zerrenda |
| **GET** | `/api/bonoak` | Bonoen saldoak |
| **POST** | `/api/erreserbak` | Erreserba berria egin (Transakzioa + FOR UPDATE) |
| **DELETE** | `/api/erreserbak/{id}?hard=false` | Soft Delete (erreserba logikoki ezabatu) |
| **PUT** | `/api/erreserbak/{id}/restore` | Erreserba berreskuratu (Restore) |
| **DELETE** | `/api/erreserbak/{id}?hard=true` | Hard Delete (behin betiko ezabaketa) |
| **PUT** | `/api/bonoak/{emaila}/kargatu` | Bonoari saioak kargatu |
| **DELETE** | `/api/erreserbak/garbitu` | Erreserba taula guztiz garbitu |

---

## Proiektua Martxan Jartzeko

1. **Datu-basea kargatu:**
   MySQL-n `kiroldegia_db.sql` gidoia exekutatu:
   ```bash
   mysql -u root -p < kiroldegia_db.sql
   ```

2. **APIa abiarazi:**
   ```bash
   dotnet run --project API_kiroldegia_db/API_kiroldegia_db.csproj
   ```
   Lehenetsita `http://localhost:5089` helbidean entzungo du.

3. **Postman probak:**
   Inportatu `Kiroldegia_API.postman_collection.json` fitxategia Postman-en proba guztiak egiteko.
   Datu-basearen konexio-probak eta pausoz pausoko gida xehea ikusteko: 👉 **[POSTMAN_GIDA.md](./POSTMAN_GIDA.md)**.
