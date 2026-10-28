namespace API_kiroldegia_db.DTO;

public class ErreserbaDTO
{
    public int Id { get; set; }
    public int PistaId { get; set; }
    public string PistaIzena { get; set; } = string.Empty;
    public string Kirola { get; set; } = string.Empty;
    public int BazkideaId { get; set; }
    public string BazkideaIzena { get; set; } = string.Empty;
    public string? BazkideaEmaila { get; set; }
    public string Data { get; set; } = string.Empty;
    public string Ordua { get; set; } = string.Empty;
    public bool Ezabatua { get; set; }
    public DateTime? EzabaketaData { get; set; }
    public DateTime Sortua { get; set; }
}