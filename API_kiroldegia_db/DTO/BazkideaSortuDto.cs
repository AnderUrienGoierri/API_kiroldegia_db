namespace API_kiroldegia_db.DTO;

/// <summary>
/// Bazkide berria sortzeko bidaltzen diren datuen DTOa (POST).
/// </summary>
public class BazkideaSortuDTO
{
    public string Izena { get; set; } = string.Empty;
    public string Abizena { get; set; } = string.Empty;
    public string Emaila { get; set; } = string.Empty;
    public int HasierakoBonoa { get; set; } = 0;
}

