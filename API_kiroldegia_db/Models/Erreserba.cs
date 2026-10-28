namespace API_kiroldegia_db.Models
{
    /// <summary>
    /// Kiroldegiko erreserbaren domeinu-eredua (erreserba taula).
    /// </summary>
    public class Erreserba
    {
        public int Id { get; set; }
        public int PistaId { get; set; }
        public string Lekua { get; set; } = string.Empty;
        public int BazkideaId { get; set; }
        public string Emaila { get; set; } = string.Empty;
        public DateOnly Data { get; set; }
        public TimeOnly Ordua { get; set; }
        public bool Ezabatua { get; set; } = false;
        public DateTime? EzabaketaData { get; set; }
        public DateTime Sortua { get; set; } = DateTime.Now;
    }
}