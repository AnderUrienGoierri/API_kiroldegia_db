using System.ComponentModel.DataAnnotations;

namespace API_kiroldegia_db.DTO
{
    public class ErreserbaSortuDto
    {
        public int PistaId { get; set; }
        public string? Lekua { get; set; }
        public int BazkideaId { get; set; }
        public string? Emaila { get; set; }

        [Required]
        public string Data { get; set; } = string.Empty;

        [Required]
        public string Ordua { get; set; } = string.Empty;

        public bool ErabiliBonoa { get; set; } = false;
        public int Itxaron { get; set; } = 0;
    }
}
