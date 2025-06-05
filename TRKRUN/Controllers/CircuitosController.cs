using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TRKRUN.Context;
using TRKRUN.DTOs.CreateDTOs;   
using TRKRUN.DTOs.UpdateDTOs;
using TRKRUN.Identity;

namespace TRKRUN.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CircuitosController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CircuitosController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Circuitos
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CreateCircuitoDto>>> GetCircuito()
        {
            var lista = await _context.Circuito
                .Select(c => new CreateCircuitoDto
                {
                    Id = c.id,
                    Name = c.name,
                    Ubicacion = c.ubicacion
                })
                .ToListAsync();

            return Ok(lista);
        }

        // GET: api/Circuitos/5
        [HttpGet("{id}")]
        public async Task<ActionResult<CreateCircuitoDto>> GetCircuito(int id)
        {
            var c = await _context.Circuito
                .Where(x => x.id == id)
                .Select(cir => new CreateCircuitoDto
                {
                    Id = cir.id,
                    Name = cir.name,
                    Ubicacion = cir.ubicacion
                })
                .SingleOrDefaultAsync();

            if (c == null)
                return NotFound();

            return Ok(c);
        }

        // PUT: api/Circuitos/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutCircuito(int id, UpdateCircuitoDto dto)
        {
            // 1) Verificamos que el id de la ruta coincida con el id del DTO
            if (id != dto.Id)
            {
                return BadRequest($"El Id de la URL ({id}) no coincide con el Id del DTO ({dto.Id}).");
            }

            // 2) Buscamos la entidad en la base de datos
            var circuitoExistente = await _context.Circuito.FindAsync(id);
            if (circuitoExistente == null)
            {
                return NotFound(); // Si no existe, devolvemos 404
            }

            // 3) Actualizamos solo las propiedades que vienen en el DTO
            circuitoExistente.name = dto.Name;
            circuitoExistente.ubicacion = dto.Ubicacion;

            // 4) Marcamos la entidad como modificada y guardamos cambios
            _context.Entry(circuitoExistente).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                // En caso de concurrencia, comprobamos de nuevo si existe
                if (!_context.Circuito.Any(e => e.id == id))
                    return NotFound();
                else
                    throw; // Si fue otro error de concurrencia, re-lanzamos
            }

            // 5) Devolvemos 204 No Content (o podrías devolver el DTO actualizado si prefieres)
            return NoContent();
        }

        // NUEVO método POST: crea usando CreateCircuitoDto en lugar de la entidad completa
        // POST: api/Circuitos
        [HttpPost]
        public async Task<ActionResult<CreateCircuitoDto>> PostCircuito(CreateCircuitoDto dto)
        {
            // 1) Mapeo manual de DTO hacia entidad
            var circuitoEntidad = new Circuito
            {
                // No asignamos dto.Id aquí, ya que lo genera la BD
                name = dto.Name,
                ubicacion = dto.Ubicacion
            };

            // 2) Agregar a contexto y guardar en BD
            _context.Circuito.Add(circuitoEntidad);
            await _context.SaveChangesAsync();

            // 3) Asignar el Id que la BD generó al DTO
            dto.Id = circuitoEntidad.id;

            // 4) Devolver 201 Created apuntando al GET por ese id recién creado
            return CreatedAtAction(
                nameof(GetCircuito),
                new { id = dto.Id },
                dto
            );
        }

        // DELETE: api/Circuitos/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCircuito(int id)
        {
            var circuito = await _context.Circuito.FindAsync(id);
            if (circuito == null)
                return NotFound();

            _context.Circuito.Remove(circuito);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool CircuitoExists(int id)
        {
            return _context.Circuito.Any(e => e.id == id);
        }
    }
}
