namespace TRKRUN.Identity
{
    public class Torneo
    {
        public int id { get; set; }
        public string name { get; set; }
        public int circuito_id { get; set; }
        public DateTime fecha { get; set; }
        public int premio { get; set; }
        public int participantes { get; set; }

        // — Navegaciones —
        public Circuito Circuito { get; set; }
        // Relación N:N con usuarios
        public ICollection<User> Participants { get; set; } = new List<User>();
    }
}
