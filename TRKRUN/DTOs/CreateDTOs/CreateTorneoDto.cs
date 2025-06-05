namespace TRKRUN.DTOs.CreateDTOs
{
    public class CreateTorneoDto
    {
        public string Name { get; set; } = null!;
        public DateTime Fecha { get; set; }
        public int Premio { get; set; }
        public int CircuitoId { get; set; }
    }
}
