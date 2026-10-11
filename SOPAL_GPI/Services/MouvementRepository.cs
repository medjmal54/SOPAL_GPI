using Microsoft.EntityFrameworkCore;
using SOPAL_GPI.Data;
using SOPAL_GPI.DTOs;
using SOPAL_GPI.Models;

namespace SOPAL_GPI.Services;

public class MouvementRepository(GpiContext db, IMaterialRepository materialRepository) : IMouvementRepository
{

    public async Task<List<Mouvement>> GetAllAsync()
    {
        return await db.Mouvements.AsNoTracking().ToListAsync();
    }

    public async Task<Mouvement?> GetByIdAsync(int id)
    {
        return await db.Mouvements.FindAsync(id);
    }

    public async Task AddAsync(Mouvement entity)
    {
        await db.Mouvements.AddAsync(entity);
        await db.SaveChangesAsync();
    }

    public async Task UpdateAsync(Mouvement entity)
    {
        db.Mouvements.Update(entity);
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(Mouvement entity)
    {
        db.Mouvements.Remove(entity);
        await db.SaveChangesAsync();
    }

    public async Task<List<Mouvement>> GetMouvementsById(int id)
    {
        return await db.Mouvements.Where(x => x.MaterielId == id).ToListAsync();
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
            Commentaires = $"Ce mat�riel a �t� affect� � l'employ� {matricule}"
        };
        entity.EtatId = 2;
        entity.DateModif = DateTime.Now;
        await materialRepository.UpdateAsync(entity);
        await AddAsync(mouvement);
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
            Commentaires = "Mat�riel transfer�!"
        };
        await AddAsync(mouvement);
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

