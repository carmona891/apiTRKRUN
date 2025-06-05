using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TRKRUN.Context;
using TRKRUN.Identity;
using TRKRUN.DTOs;               // Para TorneoDto (si lo usas en GET)
using TRKRUN.DTOs.CreateDTOs;    // Para CreateTorneoDto (POST)
using TRKRUN.DTOs.UpdateDTOs;    // Para UpdateTorneoDto (PUT)

namespace TRKRUN.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TorneosController : ControllerBase
    {
        private readonly AppDbContext _context;
        public TorneosController(AppDbContext context) => _context = context;

        // GET: api/Torneos
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TorneoDto>>> GetTorneos()
        {
            var torneos = await _context.Torneo
                .Include(t => t.Participants)
                    .ThenInclude(u => u.Rol)
                .ToListAsync();

            var result = torneos.Select(t => new TorneoDto
            {
                Id = t.id,
                Name = t.name,
                Fecha = t.fecha,
                Premio = t.premio,
                CircuitoId = t.circuito_id,
                participantes = t.participantes,
                // Si quisieras devolver Participants, descomenta y mapea aquí:
                Participants = t.Participants.Select(u => new UserForTorneoDto
                {
                    Id = u.id,
                    Name = u.name,
                    Email = u.email
                }).ToList()
            }).ToList();

            return Ok(result);
        }

        // GET: api/Torneos/5
        [HttpGet("{id}")]
        public async Task<ActionResult<TorneoDto>> GetTorneo(int id)
        {
            var torneo = await _context.Torneo
                .Include(t => t.Participants)
                    .ThenInclude(u => u.Rol)
                .FirstOrDefaultAsync(t => t.id == id);

            if (torneo == null)
                return NotFound();

            var dto = new TorneoDto
            {
                Id = torneo.id,
                Name = torneo.name,
                Fecha = torneo.fecha,
                Premio = torneo.premio,
                CircuitoId = torneo.circuito_id
                // Participants = torneo.Participants.Select(u => new UserDto { … }).ToList()
            };

            return Ok(dto);
        }

        // ----------------------------------------------------
        // PUT: api/Torneos/5
        // Ahora recibe UpdateTorneoDto en lugar de la entidad Torneo completa
        [HttpPut("{id}")]
        public async Task<IActionResult> PutTorneo(int id, UpdateTorneoDto dto)
        {
            // 1) Validar que el id de la ruta coincida con dto.Id
            if (dto.Id == null || id != dto.Id.Value)
            {
                return BadRequest("El ID de la ruta debe coincidir con el ID del DTO.");
            }

            // 2) Recuperar el torneo existente (puedes incluir relaciones si necesitas validarlas)
            var torneo = await _context.Torneo
                .Include(t => t.Participants) // solo si luego vas a modificar participants; si no, puedes omitirlo
                .FirstOrDefaultAsync(t => t.id == id);

            if (torneo == null)
            {
                return NotFound($"No existe un Torneo con id {id}.");
            }

            // 3) Si van a poder cambiar el Circuito, valida que exista
            if (dto.CircuitoId.HasValue)
            {
                var circuito = await _context.Circuito.FindAsync(dto.CircuitoId.Value);
                if (circuito == null)
                {
                    return BadRequest($"No existe un Circuito con id {dto.CircuitoId.Value}.");
                }
                torneo.circuito_id = dto.CircuitoId.Value;
            }
            else
            {
                // Si dto.CircuitoId viene null y no permites desasignar, podrías:
                // return BadRequest("CircuitoId no puede ser null al actualizar.");
                // O bien dejarlo igual: torneo.circuito_id = torneo.circuito_id;
            }

            // 4) Actualizar los demás campos
            torneo.name = dto.Name;
            torneo.fecha = dto.Fecha;
            // Si en la entidad Torneo el campo premio es int, casteamos:
            torneo.premio = (int)dto.Premio;

            // 5) Marcar entidad como modificada y guardar cambios
            _context.Entry(torneo).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TorneoExists(id))
                    return NotFound();
                else
                    throw;
            }

            return NoContent();
        }

        // POST: api/Torneos
        [HttpPost]
        public async Task<ActionResult<TorneoDto>> PostTorneo([FromBody] CreateTorneoDto dto)
        {
            // 1) Comprobar que el Circuito existe
            var circuito = await _context.Circuito.FindAsync(dto.CircuitoId);
            if (circuito == null)
                return BadRequest($"No existe un Circuito con id {dto.CircuitoId}.");

            // 2) Mapear a entidad Torneo
            var nuevoTorneo = new Torneo
            {
                name = dto.Name,
                fecha = dto.Fecha,
                premio = (int)dto.Premio,
                circuito_id = dto.CircuitoId
            };

            // 3) Guardar en BD
            _context.Torneo.Add(nuevoTorneo);
            await _context.SaveChangesAsync();

            // 4) Devolver TorneoDto con el nuevo ID
            var resultDto = new TorneoDto
            {
                Id = nuevoTorneo.id,
                Name = nuevoTorneo.name,
                Fecha = nuevoTorneo.fecha,
                Premio = nuevoTorneo.premio,
                CircuitoId = nuevoTorneo.circuito_id
            };

            return CreatedAtAction(nameof(GetTorneo), new { id = nuevoTorneo.id }, resultDto);
        }

        // POST: api/Torneos/{torneoId}/Usuarios/{userId}
        [HttpPost("{torneoId}/Usuarios/{userId}")]
        public async Task<IActionResult> AddUsuarioToTorneo(int torneoId, int userId)
        {
            var torneo = await _context.Torneo.FindAsync(torneoId);
            if (torneo == null) return NotFound($"Torneo {torneoId} no existe.");

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound($"Usuario {userId} no existe.");

            if (torneo.Participants.Any(u => u.id == userId))
                return BadRequest($"El usuario {userId} ya está en el torneo {torneoId}.");

            torneo.Participants.Add(user);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/Torneos/{torneoId}/Usuarios/{userId}
        [HttpDelete("{torneoId}/Usuarios/{userId}")]
        public async Task<IActionResult> RemoveUsuarioFromTorneo(int torneoId, int userId)
        {
            var torneo = await _context.Torneo
                .Include(t => t.Participants)
                .SingleOrDefaultAsync(t => t.id == torneoId);

            if (torneo == null) return NotFound($"Torneo {torneoId} no existe.");

            var user = torneo.Participants.SingleOrDefault(u => u.id == userId);
            if (user == null) return NotFound($"Usuario {userId} no está inscrito en el torneo {torneoId}.");

            torneo.Participants.Remove(user);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/Torneos/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTorneo(int id)
        {
            var torneo = await _context.Torneo.FindAsync(id);
            if (torneo == null)
                return NotFound();

            _context.Torneo.Remove(torneo);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool TorneoExists(int id)
        {
            return _context.Torneo.Any(e => e.id == id);
        }


        //----------------------------------------------------------------------------------------------------
        [HttpPatch("{id}/participants/{userId}")]
        public async Task<ActionResult<TorneoDto>> ToggleParticipant(int id, int userId, [FromBody] ParticipantToggleDto request)
        {
            var torneo = await _context.Torneo
                .Include(t => t.Participants)
                .FirstOrDefaultAsync(t => t.id == id);

            if (torneo == null)
                return NotFound($"Torneo {id} no existe.");

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound($"Usuario {userId} no existe.");

            var isCurrentlyInTorneo = torneo.Participants.Any(u => u.id == userId);

            if (request.Join) // Quiere unirse
            {
                if (isCurrentlyInTorneo)
                    return BadRequest("El usuario ya está inscrito.");

                if (torneo.Participants.Count >= 20)
                    return BadRequest("Torneo completo (máximo 20 participantes).");

                torneo.Participants.Add(user);
            }
            else // Quiere salirse
            {
                if (!isCurrentlyInTorneo)
                    return BadRequest("El usuario no está inscrito.");

                var existingUser = torneo.Participants.First(u => u.id == userId);
                torneo.Participants.Remove(existingUser);
            }

            // Actualizar contador
            torneo.participantes = torneo.Participants.Count;
            await _context.SaveChangesAsync();

            // Devolver resultado
            var result = new TorneoDto
            {
                Id = torneo.id,
                Name = torneo.name,
                Fecha = torneo.fecha,
                Premio = torneo.premio,
                CircuitoId = torneo.circuito_id,
                participantes = torneo.participantes,
                Participants = torneo.Participants.Select(u => new UserForTorneoDto
                {
                    Id = u.id,
                    Name = u.name,
                    Email = u.email
                }).ToList()
            };

            return Ok(result);
        }


        //--------------------------------------------------------------------------------------------------------------------------------

        // POST: api/Torneos/{id}/finalize
        [HttpPost("{id}/finalize")]
        public async Task<IActionResult> FinalizeTorneo(int id, [FromBody] FinalizeTorneoRequest request)
        {
            // 1) Verificar que el torneo existe
            var torneo = await _context.Torneo
                .Include(t => t.Participants)
                .FirstOrDefaultAsync(t => t.id == id);

            if (torneo == null)
                return NotFound("Torneo no encontrado");

            // 2) Verificar que el usuario ganador existe y está inscrito en el torneo
            var ganador = await _context.Users
                .Include(u => u.Torneos)
                .FirstOrDefaultAsync(u => u.id == request.GanadorId);

            if (ganador == null)
                return BadRequest("Usuario ganador no encontrado");

            if (!ganador.Torneos.Any(t => t.id == id))
                return BadRequest("El usuario no está inscrito en este torneo");

            // 3) Calcular puntos a otorgar
            int puntosGanador = CalcularPuntosGanador(torneo);

            // 4) Actualizar puntos del ganador
            ganador.points += puntosGanador;

            // 5) Opcional: Otorgar puntos de participación a otros usuarios
            if (request.OtorgarPuntosParticipacion)
            {
                int puntosParticipacion = CalcularPuntosParticipacion(torneo);
                var participantes = torneo.Participants.Where(u => u.id != request.GanadorId);

                foreach (var participante in participantes)
                {
                    participante.points += puntosParticipacion;
                }
            }

            // 6) Eliminar el torneo (esto también eliminará las relaciones N:N automáticamente)
            _context.Torneo.Remove(torneo);

            // 7) Guardar todos los cambios
            try
            {
                await _context.SaveChangesAsync();

                return Ok(new FinalizeTorneoResponse
                {
                    Mensaje = $"Torneo '{torneo.name}' finalizado exitosamente",
                    GanadorNombre = ganador.name,
                    PuntosOtorgados = puntosGanador,
                    NuevosPuntosGanador = ganador.points
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al finalizar el torneo: {ex.Message}");
            }
        }

        // GET: api/Torneos/{id}/participantes
        [HttpGet("{id}/participantes")]
        public async Task<ActionResult<IEnumerable<ParticipanteDto>>> GetParticipantesTorneo(int id)
        {
            var torneo = await _context.Torneo
                .Include(t => t.Participants)
                .FirstOrDefaultAsync(t => t.id == id);

            if (torneo == null)
                return NotFound("Torneo no encontrado");

            var participantes = torneo.Participants.Select(u => new ParticipanteDto
            {
                Id = u.id ?? 0,
                Name = u.name,
                Email = u.email,
                Points = u.points
            }).ToList();

            return Ok(participantes);
        }

        // Métodos auxiliares para calcular puntos
        private int CalcularPuntosGanador(Torneo torneo)
        {
            int puntosBase = 100;
            int participantes = torneo.Participants?.Count ?? 1;
            int bonusPorParticipantes = participantes * 10;
            int bonusPorPremio = (int)(torneo.premio / 1000); // 1 punto por cada 1000€ de premio

            return puntosBase + bonusPorParticipantes + bonusPorPremio;
        }

        private int CalcularPuntosParticipacion(Torneo torneo)
        {
            return 25; // Puntos fijos para participantes que no ganaron
        }

        // DTOs para las requests y responses
        public class FinalizeTorneoRequest
        {
            public int GanadorId { get; set; }
            public bool OtorgarPuntosParticipacion { get; set; } = true;
        }

        public class FinalizeTorneoResponse
        {
            public string Mensaje { get; set; }
            public string GanadorNombre { get; set; }
            public int PuntosOtorgados { get; set; }
            public int NuevosPuntosGanador { get; set; }
        }

        public class ParticipanteDto
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string Email { get; set; }
            public int Points { get; set; }
        }


    }
}
