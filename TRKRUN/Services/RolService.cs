// Services/RolService.cs
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TRKRUN.Context;
using TRKRUN.Identity;

namespace TRKRUN.Services
{
    public class RolService : IRolService
    {
        private readonly AppDbContext _ctx;

        public RolService(AppDbContext ctx)
        {
            _ctx = ctx;
        }
        
        public async Task<IEnumerable<Rol>> GetAllAsync()
            => await _ctx.Rol.ToListAsync();

        public async Task<Rol?> GetByIdAsync(int id)
            => await _ctx.Rol.FindAsync(id);

        public async Task<Rol> CreateAsync(Rol newRol)
        {
            _ctx.Rol.Add(newRol);
            await _ctx.SaveChangesAsync();
            return newRol;
        }

        public async Task<bool> UpdateAsync(int id, Rol updatedRol)
        {
            var existing = await _ctx.Rol.FindAsync(id);
            if (existing == null) return false;

            // Mapear cambios
            existing.name = updatedRol.name;
            // Si Rol tuviera más propiedades, aquí las actualizas

            await _ctx.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var rol = await _ctx.Rol.FindAsync(id);
            if (rol == null) return false;

            _ctx.Rol.Remove(rol);
            await _ctx.SaveChangesAsync();
            return true;
        }
    }
}
