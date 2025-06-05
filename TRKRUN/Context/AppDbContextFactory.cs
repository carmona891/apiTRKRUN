using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;

namespace TRKRUN.Context
{
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            // Tu cadena de conexión real, aunque no vaya a usarse aquí:
            var connectionString = "server=localhost;database=TRKRUN;user=root;password=usuario;";

            // FIJA aquí la versión de tu servidor MySQL (por ejemplo 8.0.26):
            var serverVersion = new MySqlServerVersion(new Version(8, 0, 26));

            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder.UseMySql(connectionString, serverVersion);

            return new AppDbContext(optionsBuilder.Options);
        }
    }
}
