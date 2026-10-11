using Microsoft.EntityFrameworkCore;
using SOPAL_GPI.Data;
using SOPAL_GPI.Models;

namespace SOPAL_GPI.Services
{
    public class LocalisationRepository(GpiContext db) : ILocalisationRepository
    {
        public async Task<List<Localistation>> GetAllAsync()
        {
            return await db.Localistations.AsNoTracking().ToListAsync();
        }

        public async Task<Localistation?> GetByIdAsync(int id)
        {
            return await db.Localistations.FindAsync(id);
        }

        public async Task AddAsync(Localistation entity)
        {
            await db.Localistations.AddAsync(entity);
            await db.SaveChangesAsync();
        }

        public async Task UpdateAsync(Localistation entity)
        {
            db.Localistations.Update(entity);
            await db.SaveChangesAsync();
        }

        public async Task DeleteAsync(Localistation entity)
        {
            db.Localistations.Remove(entity);
            await db.SaveChangesAsync();
        }
    }

    public interface ILocalisationRepository
    {
        Task<List<Localistation>> GetAllAsync();
        Task<Localistation?> GetByIdAsync(int id);
        Task AddAsync(Localistation entity);
        Task UpdateAsync(Localistation entity);
        Task DeleteAsync(Localistation entity);
    }
}
