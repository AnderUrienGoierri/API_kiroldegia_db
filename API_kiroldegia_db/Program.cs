using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// CORS baimentzea edozein jatorritatik (web frontend aplikazioarentzat)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("AllowAll");

// Web aplikazioaren fitxategi estatikoak (aukeran /app bidean zerbitzatzeko)
var webAppPath = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, "..", "..", "kiroldegia_erreserbak_web_app"));

if (Directory.Exists(webAppPath))
{
    app.UseFileServer(new FileServerOptions
    {
        FileProvider = new PhysicalFileProvider(webAppPath),
        RequestPath = "/app",
        EnableDefaultFiles = true
    });
}

app.UseAuthorization();

app.MapGet("/", () => Results.Ok(new
{
    mezua = "Ordiziako Kiroldegia REST API martxan dago!",
    webAplikazioa = "http://localhost/kiroldegia/",
    endpointsGuztira = 29,
    laburpena = new
    {
        GET = 9,
        POST = 6,
        PUT = 5,
        DELETE = 9
    },
    endpoints = new[]
    {
        // ==========================================
        // 1. GET (9 endpoint)
        // ==========================================
        "GET    /api/pistak (?barneEzabatuak=true/false) - Pistak zerrendatu",
        "GET    /api/pistak/{id} - Pista bakarra ID bidez lortu",
        "GET    /api/erreserbak (?barneEzabatuak=true/false) - Erreserba guztiak zerrendatu",
        "GET    /api/erreserbak/{id} - Erreserba bakarra ID bidez lortu",
        "GET    /api/erreserbak/bazkideak (?barneEzabatuak=true/false) - Soilik erreserbadun bazkideak lortu",
        "GET    /api/bazkideak (?barneEzabatuak=true/false) - Bazkide guztiak lortu (bono saldoarekin)",
        "GET    /api/bazkideak/{id} - Bazkide bakarra ID bidez lortu",
        "GET    /api/bonoak (?barneEzabatuak=true/false) - Bono guztiak eta saio erabilgarriak",
        "GET    /api/bonoak/{emaila} - Email zehatz baten bonoa lortu",

        // ==========================================
        // 2. POST (6 endpoint)
        // ==========================================
        "POST   /api/pistak - Pista berria sortu",
        "POST   /api/erreserbak (?itxaron=ms) - Erreserba berria egin transakzioan",
        "POST   /api/erreserbak/transakzioa (?itxaron=ms) - Erreserba transakzioarekin FOR UPDATE bidez",
        "POST   /api/bazkideak - Bazkide berria sortu",
        "POST   /api/bonoak - Bono berria sortu edo erosi",
        "POST   /api/bonoak/{emaila}/erabili - Bonoaren saio 1 kontsumitu transakzioan",

        // ==========================================
        // 3. PUT (5 endpoint)
        // ==========================================
        "PUT    /api/pistak/{id}/restore - Desgaitutako pista berreskuratu (Restore)",
        "PUT    /api/erreserbak/{id}/restore - Soft-Delete egindako erreserba berreskuratu (Restore)",
        "PUT    /api/bazkideak/{id}/restore - Desgaitutako bazkidea berreskuratu (Restore)",
        "PUT    /api/bonoak/{emaila}/kargatu - Bonoari saioak kargatu transakzioan",
        "PUT    /api/bonoak/{emaila}/restore - Desgaitutako bonoa berreskuratu (Restore)",

        // ==========================================
        // 4. DELETE (9 endpoint)
        // ==========================================
        "DELETE /api/pistak/{id}?hard=false - Pista Soft Delete (erlazionatutako erreserbak desgaitu)",
        "DELETE /api/pistak/{id}?hard=true - Pista Hard Delete (pista eta erreserbak behin betiko ezabatu)",
        "DELETE /api/erreserbak/{id}?hard=false - Erreserba Soft Delete (pista askatu, ezabatua=true)",
        "DELETE /api/erreserbak/{id}?hard=true - Erreserba Hard Delete (behin betiko ezabatu)",
        "DELETE /api/erreserbak/garbitu - Erreserba taula guztiz hustu (probetarako)",
        "DELETE /api/bazkideak/{id}?hard=false - Bazkidea Soft Delete",
        "DELETE /api/bazkideak/{id}?hard=true - Bazkidea Hard Delete",
        "DELETE /api/bonoak/{emaila}?hard=false - Bonoa Soft Delete",
        "DELETE /api/bonoak/{emaila}?hard=true - Bonoa Hard Delete"
    }
}));

app.MapControllers();

app.Run();