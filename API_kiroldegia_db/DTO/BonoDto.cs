namespace API_kiroldegia_db.DTO;

/// <summary>
/// Bono baten informazioa irakurtzeko DTOa (GET).
/// </summary>
public class BonoDTO
{
    public string Emaila { get; set; } = string.Empty;
    public int Saioak { get; set; }
    public bool Ezabatua { get; set; }
    public DateTime? EzabaketaData { get; set; }
}

/// <summary>
/// Bonoari saioak kargatzeko bidaltzen den DTOa (PUT).
/// </summary>
public class BonoKargatuDTO
{
    public int Saioak { get; set; }
}
