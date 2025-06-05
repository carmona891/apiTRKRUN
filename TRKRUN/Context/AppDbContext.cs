using Microsoft.EntityFrameworkCore;
using TRKRUN.Identity;

namespace TRKRUN.Context
{
  public class AppDbContext : DbContext
  {
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // Opcional: Solo usar si no configuras la conexión en Program.cs/Startup.cs
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
      if (!optionsBuilder.IsConfigured)
      {
        var connectionString = "Server=trkrundb.mysql.database.azure.com;Database=trkrundb;User=mysqladmin@trkrundb;Password=usuarioo1?;SslMode=Required;Connection Timeout=60;Default Command Timeout=60;";

        optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString),
            mySqlOptions =>
            {
              mySqlOptions.EnableRetryOnFailure(
                          maxRetryCount: 5,
                          maxRetryDelay: TimeSpan.FromSeconds(30),
                          errorNumbersToAdd: null);
              mySqlOptions.CommandTimeout(60);
            });
      }
    }

    public DbSet<User> Users { get; set; }
    public DbSet<TRKRUN.Identity.Circuito> Circuito { get; set; } = default!;
    public DbSet<TRKRUN.Identity.Rol> Rol { get; set; } = default!;
    public DbSet<TRKRUN.Identity.Torneo> Torneo { get; set; } = default!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
      // User → Rol (muchos usuarios tienen un rol)
      modelBuilder.Entity<User>()
          .HasOne(u => u.Rol)
          .WithMany()                    // ← sin parámetro, rol no define Users
          .HasForeignKey(u => u.rol_id)
          .OnDelete(DeleteBehavior.Restrict);

      // User ↔ Torneo (muchos a muchos) usando tabla intermedia UserTorneo:
      modelBuilder.Entity<User>()
         .HasMany(u => u.Torneos)
         .WithMany(t => t.Participants)
         .UsingEntity<Dictionary<string, object>>(
             "UserTorneo",
             jt => jt
                 .HasOne<Torneo>()
                 .WithMany()
                 .HasForeignKey("torneo_id")
                 .OnDelete(DeleteBehavior.Cascade),
             jt => jt
                 .HasOne<User>()
                 .WithMany()
                 .HasForeignKey("user_id")
                 .OnDelete(DeleteBehavior.Cascade),
             jt =>
             {
               jt.HasKey("user_id", "torneo_id");
               jt.ToTable("UserTorneo");
             });

      // Relación 1:N Torneo <- Circuito
      modelBuilder.Entity<Torneo>()
          .HasOne(t => t.Circuito)
          .WithMany(c => c.Torneos)
          .HasForeignKey(t => t.circuito_id)
          .OnDelete(DeleteBehavior.Restrict);
    }
  }
}