        await AddAsync(mouvement);
        entity.DateModif = DateTime.Now;
        entity.EtatId = mouvement.NouvEtatmatId;
        await materialRepository.UpdateAsync(entity);
    }

    public Task<List<Mouvement>> GetIntrvRequests()
    {
        return db.Mouvements.Where(x => x.Typmov == 4 && x.Statut == "Pas encore intervenu").ToListAsync();
    }

    public async Task<List<Mouvement>> GetIntrvRequests(string? q)
    {
        var query = db.Mouvements.AsNoTracking().Where(x => x.Typmov == 4 && x.Statut == "Pas encore intervenu");
        if (!string.IsNullOrWhiteSpace(q))
        {
            var needle = q.Trim().ToLower();
            query = query.Where(m =>
                (m.Materiel != null && m.Materiel.Libelle.ToLower().Contains(needle)) ||
                (m.Commentaires != null && m.Commentaires.ToLower().Contains(needle)) ||
                (m.EmployeMatNavigation != null &&
                    (m.EmployeMatNavigation.Nom.ToLower().Contains(needle) ||
                     m.EmployeMatNavigation.Prenom.ToLower().Contains(needle))));
        }
        return await query.OrderByDescending(m => m.DateMouvement).ToListAsync();
    }

    public async Task<List<Mouvement>> GetRequetsByUserAsync(int matricule)
    {
        return await db.Mouvements.Where(x => x.EmployeMat == matricule && x.Typmov == 4).ToListAsync();
    }

    public async Task<List<Mouvement>> GetRequetsByUserAsync(int matricule, string? q)
    {
        var query = db.Mouvements.AsNoTracking().Where(x => x.EmployeMat == matricule && x.Typmov == 4);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var needle = q.Trim().ToLower();
            query = query.Where(m =>
                (m.Materiel != null && m.Materiel.Libelle.ToLower().Contains(needle)) ||
                (m.Commentaires != null && m.Commentaires.ToLower().Contains(needle)));
        }
        return await query.OrderByDescending(m => m.DateMouvement).ToListAsync();
    }

    public async Task<Mouvement?> GetRequestWithDetails(int id)
    {
        return await db.Mouvements
            .Include(m => m.Materiel)
            .Include(m => m.EmployeMatNavigation)
            .FirstOrDefaultAsync(m => m.Id == id && m.Typmov == 4);
    }

    public async Task<List<Mouvement>> SearchAsync(string? q, int? typeId, int? localisationId, int? employeMat, int? typeMouvement, string? department)
    {
        var query = db.Mouvements.AsNoTracking()
            .Include(m => m.Materiel)
            .Include(m => m.EmployeMatNavigation)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var needle = q.Trim().ToLower();
            query = query.Where(m =>
                (m.Commentaires != null && m.Commentaires.ToLower().Contains(needle)) ||
                (m.Materiel != null && (
                    m.Materiel.Libelle.ToLower().Contains(needle) ||
                    m.Materiel.NumSerie.ToLower().Contains(needle) ||
                    m.Materiel.Marque.ToLower().Contains(needle) ||
                    m.Materiel.Modele.ToLower().Contains(needle)
                )) ||
                (m.EmployeMatNavigation != null && (
                    m.EmployeMatNavigation.Nom.ToLower().Contains(needle) ||
                    m.EmployeMatNavigation.Prenom.ToLower().Contains(needle)
                ))
            );
        }
        if (typeId.HasValue)
        {
            query = query.Where(m => m.Materiel != null && m.Materiel.TypeMaterielId == typeId.Value);
        }
        if (localisationId.HasValue) query = query.Where(m => m.LocalisationId == localisationId.Value);
        if (employeMat.HasValue) query = query.Where(m => m.EmployeMat == employeMat.Value);
        if (typeMouvement.HasValue) query = query.Where(m => m.Typmov == typeMouvement.Value);
        if (!string.IsNullOrWhiteSpace(department) && department != "all")
            query = query.Where(m => m.EmployeMatNavigation != null && m.EmployeMatNavigation.Service == department);

        var items = await query.OrderByDescending(m => m.DateMouvement).ToListAsync();
        return items;
    }

    public async Task<PaginatedResult<Mouvement>> SearchPaginatedAsync(string? q, int? typeId, int? localisationId, int? employeMat, int? typeMouvement, string? department, int page = 1, int pageSize = 10)
    {
        var query = db.Mouvements.AsNoTracking()
            .Include(m => m.Materiel)
            .Include(m => m.EmployeMatNavigation)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var needle = q.Trim().ToLower();
            query = query.Where(m =>
                (m.Commentaires != null && m.Commentaires.ToLower().Contains(needle)) ||
                (m.Materiel != null && (
                    m.Materiel.Libelle.ToLower().Contains(needle) ||
                    m.Materiel.NumSerie.ToLower().Contains(needle) ||
                    m.Materiel.Marque.ToLower().Contains(needle) ||
                    m.Materiel.Modele.ToLower().Contains(needle)
                )) ||
                (m.EmployeMatNavigation != null && (
                    m.EmployeMatNavigation.Nom.ToLower().Contains(needle) ||
                    m.EmployeMatNavigation.Prenom.ToLower().Contains(needle)
                ))
            );
        }
        if (typeId.HasValue)
        {
            query = query.Where(m => m.Materiel != null && m.Materiel.TypeMaterielId == typeId.Value);
        }
        if (localisationId.HasValue) query = query.Where(m => m.LocalisationId == localisationId.Value);
        if (employeMat.HasValue) query = query.Where(m => m.EmployeMat == employeMat.Value);
        if (typeMouvement.HasValue) query = query.Where(m => m.Typmov == typeMouvement.Value);
        if (!string.IsNullOrWhiteSpace(department) && department != "all")
            query = query.Where(m => m.EmployeMatNavigation != null && m.EmployeMatNavigation.Service == department);

        var total = await query.CountAsync();
        var items = await query.OrderByDescending(m => m.DateMouvement)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PaginatedResult<Mouvement> { Items = items, Total = total };
    }

        public async Task<Mouvement> AddRequestAsync(int matricule, Material material, string descPanne)
    {
        var mouvement = new Mouvement
        {
            Typmov = 4,
            MaterielId = material.Id,
            LocalisationId = material.LocalisationId,
            AncienEtatmatId = material.EtatId,
            NouvEtatmatId = material.EtatId,
            EmployeMat = matricule,
            Commentaires = descPanne,
            Statut = "Pas encore intervenu",
            DateMouvement = DateTime.Now
        };
        await db.Mouvements.AddAsync(mouvement);
        await db.SaveChangesAsync();
        return mouvement;
    }

    public async Task<int> GetPendingRequestCountAsync()
    {
        return await db.Mouvements.CountAsync(x => x.Typmov == 4 && x.Statut == "Pas encore intervenu");
    }

    public async Task ResolveAsync(int id)
    {
        var mouvement = await db.Mouvements.FindAsync(id);
        if (mouvement != null)
        {
            mouvement.Statut = "Terminé";
            db.Mouvements.Update(mouvement);
            await db.SaveChangesAsync();
        }
    }

    public async Task ToggleResolveAsync(int id)
    {
        var mouvement = await db.Mouvements.FindAsync(id);
        if (mouvement != null)
        {
            mouvement.Statut = mouvement.Statut == "Terminé" ? "Pas encore intervenu" : "Terminé";
            db.Mouvements.Update(mouvement);
            await db.SaveChangesAsync();
        }
    }

    public async Task StartInterventionAsync(int id, int matricule)
    {
        var mouvement = await db.Mouvements.FindAsync(id);
        if (mouvement != null)
        {
            mouvement.Statut = "En cours";
            // IntervenantId stored on Intervention, not on Mouvement
            db.Mouvements.Update(mouvement);
            await db.SaveChangesAsync();
        }
    }

    public async Task<List<Mouvement>> GetMyInterventionsAsync(int matricule, string? q, bool? done)
    {
        var query = db.Mouvements.AsNoTracking()
            .Where(x => x.Typmov == 4 && x.Interventions.Any(i => i.IntervenantId == matricule));
        
        if (done.HasValue)
        {
            query = done.Value ? query.Where(x => x.Statut == "Terminé") : query.Where(x => x.Statut != "Terminé");
        }
        if (!string.IsNullOrWhiteSpace(q))
        {
            var needle = q.Trim().ToLower();
            query = query.Where(m =>
                (m.Materiel != null && m.Materiel.Libelle.ToLower().Contains(needle)) ||
                (m.Commentaires != null && m.Commentaires.ToLower().Contains(needle)));
        }
        return await query.Include(m => m.Materiel).OrderByDescending(m => m.DateMouvement).ToListAsync();
    }

}

