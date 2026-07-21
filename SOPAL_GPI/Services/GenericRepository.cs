using Microsoft.EntityFrameworkCore;
using SOPAL_GPI.Data;
namespace SOPAL_GPI.Services;

public class GenericRepository<T>(GpiContext _context) : IGenericRepository<T> where T : class
{
    private readonly DbSet<T> _dbSet = _context.Set<T>();
    public async Task<List<T>> GetAllAsync() => await _dbSet.AsNoTracking().ToListAsync();
    public async Task<T?> GetByIdAsync(int id) => await _dbSet.FindAsync(id);

    public async Task AddAsync(T entity)
    {
        await _dbSet.AddAsync(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(T entity)
    {
        _dbSet.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(T entity)
    {
        _dbSet.Remove(entity);
        await _context.SaveChangesAsync();
    }
}

public interface IGenericRepository<T> where T : class
{
    Task<List<T>> GetAllAsync();
    Task<T?> GetByIdAsync(int id);
    Task AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(T entity);
}