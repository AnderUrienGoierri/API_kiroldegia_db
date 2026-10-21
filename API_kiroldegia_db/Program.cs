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
    endpoints = new[]
    {
        "GET    /api/pistak",
        "GET    /api/erreserbak",
        "GET    /api/bazkideak (Bazkide guztiak)",
        "GET    /api/erreserbak/bazkideak (Soilik erreserba duten bazkideak)",
        "GET    /api/bonoak",
        "POST   /api/erreserbak",
        "DELETE /api/erreserbak/{id}?hard=false (Soft Delete)",
        "DELETE /api/erreserbak/{id}?hard=true  (Hard Delete)",
        "PUT    /api/erreserbak/{id}/restore",
        "PUT    /api/bonoak/{emaila}/kargatu",
        "DELETE /api/erreserbak/garbitu"
    }
}));

app.MapControllers();

app.Run();
