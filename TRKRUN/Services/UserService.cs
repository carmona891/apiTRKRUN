using Microsoft.EntityFrameworkCore;
using TRKRUN.Context;
using TRKRUN.Identity;

namespace TRKRUN.Services
{
    public class UserService : IUserService
    {
        private readonly AppDbContext _ctx;

        public UserService(AppDbContext ctx)
        {
            _ctx = ctx;
        }

        public async Task<IEnumerable<User>> GetAllAsync()
            => await _ctx.Users.ToListAsync();

        public async Task<User?> GetByIdAsync(int id)
            => await _ctx.Users.FindAsync(id);

        public async Task<User> CreateAsync(User newUser)
        {
            _ctx.Users.Add(newUser);
            await _ctx.SaveChangesAsync();
            return newUser;
        }

        public async Task<bool> UpdateAsync(int id, User updated)
        {
            var existing = await _ctx.Users.FindAsync(id);
            if (existing == null) return false;
            // Mapear cambios…
            existing.name = updated.name;
            existing.email = updated.email;
            // …
            await _ctx.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var user = await _ctx.Users.FindAsync(id);
            if (user == null) return false;
            _ctx.Users.Remove(user);
            await _ctx.SaveChangesAsync();
            return true;
        }
    }
}
