namespace API_kiroldegia_db.DTO;

public class BonoDTO
{
    public string Emaila { get; set; } = string.Empty;
    public int Saioak { get; set; }
    public bool Ezabatua { get; set; }
    public DateTime? EzabaketaData { get; set; }
}

public class BonoKargatuDTO
{
    public int Saioak { get; set; }
}
