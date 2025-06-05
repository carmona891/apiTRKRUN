// CircuitoService.cs
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TRKRUN.Context;
using TRKRUN.Identity;

namespace TRKRUN.Services
{
    public class CircuitoService : ICircuitoService
    {
        private readonly AppDbContext _ctx;

        public CircuitoService(AppDbContext ctx)
        {
            _ctx = ctx;
        }

        public async Task<IEnumerable<Circuito>> GetAllAsync()
            => await _ctx.Circuito.ToListAsync();

        public async Task<Circuito?> GetByIdAsync(int id)
            => await _ctx.Circuito.FindAsync(id);

        public async Task<Circuito> CreateAsync(Circuito newCircuito)
        {
            _ctx.Circuito.Add(newCircuito);
            await _ctx.SaveChangesAsync();
            return newCircuito;
        }

        public async Task<bool> UpdateAsync(int id, Circuito updatedCircuito)
        {
            var existing = await _ctx.Circuito.FindAsync(id);
            if (existing == null) return false;

            // Mapea aquí los cambios. Ejemplos:
            existing.name = updatedCircuito.name;
            existing.ubicacion = updatedCircuito.ubicacion;
            existing.Torneos = updatedCircuito.Torneos; 
            // existing.Longitud = updatedCircuito.Longitud;
            // existing.Descripcion = updatedCircuito.Descripcion;
            // … añade/ajusta según tu modelo

            await _ctx.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var circuito = await _ctx.Circuito.FindAsync(id);
            if (circuito == null) return false;

            _ctx.Circuito.Remove(circuito);
            await _ctx.SaveChangesAsync();
            return true;
        }
    }
}
