using Microsoft.EntityFrameworkCore;
using SOPAL_GPI.Data;
using SOPAL_GPI.Models;

namespace SOPAL_GPI.Services;

public class MatypeRepository(GpiContext db) : IMatypeRepository
{
    public async Task<List<Matype>> GetAllAsync()
    {
        return await db.Matypes.AsNoTracking().ToListAsync();
    }

    public async Task<Matype?> GetByIdAsync(int id)
    {
        return await db.Matypes.FindAsync(id);
    }

    public async Task AddAsync(Matype entity)
    {
        await db.Matypes.AddAsync(entity);
        await db.SaveChangesAsync();
    }

    public async Task UpdateAsync(Matype entity)
    {
        db.Matypes.Update(entity);
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(Matype entity)
    {
        db.Matypes.Remove(entity);
        await db.SaveChangesAsync();
    }
}

public interface IMatypeRepository
{
    Task<List<Matype>> GetAllAsync();
    Task<Matype?> GetByIdAsync(int id);
    Task AddAsync(Matype entity);
    Task UpdateAsync(Matype entity);
    Task DeleteAsync(Matype entity);
}
