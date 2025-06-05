using TRKRUN.DTOs;

namespace TRKRUN.Services
{
    public interface IAuthService
    {
        /// <summary>
        /// Registra un nuevo usuario. Lanza BadRequestException si el email ya existe.
        /// </summary>
        Task RegisterAsync(RegisterUserDto dto);

        /// <summary>
        /// Autentica un usuario (email + contraseña). Devuelve un AuthResponseDto con el JWT.
        /// Lanza NotFoundException si el usuario no existe o la contraseña es inválida.
        /// </summary>
        Task<AuthResponseDto> AuthenticateAsync(string email, string password);
    }
}
