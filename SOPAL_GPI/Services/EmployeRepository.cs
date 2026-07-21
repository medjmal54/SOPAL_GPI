using SOPAL_GPI.Models;

namespace SOPAL_GPI.Services;

public class EmployeRepository(IGenericRepository<Employe> genericrepo): IGenericRepository<Employe>, IEmployeRepository
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
}

public interface IEmployeRepository : IGenericRepository<Employe>
{
    public Task<int> GenerateMatAsync();
}