namespace TRKRUN.DTOs
{
    public class RegisterUserDto
    {
        //public int? Id { get; set; }
        public string Name { get; set; } = null!;
        public string Email { get; set; } = null!;
        public int rol_id { get; set; }
        public string Password { get; set; }


        //public RolDto Rol { get; set; } = null!;
    }
}
