using Microsoft.EntityFrameworkCore;
using SOPAL_GPI.Data;
using SOPAL_GPI.Models;

namespace SOPAL_GPI.Services;

public class EtatMatRepository(GpiContext db) : IEtatMatRepository
{
    public async Task<List<Etatmat>> GetAllAsync()
    {
        return await db.Etatmats.AsNoTracking().ToListAsync();
    }

    public async Task<Etatmat?> GetByIdAsync(int id)
    {
        return await db.Etatmats.FindAsync(id);
    }

    public async Task AddAsync(Etatmat entity)
    {
        await db.Etatmats.AddAsync(entity);
        await db.SaveChangesAsync();
    }

    public async Task UpdateAsync(Etatmat entity)
    {
        db.Etatmats.Update(entity);
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(Etatmat entity)
    {
        db.Etatmats.Remove(entity);
        await db.SaveChangesAsync();
    }
}

public interface IEtatMatRepository
{
    Task<List<Etatmat>> GetAllAsync();
    Task<Etatmat?> GetByIdAsync(int id);
    Task AddAsync(Etatmat entity);
    Task UpdateAsync(Etatmat entity);
    Task DeleteAsync(Etatmat entity);
}
