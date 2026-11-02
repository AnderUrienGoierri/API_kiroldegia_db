namespace API_kiroldegia_db.DTO;

/// <summary>
/// Pista baten datuak irakurtzeko DTOa (GET).
/// </summary>
public class PistaDTO
{
    public int Id { get; set; }
    public string Izena { get; set; } = string.Empty;
    public string Kirola { get; set; } = string.Empty;
    public bool Ezabatua { get; set; }
    public DateTime? EzabaketaData { get; set; }
}

/// <summary>
/// Pista berria sortzeko bidaltzen diren datuen DTOa (POST).
/// </summary>
public class PistaSortuDTO
{
    public string Izena { get; set; } = string.Empty;
    public string Kirola { get; set; } = string.Empty;
}
