// TorneoService.cs
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TRKRUN.Context;
using TRKRUN.Identity;

namespace TRKRUN.Services
{
    public class TorneoService : ITorneoService
    {
        private readonly AppDbContext _ctx;

        public TorneoService(AppDbContext ctx)
        {
            _ctx = ctx;
        }

        public async Task<IEnumerable<Torneo>> GetAllAsync()
            => await _ctx.Torneo.ToListAsync();

        public async Task<Torneo?> GetByIdAsync(int id)
            => await _ctx.Torneo.FindAsync(id);

        public async Task<Torneo> CreateAsync(Torneo newTorneo)
        {
            _ctx.Torneo.Add(newTorneo);
            await _ctx.SaveChangesAsync();
            return newTorneo;
        }

        public async Task<bool> UpdateAsync(int id, Torneo updatedTorneo)
        {
            var existing = await _ctx.Torneo.FindAsync(id);
            if (existing == null) return false;

            // Mapea aquí los cambios según tu modelo. Ejemplos:
            // existing.Nombre = updatedTorneo.Nombre;
            // existing.FechaInicio = updatedTorneo.FechaInicio;
            // existing.FechaFin = updatedTorneo.FechaFin;
            // existing.CircuitoId = updatedTorneo.CircuitoId;
            // … añade/ajusta las propiedades que necesites

            await _ctx.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var torneo = await _ctx.Torneo.FindAsync(id);
            if (torneo == null) return false;

            _ctx.Torneo.Remove(torneo);
            await _ctx.SaveChangesAsync();
            return true;
        }
    }
}
