namespace API_kiroldegia_db.DTO;

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

public class BazkideaSortuDTO
{
    public string Izena { get; set; } = string.Empty;
    public string Abizena { get; set; } = string.Empty;
    public string Emaila { get; set; } = string.Empty;
    public int HasierakoBonoa { get; set; } = 0;
}