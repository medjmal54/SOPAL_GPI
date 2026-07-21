using SOPAL_GPI.Models;

namespace SOPAL_GPI.Services;

public class TypMovRepository(IGenericRepository<TypMov> typmov) : ITypMovRepository
{
    public Task AddAsync(TypMov entity)
    {
        throw new NotImplementedException();
    }

    public Task DeleteAsync(TypMov entity)
    {
        throw new NotImplementedException();
    }

    public async Task<List<TypMov>> GetAllAsync()
    {
        return await typmov.GetAllAsync();
    }

    public async Task<TypMov?> GetByIdAsync(int id)
    {
        return await typmov.GetByIdAsync(id);
    }

    public Task UpdateAsync(TypMov entity)
    {
        throw new NotImplementedException();
    }
}

public interface ITypMovRepository : IGenericRepository<TypMov>
{

}
