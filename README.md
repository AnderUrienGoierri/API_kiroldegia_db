# Ordiziako Kiroldegia - REST API (`API_kiroldegia_db`)

ASP.NET Core Web API profesionala, MySQL datu-basearekin (`kiroldegia`), transakzio atomikoekin (`SELECT ... FOR UPDATE`), Soft/Hard Delete mekanismoekin, bonoen kudeaketarekin eta arkitektura garbi homogeneoarekin (Clean Architecture eta Single Responsibility Principle).

- **Biltegia:** [https://github.com/AnderUrienGoierri/API_kiroldegia_db](https://github.com/AnderUrienGoierri/API_kiroldegia_db)
- **Portua:** `http://localhost:5089`

---

## Proiektuaren Arkitektura eta Egitura

Proiektua arduren banaketa garbia jarraituz antolatuta dago. Karpetak eta klaseak **fitxategi bakoitzeko klase bat** (C# konbentzio estandarra) jarraituz egituratu dira:

```text
API_kiroldegia_db/
├── Controllers/                  # API kontroladoreak karpetatan eta ekintzaka banatuta
│   ├── BaseApiKontroladorea.cs   # Oinarrizko kontroladorea (erantzun bateratuak)
│   ├── Pistak/                   # Pistak kudeatzeko ekintzak (partial class)
│   │   ├── PistakKontroladorea.cs
│   │   ├── LortuPistak.cs
│   │   ├── LortuPista.cs
│   │   ├── SortuPista.cs
│   │   ├── EzabatuPista.cs
│   │   └── BerreskuratuPista.cs
│   ├── Bazkideak/                # Bazkideak kudeatzeko ekintzak (partial class)
│   │   ├── BazkideakKontroladorea.cs
│   │   ├── LortuBazkideak.cs
│   │   ├── LortuBazkidea.cs
│   │   ├── SortuBazkidea.cs
│   │   ├── EzabatuBazkidea.cs
│   │   └── BerreskuratuBazkidea.cs
│   ├── Bonoak/                   # Bonoak kudeatzeko ekintzak (partial class)
│   │   ├── BonoakKontroladorea.cs
│   │   ├── LortuBonoak.cs
│   │   ├── LortuBonoa.cs
│   │   ├── SortuBonoa.cs
│   │   ├── KargatuSaioak.cs
│   │   ├── ErabiliSaioa.cs
│   │   ├── EzabatuBonoa.cs
│   │   └── BerreskuratuBonoa.cs
│   └── Erreserbak/               # Erreserben transakzioak eta ekintzak (partial class)
│       ├── ErreserbakKontroladorea.cs
│       ├── LortuErreserbak.cs
│       ├── LortuErreserba.cs
│       ├── LortuErreserbadunBazkideak.cs
│       ├── SortuErreserba.cs
│       ├── EzabatuErreserba.cs
│       ├── BerreskuratuErreserba.cs
│       └── GarbituErreserbak.cs
├── DTO/                          # Data Transfer Objects (POCO hutsak, 1 fitxategi = 1 klase)
│   ├── PistaDto.cs               # Pista kontsultatzeko DTOa (GET)
│   ├── PistaSortuDto.cs          # Pista berria sortzeko DTOa (POST)
│   ├── BazkideaDto.cs            # Bazkidea kontsultatzeko DTOa (GET)
│   ├── BazkideaSortuDto.cs       # Bazkide berria sortzeko DTOa (POST)
│   ├── BonoDto.cs                # Bonoa kontsultatzeko DTOa (GET)
│   ├── BonoKargatuDto.cs         # Bonoari saioak kargatzeko DTOa (PUT)
│   ├── ErreserbaDto.cs           # Erreserba kontsultatzeko DTOa (GET)
│   └── ErreserbaSortuDto.cs      # Erreserba berria eskatzeko DTOa (POST)
├── Models/                       # Domeinu-ereduak (MySQL taulekin 1:1 mapatuta)
│   ├── Pista.cs                  # 'pista' taularen eredua
│   ├── Bazkidea.cs               # 'bazkidea' taularen eredua
│   ├── Bonoa.cs                  # 'bonoak' taularen eredua
│   └── Erreserba.cs              # 'erreserba' taularen eredua
├── Responses/                    # API erantzunen bilgarri estandarra
│   ├── ApiResponse.cs            # Arrakasta erantzunen formatu uniformea (success, code, message, data, timestamp)
│   └── ApiError.cs               # Akats erantzunen formatu uniformea (status, type, code, message, detail)
├── Program.cs                    # Aplikazioaren konfigurazioa, DI, CORS eta '/' erroko ibilbidea
└── appsettings.json              # Datu-baseko konexio-katea (ConnectionStrings:DefaultConnection)
```

---

## DTOen Diseinu-Printzipioak (Clean Design)

1. **Ardura Bakarra (Single Responsibility Principle - SRP):**
   - DTOek **datuak soilik garraiatzen dituzte** (JSON eta C# propietateen artean).
   - DTOek **ez dute datu-baseko sarbiderik** (`MySqlConnection` edo `SELECT` kontsultarik ez dago DTOetan).
2. **Homogeneotasuna eta Estandarrak:**
   - DTO klase bakoitzak bere fitxategi propioa du (`DTO/` karpetan), izen bera daramana.
   - Kontsultetarako DTOak (`*Dto.cs`) eta eskaeretarako DTOak (`*SortuDto.cs`, `*KargatuDto.cs`) bereizita daude.
3. **Logika kapsulatua kontroladoreetan:**
   - Datuak osatzeko (`BeteDatuakAsync`), parametroak gehitzeko (`GehituErreserbaParametroak`) eta ereduak eraikitzeko metodoak kontroladoreari dagozkio eta metodo laguntzaile pribatu gisa kapsulatuta daude ([ErreserbakKontroladorea.cs](file:///c:/Dev/GoierriEskola/2_maila/0486-Datu-atzipena/1_ebaluaketa/6-transakzioak/ariketak/10_jarraipena/API_kiroldegia_db/API_kiroldegia_db/Controllers/Erreserbak/ErreserbakKontroladorea.cs)).

---

## Kontroladoreen Egitura Modularra (Partial Classes)

Kontroladore bakoitza bere baliabidearen araberako karpeta batean antolatu da (`Controllers/Pistak/`, `Controllers/Bazkideak/`, `Controllers/Bonoak/` eta `Controllers/Erreserbak/`).
C#-ko `partial class` mekanismoa erabiliz:

- Karpetako kontroladore nagusiak (`*Kontroladorea.cs`) ibilbidearen (`[Route]`) konfigurazioa, mendekotasun-injekzioa (`ILogger`, `IConfiguration`) eta klasearen oinarrizko definizioa dauzka.
- Endpoint edo ekintza bakoitzak bere fitxategi propioa du (`Lortu*.cs`, `Sortu*.cs`, `Ezabatu*.cs`...), ardura bakar baten inguruan zentratuta eta kodearen irakurgarritasuna nabarmen hobetuz.
- Konpilatzaileak klase bakar batean elkartzen ditu atal guztiak, ASP.NET Core-ren ibilbide-mekanismoari erabateko bateragarritasuna emanez.

---

## Ezaugarri Tekniko Nagusiak

- **Transakzio sendoak eta atomikoak:** `SELECT ... FOR UPDATE` blokeoak erreserbak egitean hutsuneak libre daudela ziurtatzeko eta bonoen saldoak aldi berean deskontatzeko (race conditions saihestuz).
- **Salbuespenen kontrol zehatza:** MySQL 1062 (UNIQUE constraint) eta 1213 (Deadlock) erroreak kudeatzen dira rollback automatikoekin.
- **Ezabaketa bikoitza (Soft eta Hard Delete):**
  - **Soft Delete (`?hard=false`):** `ezabatua = TRUE`, pista libre uzten du baina datu historikoak gordeta mantentzen ditu. Pistak desgaitzean erlazionatutako erreserbak automatikoki desgaitzen dira.
  - **Hard Delete (`?hard=true`):** Datu-basetik fisikoki ezabatzen du (`DELETE FROM`).
  - **Restore (`PUT .../restore`):** Desgaitutako erreserbak, pistak, bazkideak eta bonoak lehengoratzeko aukera.
- **Bonoen sistema:** Bazkide bakoitzak saioak dituen bonoa erabil dezake erreserbak ordaintzeko (`erabiliBonoa=true`), saioak kargatzeko eta saldoak kudeatzeko.

---

## Bideak (29 Endpoints)

Metodoen arabera ordenatuta:

### 1. GET (9 endpoint)

| Metodoa | Bidea                                 | Deskribapena                                 |
| ------- | ------------------------------------- | -------------------------------------------- |
| **GET** | `/`                                   | APIaren egoera eta 29 endpoint-en laburpena  |
| **GET** | `/api/pistak?barneEzabatuak=true`     | Pista guztiak zerrendatu                     |
| **GET** | `/api/pistak/{id}`                    | Pista zehatza ID bidez lortu                 |
| **GET** | `/api/erreserbak?barneEzabatuak=true` | Erreserba guztiak zerrendatu                 |
| **GET** | `/api/erreserbak/{id}`                | Erreserba zehatza ID bidez lortu             |
| **GET** | `/api/erreserbak/bazkideak`           | Soilik erreserba aktiboak dituzten bazkideak |
| **GET** | `/api/bazkideak?barneEzabatuak=true`  | Bazkide guztiak bono-saldoarekin             |
| **GET** | `/api/bazkideak/{id}`                 | Bazkide zehatza ID bidez lortu               |
| **GET** | `/api/bonoak?barneEzabatuak=true`     | Bono guztiak eta saio erabilgarriak          |
| **GET** | `/api/bonoak/{emaila}`                | Bazkide baten bonoa lortu emailaren arabera  |

### 2. POST (6 endpoint)

| Metodoa  | Bidea                          | Deskribapena                                               |
| -------- | ------------------------------ | ---------------------------------------------------------- |
| **POST** | `/api/pistak`                  | Pista berria sortu                                         |
| **POST** | `/api/erreserbak`              | Erreserba berria egin (Transakzioa + FOR UPDATE)           |
| **POST** | `/api/erreserbak/transakzioa`  | Erreserba transakzioarekin (`?itxaron=ms` simulazioarekin) |
| **POST** | `/api/bazkideak`               | Bazkide berria sortu (hasierako bonoarekin)                |
| **POST** | `/api/bonoak`                  | Bono berria erosi / sortu                                  |
| **POST** | `/api/bonoak/{emaila}/erabili` | Bonoaren saio 1 kontsumitu transakzioan                    |

### 3. PUT (5 endpoint)

| Metodoa | Bidea                          | Deskribapena                              |
| ------- | ------------------------------ | ----------------------------------------- |
| **PUT** | `/api/pistak/{id}/restore`     | Pista berreskuratu (Restore)              |
| **PUT** | `/api/erreserbak/{id}/restore` | Erreserba berreskuratu (Restore)          |
| **PUT** | `/api/bazkideak/{id}/restore`  | Bazkidea berreskuratu (Restore)           |
| **PUT** | `/api/bonoak/{emaila}/kargatu` | Bonoari saioak kargatu                    |
| **PUT** | `/api/bonoak/{emaila}/restore` | Desgaitutako bonoa berreskuratu (Restore) |

### 4. DELETE (9 endpoint)

| Metodoa    | Bidea                             | Deskribapena                                           |
| ---------- | --------------------------------- | ------------------------------------------------------ |
| **DELETE** | `/api/pistak/{id}?hard=false`     | Soft Delete (pista desgaitu eta erreserbak askatu)     |
| **DELETE** | `/api/pistak/{id}?hard=true`      | Hard Delete (pista eta bere erreserbak betiko ezabatu) |
| **DELETE** | `/api/erreserbak/{id}?hard=false` | Soft Delete (erreserba desgaitu eta pista askatu)      |
| **DELETE** | `/api/erreserbak/{id}?hard=true`  | Hard Delete (erreserba behin betiko ezabatu)           |
| **DELETE** | `/api/erreserbak/garbitu`         | Erreserba taula guztiz garbitu (probetarako)           |
| **DELETE** | `/api/bazkideak/{id}?hard=false`  | Soft Delete (bazkidea logikoki ezabatu)                |
| **DELETE** | `/api/bazkideak/{id}?hard=true`   | Hard Delete (bazkidea behin betiko ezabatu)            |
| **DELETE** | `/api/bonoak/{emaila}?hard=false` | Soft Delete (bonoa logikoki ezabatu)                   |
| **DELETE** | `/api/bonoak/{emaila}?hard=true`  | Hard Delete (bonoa behin betiko ezabatu)               |

---

## Proiektua Martxan Jartzeko

1. **Datu-basea kargatu:**
   MySQL-n `kiroldegia_db.sql` gidoia inportatu:

   ```bash
   mysql -u root -p < kiroldegia_db.sql
   ```

2. **APIa eraiki eta abiarazi:**

   ```bash
   dotnet build
   dotnet run --project API_kiroldegia_db/API_kiroldegia_db.csproj
   ```

   Nabigatzailean `http://localhost:5089/` helbidera joan APIaren egoera eta 26 endpoint-ak ikusteko.

3. **Postman probak:**
   Inportatu `Kiroldegia_API.postman_collection.json` fitxategia Postman aplikazioan. Eskaera guztiak metodoka antolatuta daude (`GET`, `POST`, `PUT`, `DELETE`).
   Gida osoa eta zehatza ikusteko: **[POSTMAN_GIDA.md](./POSTMAN_GIDA.md)**.
