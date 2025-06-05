using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TRKRUN.Context;
using TRKRUN.Identity;
using TRKRUN.DTOs;               // Aquí están UserDto y TorneoDto (si los usas en GET)
using TRKRUN.DTOs.CreateDTOs;    // Para CreateUserDto (POST)
using TRKRUN.DTOs.UpdateDTOs;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;    // Para UpdateUserDto (PUT)

namespace TRKRUN.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UsersController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Users
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers()
        {
            var usuarios = await _context.Users
                .Include(u => u.Torneos)
                .ToListAsync();

            var usuariosDto = usuarios.Select(u => new UserDto
            {
                Id = u.id,
                Name = u.name,
                Email = u.email,
                rol_id = u.rol_id,
                points = u.points,
                TorneosInscritos = u.Torneos
                    .Select(t => new TorneoDto
                    {
                        Id = t.id,
                        Name = t.name,
                        Fecha = t.fecha,
                        Premio = t.premio
                    })
                    .ToList()
            });

            return Ok(usuariosDto);
        }

        // GET: api/Users/5
        [HttpGet("{id}")]
        public async Task<ActionResult<UserDto>> GetUser(int id)
        {
            var user = await _context.Users
                .Include(u => u.Torneos)
                .FirstOrDefaultAsync(u => u.id == id);

            if (user == null)
                return NotFound();

            var userDto = new UserDto
            {
                Id = user.id,
                Name = user.name,
                Email = user.email,
                rol_id = user.rol_id,
                points = user.points,
                TorneosInscritos = user.Torneos
                    .Select(t => new TorneoDto
                    {
                        Id = t.id,
                        Name = t.name,
                        Fecha = t.fecha,
                        Premio = t.premio
                    })
                    .ToList()
            };

            return Ok(userDto);
        }

        // PUT: api/Users/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutUser(int id, UpdateUserDto dto)
        {
            // 1) Recuperar el usuario existente, incluyendo su relación de Torneos
            var user = await _context.Users
                .Include(u => u.Torneos)
                .FirstOrDefaultAsync(u => u.id == id);

            if (user == null)
                return NotFound();

            // 2) Actualizar campos simples
            user.name = dto.Name;
            user.email = dto.Email;
            user.rol_id = dto.rol_id;
            user.points = dto.points;
            if (!string.IsNullOrEmpty(dto.Password))
            {
                user.password = BCrypt.Net.BCrypt.HashPassword(dto.Password);
            }
            // 3) Obtener lista de IDs de torneos que vienen en el DTO
            var idsTorneosDto = dto.TorneosInscritos
                                   .Select(t => t.Id)
                                   .Where(i => i.HasValue)
                                   .Select(i => i!.Value)
                                   .ToList();

            // 4) Cargar desde la BD los torneos correspondientes a esos IDs
            var torneosEnBD = await _context.Torneo
                .Where(t => idsTorneosDto.Contains(t.id))
                .ToListAsync();

            // 5) Reemplazar la colección actual de Torneos del usuario
            user.Torneos.Clear();
            foreach (var torneo in torneosEnBD)
            {
                user.Torneos.Add(torneo);
            }

            // 6) Intentar guardar los cambios
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UserExists(id))
                    return NotFound();
                else
                    throw;
            }

            // 7) Devolver 204 No Content
            return NoContent();
        }

        // POST: api/Users
        [HttpPost]
        public async Task<ActionResult<CreateUserDto>> PostUser(CreateUserDto dto)
        {
            var userEntidad = new User
            {
                name = dto.Name,
                email = dto.Email,
                rol_id = dto.rol_id,
                password = dto.Password
            };

            _context.Users.Add(userEntidad);
            await _context.SaveChangesAsync();

            dto.Id = userEntidad.id;

            return CreatedAtAction(
                nameof(GetUser),
                new { id = dto.Id },
                dto
            );
        }

        // DELETE: api/Users/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound();

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool UserExists(int id)
        {
            return _context.Users.Any(e => e.id == id);
        }

        [HttpGet("profile")]
        [Authorize]
        public async Task<ActionResult<UserDto>> GetCurrentUserProfile()
        {
            var userId = GetUserIdFromToken();

            var user = await _context.Users
                .Include(u => u.Torneos)
                .FirstOrDefaultAsync(u => u.id == userId);

            if (user == null)
                return NotFound();

            var userDto = new UserDto
            {
                Id = user.id,
                Name = user.name,
                Email = user.email,
                rol_id = user.rol_id,
                points = user.points,
                TorneosInscritos = user.Torneos
                    .Select(t => new TorneoDto
                    {
                        Id = t.id,
                        Name = t.name,
                        Fecha = t.fecha,
                        Premio = t.premio
                    })
                    .ToList()
            };

            return Ok(userDto);
        }

        [HttpPut("profile")]
        [Authorize]
        public async Task<ActionResult<UserDto>> UpdateCurrentUserProfile([FromBody] UpdateNameRequest request)
        {
            var userId = GetUserIdFromToken();

            var user = await _context.Users
                .Include(u => u.Torneos)
                .FirstOrDefaultAsync(u => u.id == userId);

            if (user == null)
                return NotFound();

            // Actualizar solo el nombre
            user.name = request.Name;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UserExists(userId))
                    return NotFound();
                else
                    throw;
            }

            // Retornar el UserDto actualizado
            var userDto = new UserDto
            {
                Id = user.id,
                Name = user.name,
                Email = user.email,
                rol_id = user.rol_id,
                points = user.points,
                TorneosInscritos = user.Torneos
                    .Select(t => new TorneoDto
                    {
                        Id = t.id,
                        Name = t.name,
                        Fecha = t.fecha,
                        Premio = t.premio
                    })
                    .ToList()
            };

            return Ok(userDto);
        }

        private int GetUserIdFromToken()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                              ?? User.FindFirst("sub")?.Value;

            if (int.TryParse(userIdClaim, out int userId))
                return userId;

            throw new UnauthorizedAccessException("Usuario no encontrado en el token");
        }

        public class UpdateNameRequest
        {
            public string Name { get; set; }
        }
    }
}
