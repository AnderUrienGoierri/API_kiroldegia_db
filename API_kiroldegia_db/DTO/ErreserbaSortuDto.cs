using System.ComponentModel.DataAnnotations;

namespace API_kiroldegia_db.DTO;

/// <summary>
/// POST bidez erreserba berria sortzean jasotzen diren datuak (DTO).
/// Pista eta Bazkidea bai ID bidez bai izen/email bidez zehaztea onartzen du.
/// DTO huts eta garbia (POCO), datuak garraiatzeko soilik.
/// </summary>
public class ErreserbaSortuDto
{
    public string? Lekua { get; set; }
    public int? PistaId { get; set; }

    [Required]
    public DateOnly Data { get; set; }

    [Required]
    public TimeOnly Ordua { get; set; }

    public string? Emaila { get; set; }
    public int? BazkideaId { get; set; }

    public bool ErabiliBonoa { get; set; } = false;
    public int Itxaron { get; set; } = 0; // ms (konkurrentzia eta blokeoa simulatzeko)
}
