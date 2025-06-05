namespace TRKRUN.DTOs.UpdateDTOs
{
    public class UpdateUserDto
    {
        //public int? Id { get; set; }
        public string Name { get; set; } = null!;
        public string Email { get; set; } = null!;
        public int rol_id { get; set; }
        public string Password { get; set; }
        public int points { get; set; }

        // Ahora exponemos solamente los TorneoDto en los que está inscrito el usuario
        public ICollection<TorneoDto> TorneosInscritos { get; set; } = new List<TorneoDto>();
    }
}