public interface IMouvementRepository
{

    Task<List<Mouvement>> GetAllAsync();
    Task<Mouvement?> GetByIdAsync(int id);
    Task AddAsync(Mouvement entity);
    Task UpdateAsync(Mouvement entity);
    Task DeleteAsync(Mouvement entity);
    Task<List<Mouvement>> GetMouvementsById(int id);
    Task AffecterEmp(Material entity, int matricule);
    Task Transfert(Material entity, int localisation);
    Task Retour(Material entity);
    Task<List<Mouvement>> GetIntrvRequests();
    Task<List<Mouvement>> GetIntrvRequests(string? q);
    Task<List<Mouvement>> GetRequetsByUserAsync(int matricule);
    Task<List<Mouvement>> GetRequetsByUserAsync(int matricule, string? q);
    Task<Mouvement?> GetRequestWithDetails(int id);
    Task<List<Mouvement>> SearchAsync(string? q, int? typeId, int? localisationId, int? employeMat, int? typeMouvement, string? department);
    Task<PaginatedResult<Mouvement>> SearchPaginatedAsync(string? q, int? typeId, int? localisationId, int? employeMat, int? typeMouvement, string? department, int page = 1, int pageSize = 10);
    Task<Mouvement> AddRequestAsync(int matricule, Material material, string descPanne);
    Task<int> GetPendingRequestCountAsync();
    Task ResolveAsync(int id);
    Task ToggleResolveAsync(int id);
    Task StartInterventionAsync(int id, int matricule);
    Task<List<Mouvement>> GetMyInterventionsAsync(int matricule, string? q, bool? done);

}


