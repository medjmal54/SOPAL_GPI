using SOPAL_GPI.Models;

namespace SOPAL_GPI.Services;

public class MatypeRepository(IGenericRepository<Matype> matypeRepo) : IGenericRepository<Matype>, IMatypeRepository
{
    public Task AddAsync(Matype entity)
    {
        throw new NotImplementedException();
    }

    public Task DeleteAsync(Matype entity)
    {
        throw new NotImplementedException();
    }

    public async Task<List<Matype>> GetAllAsync()
    {
        return await matypeRepo.GetAllAsync();
    }

    public Task<Matype?> GetByIdAsync(int id)
    {
        return matypeRepo.GetByIdAsync(id);
    }

    public Task UpdateAsync(Matype entity)
    {
        throw new NotImplementedException();
    }
}

public interface IMatypeRepository: IGenericRepository<Matype>
{

}