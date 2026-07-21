using Microsoft.EntityFrameworkCore;
using SOPAL_GPI.Data;
using SOPAL_GPI.DTOs;
using SOPAL_GPI.Models;

namespace SOPAL_GPI.Services;

public class MaterialRepository(IGenericRepository<Material> materialRepo, GpiContext dbContext) : IGenericRepository<Material>, IMaterialRepository
{
    public async Task AddAsync(Material entity) => await materialRepo.AddAsync(entity);

    public async Task DeleteAsync(Material entity) => await materialRepo.DeleteAsync(entity);


    public async Task<List<Material>> GetAllAsync() => await materialRepo.GetAllAsync();
    

    public async Task<Material?> GetByIdAsync(int id)
    {
        return await materialRepo.GetByIdAsync(id);
    }

    public async Task UpdateAsync(Material entity)
    {
        await materialRepo.UpdateAsync(entity);
    }

    public async Task<Material> GetMatsWithHist(Material entity)
    {
        return dbContext.Materials.Include(p => p.Mouvements).FirstOrDefault(i => i.Id == entity.Id && (i.EtatId != 6 || !i.Actif)) ?? new Material();
    }

    public async Task<List<Material>> GetByEmployeMatAsync(int matricule)
    {
        return await dbContext.Materials
            .Where(m => m.EmployeMat == matricule)
            .Include(m => m.Etat)
            .Include(m => m.Localisation)
            .Include(m => m.TypeMateriel)
            .ToListAsync();
    }
}

public interface IMaterialRepository : IGenericRepository<Material>
{
    public Task<Material> GetMatsWithHist(Material entity);
    public Task<List<Material>> GetByEmployeMatAsync(int matricule);
}
