# Postman Gida - Ordiziako Kiroldegia REST API

Gida honetan **Ordiziako Kiroldegiaren REST APIa** Postman tresnarekin nola probatu azaltzen da urratsez urrats.

---

## 1. Zergatik eman du `404 Not Found` zure pantailako irudian?

Zure Postman pantailan helbide hau jarri duzu:
```text
GET http://localhost:5089/
```

REST API batean eragiketak **bide zehatzetan (endpoint)** kokatzen dira (adibidez, `/api/pistak` edo `/api/erreserbak`).

> [!NOTE]
> Orain APIaren erroan (`http://localhost:5089/`) informazio-erantzun bat gehitu dugu; beraz, `Send` sakatzen baduzu, **`200 OK`** emango dizu eta bide guztiak zerrendatuko dizkizu.

---

## 2. Metodorik azkarrena: Postman Bilduma (Collection) Inportatzea

Zuretzat prestatuta utzi dugu eskaera guztien bilduma fitxategi honetan:
- [`Kiroldegia_API.postman_collection.json`](file:///c:/Dev/GoierriEskola/2_maila/0486-Datu-atzipena/1_ebaluaketa/6-transakzioak/ariketak/10_jarraipena/Kiroldegia_API.postman_collection.json)

### Nola inportatu Postman-en:
1. Ireki **Postman**.
2. Goiko ezkerrean klikatu **`Import`** botoian.
3. Arrastatu edo aukeratu fitxategi hau:
   `c:\Dev\GoierriEskola\2_maila\0486-Datu-atzipena\1_ebaluaketa\6-transakzioak\ariketak\10_jarraipena\Kiroldegia_API.postman_collection.json`
4. Automatikoki ezkerreko panelean **"Ordiziako Kiroldegia API"** karpeta sortuko da, prest dauden 10 eskaerekin!

---

## 3. Eskaerak banan-banan probatzeko taula

APIak `http://localhost:5089` (eta `http://localhost:5262`) helbideetan entzuten du:

| # | Metodoa | Bidea (URL) | Deskribapena | Body / Datuak |
|---|---|---|---|---|
| 0 | **GET** | `http://localhost:5089/` | APIaren egoera eta bideak | Ez du behar |
| 1 | **GET** | `http://localhost:5089/api/pistak` | 4 pistak lortu | Ez du behar |
| 2 | **GET** | `http://localhost:5089/api/erreserbak` | Egun dauden erreserbak | Ez du behar |
| 3 | **GET** | `http://localhost:5089/api/bazkideak` | **Bazkide GUZTIAK** (taulako 5 bazkideak) | Ez du behar |
| 4 | **GET** | `http://localhost:5089/api/erreserbak/bazkideak` | **Erreserbadun bazkideak** (soilik erreserba dutenak) | Ez du behar |
| 5 | **GET** | `http://localhost:5089/api/bonoak` | Bonoen saio erabilgarriak | Ez du behar |
| 5 | **POST** | `http://localhost:5089/api/erreserbak` | **Erreserba berria egin (Transakzioa)** | JSON (ikus beherago) |
| 6 | **DELETE** | `http://localhost:5089/api/erreserbak/1?hard=false` | **Soft Delete** (pista askatu, ezabatua=1) | Ez du behar |
| 7 | **PUT** | `http://localhost:5089/api/erreserbak/1/restore` | **Restore** (erreserba berreskuratu) | Ez du behar |
| 8 | **DELETE** | `http://localhost:5089/api/erreserbak/1?hard=true` | **Hard Delete** (behin betiko ezabatu) | Ez du behar |
| 9 | **PUT** | `http://localhost:5089/api/bonoak/ane@goierri.eus/kargatu` | Bonoari saioak kargatu | `{"saioak": 5}` |
| 10 | **DELETE** | `http://localhost:5089/api/erreserbak/garbitu` | Erreserba taula guztiz garbitu | Ez du behar |

---

## 4. Eskaera garrantzitsuenak nola konfiguratu Postman-en

### A) Pistak ikusi (GET)
1. Hautatu **`GET`** goitibeherako menuan.
2. Idatzi URLa:
   ```text
   http://localhost:5089/api/pistak
   ```
3. Sakatu **`Send`**.
4. Behean **`200 OK`** eta 4 pistak (`Tenisa 1`, `Tenisa 2`, `Padel 1`, `Frontoia`) ikusiko dituzu JSON formatuan.

---

### B) Erreserba berria sortu (POST transakzioarekin eta bonoaren deskontuarekin)
1. Aldatu metodoa **`POST`**-era.
2. URLa:
   ```text
   http://localhost:5089/api/erreserbak
   ```
3. URL azpian dauden pestainen artean, klikatu **`Body`**.
4. Hautatu **`raw`** irrati-botoia eta eskuineko formatu menuan aukeratu **`JSON`** (Testu arrunta beharrean).
5. Itsatsi JSON hau:
   ```json
   {
     "pistaId": 1,
     "bazkideaId": 1,
     "data": "2026-10-25",
     "ordua": "18:00:00",
     "erabiliBonoa": true
   }
   ```
6. Sakatu **`Send`**.
7. Erantzuna:
   - **`Status: 201 Created`**
   - Bonoaren saio 1 kenduko dio Ane Etxeberriari (10etik 9ra igaroz).

> [!TIP]
> **Probatu konkurrentzia eta blokeoa:**
> Esku hutsik utzi gabe, berriro sakatu **`Send`** datu berekin!
> APIak **`409 Conflict`** itzuliko du:
> *"Pista [1] eta ordu horretan (2026-10-25 18:00:00) jada erreserba aktibo bat eginda dago! Ezin da erreserbatu."*

---

### C) Soft Delete vs Hard Delete (DELETE)

#### Soft Delete (Ezabaketa logikoa):
Pista askatzen du beste batek erreserba dezan, baina erregistroa gordeta geratzen da estatistiketarako.
- Metodoa: **`DELETE`**
- URL: `http://localhost:5089/api/erreserbak/1?hard=false`
- Erantzuna: `200 OK` -> `[1] erreserba logikoki ezabatu da (Soft Delete).`

#### Berreskuratu (Restore):
Soft Delete egindako erreserba berrezarri nahi bada:
- Metodoa: **`PUT`**
- URL: `http://localhost:5089/api/erreserbak/1/restore`
- Erantzuna: `200 OK` -> `[1] erreserba arrakastaz berreskuratu da!`

#### Hard Delete (Ezabaketa fisikoa):
- Metodoa: **`DELETE`**
- URL: `http://localhost:5089/api/erreserbak/1?hard=true`
- Erantzuna: `200 OK` -> `[1] erreserba behin betiko ezabatu da (Hard Delete) datu-basetik.`

---

### D) Bonoari saioak kargatu (PUT)
1. Metodoa: **`PUT`**
2. URL: `http://localhost:5089/api/bonoak/ane@goierri.eus/kargatu`
3. **`Body`** -> **`raw`** -> **`JSON`**:
   ```json
   {
     "saioak": 5
   }
   ```
4. Sakatu **`Send`**.
5. Erantzuna: `200 OK` -> `+5 saio gehitu zaizkio bonoari transakzioan.`
