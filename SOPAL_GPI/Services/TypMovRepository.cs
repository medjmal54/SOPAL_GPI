using Microsoft.EntityFrameworkCore;
using SOPAL_GPI.Data;
using SOPAL_GPI.Models;

namespace SOPAL_GPI.Services;

public class TypMovRepository(GpiContext db) : ITypMovRepository
{
    public async Task<List<TypMov>> GetAllAsync()
    {
        return await db.TypMovs.AsNoTracking().ToListAsync();
    }

    public async Task<TypMov?> GetByIdAsync(int id)
    {
        return await db.TypMovs.FindAsync(id);
    }

    public async Task AddAsync(TypMov entity)
    {
        await db.TypMovs.AddAsync(entity);
        await db.SaveChangesAsync();
    }

    public async Task UpdateAsync(TypMov entity)
    {
        db.TypMovs.Update(entity);
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(TypMov entity)
    {
        db.TypMovs.Remove(entity);
        await db.SaveChangesAsync();
    }
}

public interface ITypMovRepository
{
    Task<List<TypMov>> GetAllAsync();
    Task<TypMov?> GetByIdAsync(int id);
    Task AddAsync(TypMov entity);
    Task UpdateAsync(TypMov entity);
    Task DeleteAsync(TypMov entity);
}
