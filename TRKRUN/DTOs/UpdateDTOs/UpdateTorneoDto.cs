namespace TRKRUN.DTOs.UpdateDTOs
{
    public class UpdateTorneoDto
    {
        public int? Id { get; set; }
        public string Name { get; set; } = null!;
        public DateTime Fecha { get; set; }
        public decimal Premio { get; set; }
        public int? CircuitoId { get; set; }
        public int participantes { get; set; }

        //public List<UserDto> Participants { get; set; } = new();
    }
}
