namespace API_kiroldegia_db.Models
{
    /// <summary>
    /// Kiroldegiko pistaren domeinu-eredua (pista taula).
    /// </summary>
    public class Pista
    {
        public int Id { get; set; }
        public string Izena { get; set; } = string.Empty;
        public string Kirola { get; set; } = string.Empty;
        public bool Ezabatua { get; set; } = false;
        public DateTime? EzabaketaData { get; set; }
    }
}