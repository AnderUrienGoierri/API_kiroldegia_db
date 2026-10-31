namespace API_kiroldegia_db.Models
{
    /// <summary>
    /// Kiroldegiko bonoen domeinu-eredua (bonoak taula).
    /// </summary>
    public class Bonoa
    {
        public string Emaila { get; set; } = string.Empty;
        public int Saioak { get; set; }
        public bool Ezabatua { get; set; }
        public DateTime? EzabaketaData { get; set; }
    }
}
