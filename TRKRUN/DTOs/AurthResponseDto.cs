namespace TRKRUN.DTOs
{
    public class AuthResponseDto
    {
        public string Token { get; set; } = null!;
        public DateTime Expiration { get; set; }

        // Puedes agregar más info si quieres (por ejemplo, el rol o el nombre de usuario)
        //public int UserId { get; set; }
        //public string Email { get; set; } = null!;
        //public string Name { get; set; } = null!;
        //public int RolId { get; set; }
    }
}
