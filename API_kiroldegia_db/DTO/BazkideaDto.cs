namespace API_kiroldegia_db.DTO;

/// <summary>
/// Bazkide baten informazioa irakurtzeko DTOa (GET).
/// </summary>
public class BazkideaDTO
{
    public int Id { get; set; }
    public string Izena { get; set; } = string.Empty;
    public string Abizena { get; set; } = string.Empty;
    public string? Emaila { get; set; }
    public int BonoSaioak { get; set; } = 0;
    public bool Ezabatua { get; set; }
    public DateTime? EzabaketaData { get; set; }
}
