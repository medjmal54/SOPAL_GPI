using Microsoft.EntityFrameworkCore;
using SOPAL_GPI.Data;
using SOPAL_GPI.Models;
using SOPAL_GPI.Services;

namespace SOPAL_GPI.Services;

public class MouvementRepository(IGenericRepository<Mouvement> MouvRepo, IMaterialRepository materialRepository, GpiContext db) : IMouvementRepository, IGenericRepository<Mouvement>
{
    public async Task AddAsync(Mouvement entity) => await MouvRepo.AddAsync(entity);
    

    public async Task DeleteAsync(Mouvement entity)
    {
        await MouvRepo.DeleteAsync(entity);
    }

    public async Task<List<Mouvement>> GetAllAsync()
    {
        return await MouvRepo.GetAllAsync();
    }

    public async Task<Mouvement?> GetByIdAsync(int id) => await MouvRepo.GetByIdAsync(id);

    public async Task<List<Mouvement>> GetMouvementsById(int id) => await db.Mouvements.Where(x => x.MaterielId == id).ToListAsync();
    

    public Task UpdateAsync(Mouvement entity)
    {
        throw new NotImplementedException();
    }

    public async Task AffecterEmp(Material entity, int matricule)
    {
        entity.EmployeMat = matricule;
        var mouvement = new Mouvement
        {
            Typmov = 1,
            MaterielId = entity.Id,
            AncienEtatmatId = entity.EtatId,
            NouvEtatmatId = 2,
            LocalisationId = entity.LocalisationId,
            EmployeMat = matricule,
            Commentaires = $"Ce matériel a été affecté à l'employé {matricule}"
        };
        entity.EtatId = 2;
        entity.DateModif = DateTime.Now;
        await materialRepository.UpdateAsync(entity);
        await MouvRepo.AddAsync(mouvement);
    }

    public async Task Transfert(Material entity, int localisation)
    {
        entity.EmployeMat = null;
        entity.DateModif = DateTime.Now;
        entity.LocalisationId = localisation;
        await materialRepository.UpdateAsync(entity);
        var mouvement = new Mouvement
        {
            Typmov = 3,
            MaterielId = entity.Id,
            LocalisationId = localisation,
            AncienEtatmatId = entity.EtatId,
            NouvEtatmatId = entity.EtatId,
            EmployeMat = entity.EmployeMat,
            Commentaires = "Matériel transferé!"
        };
        await MouvRepo.AddAsync(mouvement);
    }

    public async Task Retour(Material entity)
    {
        entity.LocalisationId = 6;
        entity.EmployeMat = null;
        var mouvement = new Mouvement
        {
            Typmov = 2,
            MaterielId = entity.Id,
            LocalisationId = 6,
            AncienEtatmatId = entity.EtatId,
            NouvEtatmatId = 1,
            EmployeMat = null,
            Commentaires = "Retour"
        };

        await MouvRepo.AddAsync(mouvement);
        entity.DateModif = DateTime.Now;
        entity.EtatId = mouvement.NouvEtatmatId;
        await materialRepository.UpdateAsync(entity);
    }

    public Task<List<Mouvement>> GetIntrvRequests()
    {
        return db.Mouvements.Where(x => x.Typmov == 4 && x.Statut == "Pas encore intervenu").ToListAsync();
    }

    public async Task<List<Mouvement>> GetRequetsByUserAsync(int matricule)
    {
        return await db.Mouvements.Where(x => x.EmployeMat == matricule && x.Typmov == 4).ToListAsync();
    }

    public async Task<int> GetPendingRequestCountAsync()
    {
        return await db.Mouvements.CountAsync(x => x.Typmov == 4 && x.Statut == "Pas encore intervenu");
    }

    public async Task ResolveAsync(int mouvementId)
    {
        var mouvement = await db.Mouvements.FindAsync(mouvementId);
        if (mouvement is not null)
        {
            mouvement.Statut = "Terminé";
            await db.SaveChangesAsync();
        }
    }

    public async Task ToggleResolveAsync(int mouvementId)
    {
        var mouvement = await db.Mouvements.FindAsync(mouvementId);
        if (mouvement is not null)
        {
            mouvement.Statut = mouvement.Statut == "Terminé" ? "En cours" : "Terminé";
            await db.SaveChangesAsync();
        }
    }

    public async Task StartInterventionAsync(int mouvementId, int adminMatricule)
    {
        var mouvement = await db.Mouvements.FindAsync(mouvementId);
        if (mouvement is not null)
        {
            mouvement.Statut = "En cours";
            mouvement.EmployeMat = adminMatricule;
            await db.SaveChangesAsync();
        }
    }

    public async Task<List<Mouvement>> GetMyInterventionsAsync(int adminMatricule)
    {
        return await db.Mouvements
            .Where(x => x.Typmov == 4 && x.EmployeMat == adminMatricule)
            .ToListAsync();
    }

    public async Task<Mouvement> AddRequestAsync(int matricule, Material entity, string comm)
    {
        entity.LocalisationId = 4;
        entity.DateModif = DateTime.Now;
        var mouvement = new Mouvement
        {
            Typmov = 4,
            MaterielId = entity.Id,
            AncienEtatmatId = entity.EtatId,
            NouvEtatmatId = 3,
            EmployeMat = null,
            LocalisationId = 4,
            Commentaires = comm,
            Statut = "Pas encore intervenu"
        };
        await MouvRepo.AddAsync(mouvement);
        entity.EtatId = 3;
        await materialRepository.UpdateAsync(entity);
        return mouvement;
    }
}

public interface IMouvementRepository : IGenericRepository<Mouvement>
{
    public Task AffecterEmp(Material entity, int matricule);
    public Task Transfert(Material entity, int localisation);
    public Task Retour(Material entity);
    public Task<List<Mouvement>> GetMouvementsById(int id);
    public Task<List<Mouvement>> GetIntrvRequests();
    public Task<List<Mouvement>> GetRequetsByUserAsync(int matricule);
    public Task<int> GetPendingRequestCountAsync();
    public Task<Mouvement> AddRequestAsync(int matricule, Material entity, string comm);
    public Task ResolveAsync(int mouvementId);
    public Task ToggleResolveAsync(int mouvementId);
    public Task StartInterventionAsync(int mouvementId, int adminMatricule);
    public Task<List<Mouvement>> GetMyInterventionsAsync(int adminMatricule);
}
