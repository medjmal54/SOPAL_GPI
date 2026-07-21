using Microsoft.EntityFrameworkCore;
using SOPAL_GPI.Data;
using SOPAL_GPI.Models;

namespace SOPAL_GPI.Services;

public class InterventionRepository(IGenericRepository<Intervention> interventionRepository, GpiContext db) : IinterventionRepositroy
{
    public async Task AddAsync(Intervention entity)
    {
        await interventionRepository.AddAsync(entity);
    }

    public Task DeleteAsync(Intervention entity)
    {
        throw new NotImplementedException();
    }

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

    public async Task UpdateAsync(Intervention entity) =>  await interventionRepository.UpdateAsync(entity);

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

public interface IinterventionRepositroy : IGenericRepository<Intervention>
{
    public Task<List<Intervention>> GetUserInterventionsAsync(int id);
    public Task<List<Intervention>> GetByMouvementIdAsync(int mouvementId);
    public Task AddRangeAsync(IEnumerable<Intervention> entities);
}
