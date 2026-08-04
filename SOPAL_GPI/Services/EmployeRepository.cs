using Microsoft.EntityFrameworkCore;
using SOPAL_GPI.Data;
using SOPAL_GPI.DTOs;
using SOPAL_GPI.Models;

namespace SOPAL_GPI.Services;

public class EmployeRepository(IGenericRepository<Employe> genericrepo, GpiContext db) : IGenericRepository<Employe>, IEmployeRepository
{
    public async Task AddAsync(Employe entity)
    {
        await genericrepo.AddAsync(entity);
    }

    public Task DeleteAsync(Employe entity)
    {
        throw new NotImplementedException();
    }

    public async Task<List<Employe>> GetAllAsync()
    {
        return await genericrepo.GetAllAsync();
    }

    public Task<Employe?> GetByIdAsync(int id)
    {
        return genericrepo.GetByIdAsync(id);
    }

    public async Task UpdateAsync(Employe entity)
    {
        await genericrepo.UpdateAsync(entity);
    }

    public async Task<int> GenerateMatAsync()
    {
        var all = await genericrepo.GetAllAsync();
        return (DateTime.Now.Year % 100) * 1000 + all.Count;
    }

    public async Task<PaginatedResult<EmployeListItemDto>> SearchAsync(string? q, string? service)
    {
        var query = db.Employes.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var needle = q.Trim().ToLower();
            query = query.Where(e =>
                e.Nom.ToLower().Contains(needle) ||
                e.Prenom.ToLower().Contains(needle) ||
                e.Email.ToLower().Contains(needle) ||
                (e.Service != null && e.Service.ToLower().Contains(needle)) ||
                e.Poste.ToLower().Contains(needle));
        }
        if (!string.IsNullOrWhiteSpace(service))
            query = query.Where(e => e.Service == service);

        var total = await query.CountAsync();

        var items = await query
            .Select(e => new EmployeListItemDto
            {
                Matricule = e.Matricule,
                Nom = e.Nom,
                Prenom = e.Prenom,
                Email = e.Email,
                Service = e.Service,
                Poste = e.Poste,
                Actif = e.Actif,
                Role = e.Role,
                MaterialCount = e.Materials.Count()
            })
            .OrderBy(e => e.Nom)
            .ToListAsync();

        return new PaginatedResult<EmployeListItemDto> { Items = items, Total = total };
    }
}

public interface IEmployeRepository : IGenericRepository<Employe>
{
    public Task<int> GenerateMatAsync();
    public Task<PaginatedResult<EmployeListItemDto>> SearchAsync(string? q, string? service);
}