# Postman Gida - Ordiziako Kiroldegia REST API

Gida honetan **Kiroldegiaren REST APIa** Postman tresnarekin nola probatu azaltzen da urratsez urrats.

Bilduma osoa metodoen arabera (`GET`, `POST`, `PUT`, `DELETE`) eta taula guztien gaineko eragiketen arabera dago antolatuta:
- **`pista`** (Tenisa 1, Tenisa 2, Padel 1, Frontoia...)
- **`bazkidea`** (Ane, Jon, Maialen, Iker, Nerea...)
- **`bonoak`** (Bazkideen saioak eta saldoak)
- **`erreserba`** (Pistak ordu jakinetan erreserbatzeko transakzioak)

---

## 1. Datu-Basearekiko Konexioa Egiaztatzea Postman Bidez

APIa martxan jarri ondoren, Postman erabiliko dugu datu-basearekin (MySQL) komunikazioa egokia dela eta datuak ondo irakurtzen direla ziurtatzeko:

1. Ireki **Postman** eta sortu eskaera berri bat (**New Request**).
2. Metodoa: hautatu **`GET`**.
3. Idatzi datu-baseko taulak irakurtzeko endpoint-a:
   ```text
   http://localhost:5089/api/pistak
   ```
   *(Edo `http://localhost:5089/api/bazkideak`)*
4. Egin klik **`Send`** botoian.

### Nola jakin datu-basearekin ondo konektatzen ari dela?
* **Konexio arrakastatsua (`200 OK`):** 
  * Behean `200 OK` egoera-kodea jasoko duzu eta erantzunaren gorputzean (**Body**) MySQL datu-basean gordeta dauden pistak agertuko dira JSON formatuan (`Tenisa 1`, `Tenisa 2`, `Padel 1`, `Frontoia`).
  * Horrek baieztatzen du APIa datu-basearekin ondo konektatu dela eta kontsultak arazorik gabe exekutatzen ari direla.
* **Konexio okerra (`500 Internal Server Error`):**
  * Datu-baseko zerbitzaria (MySQL) itzalita badago, ataka okerra bada edo konexio-kateko kredentzialak gaizki badaude, APIak zerbitzari-errore bat (`500`) itzuliko du datu-baseko konexio-akatsekin.

---

## 2. Metodorik azkarrena: Postman Bilduma (Collection) Inportatzea

Postman-en eskaera guztiak karpetatan txukun sailkatuta edukitzeko, inportatu fitxategi hau:
- [`Kiroldegia_API.postman_collection.json`](./Kiroldegia_API.postman_collection.json)

### Nola inportatu Postman-en:
1. Ireki **Postman**.
2. Goiko ezkerrean klikatu **`Import`** botoian.
3. Arrastatu edo aukeratu `Kiroldegia_API.postman_collection.json` fitxategia.
4. Ezkerreko panelean **"Ordiziako Kiroldegia API"** bilduma agertuko da, 4 karpeta nagusitan antolatuta:
   - 📁 **1. GET Eskaerak (Irakurketa)** (10 eskaera)
   - 📁 **2. POST Eskaerak (Sortzea & Transakzioak)** (6 eskaera)
   - 📁 **3. PUT Eskaerak (Eguneratu & Berreskuratu)** (4 eskaera)
   - 📁 **4. DELETE Eskaerak (Soft & Hard Delete)** (7 eskaera)

---

## 3. Bildumako Eskaera Guztien Taula Sailkatua

APIak `http://localhost:5089` helbidean entzuten du (eta garapenean `http://localhost:5262`):

### 📁 1. GET Eskaerak (Irakurketa)
| # | Eskaera | Bidea (URL) | Deskribapena |
|---|---|---|---|
| 00 | **GET** | `/` | APIaren egoera orokorra eta endpoint zerrenda |
| 01 | **GET** | `/api/pistak?barneEzabatuak=true` | Pista guztiak zerrendatu |
| 02 | **GET** | `/api/pistak/{id}` | Pista bakarra ID bidez lortu |
| 03 | **GET** | `/api/bazkideak?barneEzabatuak=true` | Bazkide guztiak bono-saldoarekin |
| 04 | **GET** | `/api/bazkideak/{id}` | Bazkide bakarra ID bidez lortu |
| 05 | **GET** | `/api/bonoak` | Bono guztien zerrenda (saldoak) |
| 06 | **GET** | `/api/bonoak/{emaila}` | Bazkide baten bonoa lortu email bidez |
| 07 | **GET** | `/api/erreserbak?barneEzabatuak=true` | Erreserba guztiak zerrendatu |
| 08 | **GET** | `/api/erreserbak/{id}` | Erreserba zehatz bat ID bidez lortu |
| 09 | **GET** | `/api/erreserbak/bazkideak` | Soilik erreserbaren bat duten bazkideak |

