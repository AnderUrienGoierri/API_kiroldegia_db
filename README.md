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
  - **Restore:** Soft Delete egindako erreserbak, pistak eta bazkideak berreskuratzeko aukera.
- **Bonoen sistema:** Bazkide bakoitzak saioak dituen bonoa erabil dezake erreserbak egiteko eta kargatzeko.

---

## Bideak (Endpoints)

| Metodoa | Bidea | Deskribapena |
|---|---|---|
| **GET** | `/` | APIaren egoera eta bideen zerrenda |
| **GET** | `/api/pistak?barneEzabatuak=true` | Pista guztiak zerrendatu |
| **GET** | `/api/pistak/{id}` | Pista zehatza ID bidez lortu |
| **POST** | `/api/pistak` | Pista berria sortu |
| **PUT** | `/api/pistak/{id}/restore` | Pista berreskuratu (Restore) |
| **DELETE** | `/api/pistak/{id}?hard=false` | Soft Delete (pista logikoki ezabatu) |
| **DELETE** | `/api/pistak/{id}?hard=true` | Hard Delete (pista behin betiko ezabatu) |
| **GET** | `/api/bazkideak?barneEzabatuak=true` | Bazkide guztiak bono-saldoarekin |
| **GET** | `/api/bazkideak/{id}` | Bazkide zehatza ID bidez lortu |
| **POST** | `/api/bazkideak` | Bazkide berria sortu (bonoarekin) |
| **PUT** | `/api/bazkideak/{id}/restore` | Bazkidea berreskuratu (Restore) |
| **DELETE** | `/api/bazkideak/{id}?hard=false` | Soft Delete (bazkidea logikoki ezabatu) |
| **DELETE** | `/api/bazkideak/{id}?hard=true` | Hard Delete (bazkidea behin betiko ezabatu) |
| **GET** | `/api/bonoak` | Bonoen saldo guztiak |
| **GET** | `/api/bonoak/{emaila}` | Bazkide baten bonoa lortu |
| **POST** | `/api/bonoak` | Bono berria erosi/sortu |
| **POST** | `/api/bonoak/{emaila}/erabili` | Bonoaren saio 1 kontsumitu |
| **PUT** | `/api/bonoak/{emaila}/kargatu` | Bonoari saioak kargatu |
| **GET** | `/api/erreserbak?barneEzabatuak=true` | Erreserba guztiak zerrendatu |
| **GET** | `/api/erreserbak/{id}` | Erreserba zehatza ID bidez lortu |
| **GET** | `/api/erreserbak/bazkideak` | Bazkide erreserbadunen zerrenda |
| **POST** | `/api/erreserbak` | Erreserba berria egin (Transakzioa + FOR UPDATE) |
| **POST** | `/api/erreserbak?itxaron=5000` | Erreserba egin blokeo-simulazioarekin |
| **PUT** | `/api/erreserbak/{id}/restore` | Erreserba berreskuratu (Restore) |
| **DELETE** | `/api/erreserbak/{id}?hard=false` | Soft Delete (erreserba logikoki ezabatu) |
| **DELETE** | `/api/erreserbak/{id}?hard=true` | Hard Delete (behin betiko ezabaketa) |
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
   Inportatu `Kiroldegia_API.postman_collection.json` fitxategia Postman-en. Eskaera guztiak 4 karpeta nagusitan daude antolatuta (`GET`, `POST`, `PUT`, `DELETE`).
   Gida osoa eta zehatza ikusteko: 👉 **[POSTMAN_GIDA.md](./POSTMAN_GIDA.md)**.
