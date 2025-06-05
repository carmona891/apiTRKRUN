using Microsoft.EntityFrameworkCore.Scaffolding.Metadata;

namespace TRKRUN.Identity
{
    public class Circuito
    {
        public int id { get; set; }
        public string name { get; set; } = null!;
        public string ubicacion { get; set; } = null!;

        // Un circuito puede alojar varios torneos
        public ICollection<Torneo> Torneos { get; set; } = new List<Torneo>();
    }
}
