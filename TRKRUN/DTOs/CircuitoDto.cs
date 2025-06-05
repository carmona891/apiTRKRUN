namespace TRKRUN.DTOs
{
    public class CreateCircuitoDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string Ubicacion { get; set; } = null!;
        // Si en algún caso quieres devolver solo IDs de torneos:
        //public List<int> TorneosIds { get; set; } = new();
    }
}
