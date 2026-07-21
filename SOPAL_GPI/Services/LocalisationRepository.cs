using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SOPAL_GPI.Models;

namespace SOPAL_GPI.Services
{
    public class LocalisationRepository(IGenericRepository<Localistation> _localisationRepo): ILocalisationRepository
    {

        public Task AddAsync(Localistation entity) => _localisationRepo.AddAsync(entity);

        public Task DeleteAsync(Localistation entity) => _localisationRepo.DeleteAsync(entity);

        public Task<List<Localistation>> GetAllAsync() => _localisationRepo.GetAllAsync();

        public Task<Localistation?> GetByIdAsync(int id) => _localisationRepo.GetByIdAsync(id);

        public Task UpdateAsync(Localistation entity) => _localisationRepo.UpdateAsync(entity);
    }

    public interface ILocalisationRepository : IGenericRepository<Localistation>
    {
    }
}
