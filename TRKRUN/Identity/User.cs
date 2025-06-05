namespace TRKRUN.Identity
{
    public class User
    {
        public int? id { get; set; }
        public string name { get; set; }
        public string email { get; set; }
        public string password { get; set; }
        //public int level { get; set; }       // ¿Lo usas o sólo rol_id?
        public int? torneo_id { get; set; }
        public int rol_id { get; set; }
        public int points { get; set; }

        // — Navegaciones —
        public Rol Rol { get; set; }
        // Relación N:N con torneos
        public ICollection<Torneo> Torneos { get; set; } = new List<Torneo>();
    }
}
