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

    public async Task<List<Material>> GetByEmployeMatAsync(int matricule, string? q = null)
    {
        IQueryable<Material> query = dbContext.Materials
            .Where(m => m.EmployeMat == matricule)
            .Include(m => m.Etat)
            .Include(m => m.Localisation)
            .Include(m => m.TypeMateriel);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var needle = q.Trim().ToLower();
            query = query.Where(m =>
                m.Libelle.ToLower().Contains(needle) ||
                m.Marque.ToLower().Contains(needle) ||
                m.Modele.ToLower().Contains(needle) ||
                m.NumSerie.ToLower().Contains(needle));
        }

        return await query.ToListAsync();
    }

    public async Task<PaginatedResult<Material>> SearchAsync(string? q, int? typeId, int? localisationId, bool? actif, int? employeMat, string? department)
    {
        var query = dbContext.Materials.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var needle = q.Trim().ToLower();
            query = query.Where(m =>
                m.Libelle.ToLower().Contains(needle) ||
                m.Marque.ToLower().Contains(needle) ||
                m.Modele.ToLower().Contains(needle) ||
                m.NumSerie.ToLower().Contains(needle) ||
                (m.Commentaires != null && m.Commentaires.ToLower().Contains(needle)));
        }
        if (typeId.HasValue) query = query.Where(m => m.TypeMaterielId == typeId.Value);
        if (localisationId.HasValue) query = query.Where(m => m.LocalisationId == localisationId.Value);
        if (actif.HasValue) query = query.Where(m => m.Actif == actif.Value);
        if (employeMat.HasValue) query = query.Where(m => m.EmployeMat == employeMat.Value);
        if (!string.IsNullOrWhiteSpace(department) && department != "all")
            query = query.Where(m => m.EmployeMatNavigation != null && m.EmployeMatNavigation.Service == department);

        var total = await query.CountAsync();
        var items = await query.OrderByDescending(m => m.Id).ToListAsync();

        return new PaginatedResult<Material> { Items = items, Total = total };
    }
}

public interface IMaterialRepository : IGenericRepository<Material>
{
    public Task<Material> GetMatsWithHist(Material entity);
    public Task<List<Material>> GetByEmployeMatAsync(int matricule, string? q = null);
    public Task<PaginatedResult<Material>> SearchAsync(string? q, int? typeId, int? localisationId, bool? actif, int? employeMat, string? department);
}
