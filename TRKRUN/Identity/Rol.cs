namespace TRKRUN.Identity
{
    public class Rol
    {
        public int id { get; set; }
        public string name { get; set; }
        public int level { get; set; }

        // (opcional) colección de usuarios
        //public ICollection<User> Users { get; set; }
    }
}