### 📁 2. POST Eskaerak (Sortzea & Transakzioak)
| # | Eskaera | Bidea (URL) | Body (JSON) | Deskribapena |
|---|---|---|---|---|
| 01 | **POST** | `/api/pistak` | `{"izena": "Padel 2", "kirola": "Padela"}` | Pista berria txertatu |
| 02 | **POST** | `/api/bazkideak` | `{"izena": "Mikel", "abizena": "Zubizarreta", "emaila": "mikel@goierri.eus", "hasierakoBonoa": 5}` | Bazkide berria eta hasierako bonoa |
| 03 | **POST** | `/api/bonoak` | `{"emaila": "mikel@goierri.eus", "saioak": 10}` | Bono berria erosi/sortu |
| 04 | **POST** | `/api/bonoak/{emaila}/erabili` | *(Hutsik)* | Bonoaren saio 1 kontsumitu transakzioan |
| 05 | **POST** | `/api/erreserbak` | `{"pistaId": 1, "bazkideaId": 1, "data": "2026-10-28", "ordua": "18:00:00", "erabiliBonoa": true}` | **Erreserba egin transakzioan** (Bonoa deskontatuz eta FOR UPDATE bidez) |
| 06 | **POST** | `/api/erreserbak?itxaron=5000` | `{"pistaId": 2, "bazkideaId": 2, ... "itxaron": 5000}` | **Konkurrentzia simulazioa:** 5 segundoko blokeoa probatzeko |

### 📁 3. PUT Eskaerak (Eguneratu & Berreskuratu)
| # | Eskaera | Bidea (URL) | Body (JSON) | Deskribapena |
|---|---|---|---|---|
| 01 | **PUT** | `/api/bonoak/{emaila}/kargatu` | `{"saioak": 5}` | Bonoari saioak gehitu transakzioan |
| 02 | **PUT** | `/api/pistak/{id}/restore` | *(Hutsik)* | Soft-Delete egindako pista berreskuratu |
| 03 | **PUT** | `/api/bazkideak/{id}/restore` | *(Hutsik)* | Soft-Delete egindako bazkidea berreskuratu |
| 04 | **PUT** | `/api/erreserbak/{id}/restore` | *(Hutsik)* | Soft-Delete egindako erreserba berreskuratu |

### 📁 4. DELETE Eskaerak (Soft & Hard Delete)
| # | Eskaera | Bidea (URL) | Deskribapena |
|---|---|---|---|
| 01 | **DELETE** | `/api/pistak/{id}?hard=false` | **Pista Soft Delete:** Pista eta bere erreserbak logikoki desgaitu |
| 02 | **DELETE** | `/api/pistak/{id}?hard=true` | **Pista Hard Delete:** Pista eta erreserbak behin betiko ezabatu |
| 03 | **DELETE** | `/api/bazkideak/{id}?hard=false` | **Bazkidea Soft Delete:** Bazkidea logikoki ezabatu |
| 04 | **DELETE** | `/api/bazkideak/{id}?hard=true` | **Bazkidea Hard Delete:** Bazkidea fisikoki ezabatu (erreserbarik ez badu) |
| 05 | **DELETE** | `/api/erreserbak/{id}?hard=false` | **Erreserba Soft Delete:** Erreserba logikoki ezabatu (pista libre uzten du) |
| 06 | **DELETE** | `/api/erreserbak/{id}?hard=true` | **Erreserba Hard Delete:** Erreserba behin betiko ezabatu |
| 07 | **DELETE** | `/api/erreserbak/garbitu` | **Garbitu Taula:** Erreserba guztiak garbitu probak hutsetik hasteko |

---

## 4. Probatzeko Agertoki Garrantzitsuenak

### A) Transakzio atomikoa eta bonoaren deskontua
* Egin `POST 05. Erreserba Sortu`: Ane Etxeberriaren saio 1 kenduko da (adib. 10etik 9ra) eta erreserba sortuko da.
* Jarraian egin berriro klik **`Send`** botoian:
  * APIak **`409 Conflict`** itzuliko du: pista eta ordu hori jada okupatuta dagoela ohartaraziz.

### B) Konkurrentzia eta Blokeoa (Lock Wait) frogatzea
* Bidali `POST 06. Erreserba Sortu (Blokeo simulazioa)`: eskaerak 5 segundoz mantenduko du transakzioa irekita.
* Tarte horretan, beste fitxa batetik saiatu ordu bera erreserbatzen: beste eskaerak itxaron egingo du lehenak bukatu arte edo gatazka emango du.
