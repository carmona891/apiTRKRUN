namespace TRKRUN.DTOs
{
    public class TorneoDto
    {
        public int? Id { get; set; }
        public string Name { get; set; } = null!;
        public DateTime Fecha { get; set; }
        public decimal Premio { get; set; }
        public int? CircuitoId { get; set; }
        public int participantes { get; set; }

        public List<UserForTorneoDto> Participants { get; set; } = new();
    }
}
