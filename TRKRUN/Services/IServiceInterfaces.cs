// Services/IServiceInterfaces.cs
using System.Collections.Generic;
using System.Threading.Tasks;
using TRKRUN.Identity;

namespace TRKRUN.Services
{
    public interface IUserService
    {
        Task<IEnumerable<User>> GetAllAsync();
        Task<User?> GetByIdAsync(int id);
        Task<User> CreateAsync(User newUser);
        Task<bool> UpdateAsync(int id, User updatedUser);
        Task<bool> DeleteAsync(int id);
    }

    public interface ITorneoService
    {
        Task<IEnumerable<Torneo>> GetAllAsync();
        Task<Torneo?> GetByIdAsync(int id);
        Task<Torneo> CreateAsync(Torneo newTorneo);
        Task<bool> UpdateAsync(int id, Torneo updatedTorneo);
        Task<bool> DeleteAsync(int id);
    }

    public interface ICircuitoService
    {
        Task<IEnumerable<Circuito>> GetAllAsync();
        Task<Circuito?> GetByIdAsync(int id);
        Task<Circuito> CreateAsync(Circuito newCircuito);
        Task<bool> UpdateAsync(int id, Circuito updatedCircuito);
        Task<bool> DeleteAsync(int id);
    }

    public interface IRolService
    {
        Task<IEnumerable<Rol>> GetAllAsync();
        Task<Rol?> GetByIdAsync(int id);
        Task<Rol> CreateAsync(Rol newCircuito);
        Task<bool> UpdateAsync(int id, Rol updatedCircuito);
        Task<bool> DeleteAsync(int id);
    }

    // Añade aquí más interfaces según tus servicios…
}
