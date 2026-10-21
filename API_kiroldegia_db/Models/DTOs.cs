namespace API_kiroldegia_db.Models;

public class PistaDTO
{
    public int Id { get; set; }
    public string Izena { get; set; } = string.Empty;
    public string Kirola { get; set; } = string.Empty;
    public bool Ezabatua { get; set; }
    public DateTime? EzabaketaData { get; set; }
}

public class PistaSortuDTO
{
    public string Izena { get; set; } = string.Empty;
    public string Kirola { get; set; } = string.Empty;
}

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

public class BonoDTO
{
    public string Emaila { get; set; } = string.Empty;
    public int Saioak { get; set; }
}

public class BonoKargatuDTO
{
    public int Saioak { get; set; }
}

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

public class ErreserbaSortuDTO
{
    public int PistaId { get; set; }
    public string? Lekua { get; set; }
    public int BazkideaId { get; set; }
    public string? Emaila { get; set; }
    public string Data { get; set; } = string.Empty;
    public string Ordua { get; set; } = string.Empty;
    public bool ErabiliBonoa { get; set; } = false;
    public int Itxaron { get; set; } = 0; // ms (adibidez 5000ms konkurrentzia eta blokeoa simulatzeko)
}

