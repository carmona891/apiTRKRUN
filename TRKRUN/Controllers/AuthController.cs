using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using TRKRUN.Context;
using TRKRUN.DTOs;
using TRKRUN.Identity;

namespace TRKRUN.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly PasswordHasher<User> _passwordHasher;
        private readonly IConfiguration _configuration;

        public AuthController(
            AppDbContext context,
            PasswordHasher<User> passwordHasher,
            IConfiguration configuration)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _configuration = configuration;
        }

        // POST: api/Auth/register
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterUserDto dto)
        {
            // 1) Verificar si ya existe un usuario con ese email
            if (await _context.Users.AnyAsync(u => u.email == dto.Email))
                return BadRequest(new { message = "Ya existe un usuario con ese email." });

            // 2) Crear entidad User y hashear la contraseña
            var newUser = new User
            {
                name = dto.Name,
                email = dto.Email,
                rol_id = dto.rol_id
            };
            newUser.password = _passwordHasher.HashPassword(newUser, dto.Password);

            // 3) Guardar en BD
            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            // 4) (Opcional) Devolver un UserDto o simplemente un mensaje
            return Ok(new { message = "Usuario registrado correctamente." });
        }

        // POST: api/Auth/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            // 1) Buscar al usuario por email
            var user = await _context.Users
                              .Include(u => u.Rol) // Si luego quieres usar info del rol
                              .SingleOrDefaultAsync(u => u.email == dto.Email);

            if (user == null)
                return Unauthorized(new { message = "Email o contraseña inválidos." });

            // 2) Verificar el hash de la contraseña
            var result = _passwordHasher.VerifyHashedPassword(user, user.password, dto.Password);
            if (result == PasswordVerificationResult.Failed)
                return Unauthorized(new { message = "Email o contraseña inválidos." });

            // 3) Generar el token JWT
            var token = GenerateJwtToken(user);

            // 4) Devolver el token en la respuesta
            return Ok(new { token });
        }

        // ---------- MÉTODO PRIVADO PARA CREAR EL JWT ----------
        private string GenerateJwtToken(User user)
        {
            // 1) Leer configuración de JWT desde appsettings.json
            var jwtSection = _configuration.GetSection("Jwt");
            var key = Encoding.UTF8.GetBytes(jwtSection["Key"]!);
            var issuer = jwtSection["Issuer"]!;
            var audience = jwtSection["Audience"]!;
            var expireMin = Convert.ToInt32(jwtSection["ExpireMinutes"]);

            // 2) Definir claims básicos
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub,    user.id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email,  user.email),
                new Claim("rol_id",                       user.rol_id.ToString()),
                new Claim("name",                         user.name)
            };

            // (Si quieres meter el rol en un claim estándar, podrías usar:)
            // claims.Add(new Claim(ClaimTypes.Role, user.Rol.name));

            // 3) Crear credenciales con la llave secreta
            var credentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256
            );

            // 4) Construir el token
            var jwtToken = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: DateTime.UtcNow.AddMinutes(expireMin),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(jwtToken);
        }
    }
}
