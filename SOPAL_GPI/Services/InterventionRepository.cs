using Microsoft.EntityFrameworkCore;
using SOPAL_GPI.Data;
using SOPAL_GPI.Models;

namespace SOPAL_GPI.Services;

public class InterventionRepository(GpiContext db) : IinterventionRepositroy
{
    public async Task<List<Intervention>> GetAllAsync()
    {
        return await db.Interventions
            .Include(i => i.Intervenant)
            .Include(i => i.Maint)
            .Include(i => i.Mat)
            .ToListAsync();
    }

    public async Task<Intervention?> GetByIdAsync(int id)
    {
        return await db.Interventions
            .Include(i => i.Intervenant)
            .Include(i => i.Maint)
            .Include(i => i.Mat)
            .FirstOrDefaultAsync(i => i.Id == id);
    }

    public async Task AddAsync(Intervention entity)
    {
        await db.Interventions.AddAsync(entity);
        await db.SaveChangesAsync();
    }

    public async Task UpdateAsync(Intervention entity)
    {
        db.Interventions.Update(entity);
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(Intervention entity)
    {
        db.Interventions.Remove(entity);
        await db.SaveChangesAsync();
    }

    public async Task<List<Intervention>> GetUserInterventionsAsync(int id)
    {
        return await db.Interventions.Where(x => x.IntervenantId == id)
            .Include(i => i.Maint)
            .Include(i => i.Mat)
            .ToListAsync();
    }

    public async Task<List<Intervention>> GetByMouvementIdAsync(int mouvementId)
    {
        return await db.Interventions
            .Where(i => i.MaintId == mouvementId)
            .Include(i => i.Intervenant)
            .Include(i => i.Mat)
            .ToListAsync();
    }

    public async Task AddRangeAsync(IEnumerable<Intervention> entities)
    {
        await db.Interventions.AddRangeAsync(entities);
        await db.SaveChangesAsync();
    }
}

public interface IinterventionRepositroy
{
    Task<List<Intervention>> GetAllAsync();
    Task<Intervention?> GetByIdAsync(int id);
    Task AddAsync(Intervention entity);
    Task UpdateAsync(Intervention entity);
    Task DeleteAsync(Intervention entity);
    Task<List<Intervention>> GetUserInterventionsAsync(int id);
    Task<List<Intervention>> GetByMouvementIdAsync(int mouvementId);
    Task AddRangeAsync(IEnumerable<Intervention> entities);
}
