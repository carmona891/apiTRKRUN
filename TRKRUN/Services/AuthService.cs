using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using TRKRUN.Context;
using TRKRUN.DTOs;

using TRKRUN.Identity;
using TRKRUN.Utils.Exceptions;

namespace TRKRUN.Services
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly PasswordHasher<User> _passwordHasher;
        private readonly IConfiguration _configuration;

        public AuthService(
            AppDbContext context,
            PasswordHasher<User> passwordHasher,
            IConfiguration configuration)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _configuration = configuration;
        }

        public async Task RegisterAsync(RegisterUserDto dto)
        {
            // 1) Verificar si ya hay un usuario con ese email
            bool existe = await _context.Users.AnyAsync(u => u.email == dto.Email);
            if (existe)
                throw new BadRequestException($"Ya existe un usuario con email {dto.Email}");

            // 2) Verificar que el Rol exista
            var rol = await _context.Rol.FindAsync(dto.rol_id);
            if (rol == null)
                throw new BadRequestException($"No existe un rol con id {dto.rol_id}");

            // 3) Crear la entidad User y hashear la contraseña
            var nuevoUsuario = new User
            {
                name = dto.Name,
                email = dto.Email,
                rol_id = dto.rol_id
            };
            nuevoUsuario.password = _passwordHasher.HashPassword(nuevoUsuario, dto.Password);

            // 4) Guardar en la BD
            _context.Users.Add(nuevoUsuario);
            await _context.SaveChangesAsync();
        }

        public async Task<AuthResponseDto> AuthenticateAsync(string email, string password)
        {
            // 1) Buscar al usuario por email (cargando rol opcionalmente)
            var user = await _context.Users
                            .Include(u => u.Rol)
                            .FirstOrDefaultAsync(u => u.email == email);

            if (user == null)
                throw new NotFoundException("Usuario o contraseña inválidos.");

            // 2) Verificar la contraseña contra el hash
            var result = _passwordHasher.VerifyHashedPassword(user, user.password, password);
            if (result == PasswordVerificationResult.Failed)
                throw new NotFoundException("Usuario o contraseña inválidos.");

            // 3) Generar el token JWT
            var token = GenerateJwtToken(user);

            return new AuthResponseDto
            {
                Token = token,
                Expiration = DateTime.UtcNow.AddMinutes(GetJwtExpireMinutes()),
                //UserId = user.id,
                //Email = user.email,
                //Name = user.name,
                //RolId = user.rol_id
            };
        }

        #region Métodos privados de ayuda

        private string GenerateJwtToken(User user)
        {
            var jwtSection = _configuration.GetSection("Jwt");
            var keyBytes = Encoding.UTF8.GetBytes(jwtSection["Key"]!);
            var issuer = jwtSection["Issuer"]!;
            var audience = jwtSection["Audience"]!;
            var expiresMin = Convert.ToInt32(jwtSection["ExpireMinutes"]);

            // Claims básicos: sub = userId, email, rol_id, name
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub,    user.id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email,  user.email),
                new Claim("rol_id",                       user.rol_id.ToString()),
                new Claim("name",                         user.name)
            };

            // (Si más adelante quieres roles estándar, podrías añadir:)
            // claims.Add(new Claim(ClaimTypes.Role, user.Rol.name));

            var credentials = new SigningCredentials(
                new SymmetricSecurityKey(keyBytes),
                SecurityAlgorithms.HmacSha256
            );

            var jwtToken = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: DateTime.UtcNow.AddMinutes(expiresMin),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(jwtToken);
        }

        private int GetJwtExpireMinutes()
        {
            var jwtSection = _configuration.GetSection("Jwt");
            return Convert.ToInt32(jwtSection["ExpireMinutes"]);
        }

        #endregion
    }
}
