namespace API_kiroldegia_db.Models
{
    /// <summary>
    /// Kiroldegiko bazkidearen domeinu-eredua (bazkidea taula).
    /// </summary>
    public class Bazkidea
    {
        public int Id { get; set; }
        public string Izena { get; set; } = string.Empty;
        public string Abizena { get; set; } = string.Empty;
        public string? Emaila { get; set; }
        public bool Ezabatua { get; set; } = false;
        public DateTime? EzabaketaData { get; set; }
    }
}