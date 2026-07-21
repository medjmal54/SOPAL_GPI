using SOPAL_GPI.Models;

namespace SOPAL_GPI.Services;

public class EtatMatRepository(IGenericRepository<Etatmat> etatmatRepo): IGenericRepository<Etatmat>, IEtatMatRepository
{
    public Task AddAsync(Etatmat entity)
    {
        throw new NotImplementedException();
    }

    public Task DeleteAsync(Etatmat entity)
    {
        throw new NotImplementedException();
    }

    public async Task<List<Etatmat>> GetAllAsync()
    {
        return await etatmatRepo.GetAllAsync();
    }
    public Task<Etatmat?> GetByIdAsync(int id)
    {
        return etatmatRepo.GetByIdAsync(id);
    }

    public Task UpdateAsync(Etatmat entity)
    {
        throw new NotImplementedException();
    }
}

public interface IEtatMatRepository : IGenericRepository<Etatmat>
{

}