using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SOPAL_GPI.Data;
using SOPAL_GPI.Models;
using SOPAL_GPI.Services;
using System.Security.Claims;

namespace SOPAL_GPI.Controllers;

[Route("api/export")]
[ApiController]
[Authorize]
public class ExportController(GpiContext db) : ControllerBase
{
    private const string XlsxMime = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    private const string CsvMime = "text/csv;charset=utf-8";

    [HttpGet("materials")]
    public async Task<IActionResult> ExportMaterials(string? format, string? q, int? typeId, int? localisationId, bool? actif, int? employeMat, string? department)
    {
        var query = db.Materials.AsNoTracking()
            .Include(m => m.TypeMateriel)
            .Include(m => m.Localisation)
            .Include(m => m.Etat)
            .Include(m => m.EmployeMatNavigation)
            .AsQueryable();

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

        var materials = await query.OrderBy(m => m.Id).ToListAsync();

        var headers = new[] { "ID", "Libellé", "Marque", "Modèle", "N° Série", "Date d'acquisition", "Actif", "Type", "Localisation", "État", "Employé", "Commentaires" };
        var rows = materials.Select(m => new object?[]
        {
            m.Id, m.Libelle, m.Marque, m.Modele, m.NumSerie,
            m.DateAcquisition.ToDateTime(TimeOnly.MinValue),
            m.Actif ? "Oui" : "Non",
            m.TypeMateriel?.Libelle,
            m.Localisation?.Nom,
            m.Etat?.Libelle,
            m.EmployeMatNavigation is null ? "" : $"{m.EmployeMatNavigation.Prenom} {m.EmployeMatNavigation.Nom}",
            m.Commentaires
        });

        return ExportFile("materials", headers, rows, format);
    }

    [HttpGet("employes")]
    public async Task<IActionResult> ExportEmployes(string? format, string? q, string? service)
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

        var employes = await query.OrderBy(e => e.Nom).ToListAsync();

        var headers = new[] { "Matricule", "Nom", "Prénom", "Email", "Service", "Poste", "Actif", "Rôle" };
        var rows = employes.Select(e => new object?[]
        {
            e.Matricule, e.Nom, e.Prenom, e.Email, e.Service, e.Poste,
            e.Actif == true ? "Oui" : "Non", e.Role
        });

        return ExportFile("employes", headers, rows, format);
    }

    [HttpGet("mouvements")]
    public async Task<IActionResult> ExportMouvements(string? format, string? q, int? typmov, int? localisationId, DateTime? from, DateTime? to, string? status,
        int? period, string? department, string? types)
    {
        var query = db.Mouvements.AsNoTracking()
            .Include(m => m.Materiel)
            .Include(m => m.Localisation)
            .Include(m => m.EmployeMatNavigation)
            .AsQueryable();

        var dashboardFiltered = (period.HasValue && period.Value > 0) ||
                                (!string.IsNullOrWhiteSpace(department) && department != "all") ||
                                !string.IsNullOrWhiteSpace(types);

        if (dashboardFiltered)
        {
            var typeIds = string.IsNullOrWhiteSpace(types)
                ? new List<int>()
                : types.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(int.Parse).ToList();

            var materialsQuery = db.Materials.AsNoTracking().AsQueryable();
            if (typeIds.Count > 0) materialsQuery = materialsQuery.Where(m => typeIds.Contains(m.TypeMaterielId));
            if (!string.IsNullOrWhiteSpace(department) && department != "all")
                materialsQuery = materialsQuery.Where(m => m.EmployeMatNavigation != null && m.EmployeMatNavigation.Service == department);

            var materialIds = await materialsQuery.Select(m => m.Id).ToListAsync();
            query = materialIds.Count > 0
                ? query.Where(m => materialIds.Contains(m.MaterielId))
                : query.Where(m => false);

            if (period.HasValue && period.Value > 0)
            {
                var rangeStart = DateTime.Today.AddDays(-(period.Value - 1));
                query = query.Where(m => m.DateMouvement >= rangeStart);
            }
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(q))
            {
                var needle = q.Trim().ToLower();
                query = query.Where(m =>
                    (m.Materiel != null && m.Materiel.Libelle.ToLower().Contains(needle)) ||
                    (m.Commentaires != null && m.Commentaires.ToLower().Contains(needle)));
            }
            if (typmov.HasValue) query = query.Where(m => m.Typmov == typmov.Value);
            if (localisationId.HasValue) query = query.Where(m => m.LocalisationId == localisationId.Value);
            if (from.HasValue) query = query.Where(m => m.DateMouvement >= from.Value);
            if (to.HasValue) query = query.Where(m => m.DateMouvement <= to.Value.Date.AddDays(1).AddTicks(-1));
            if (!string.IsNullOrWhiteSpace(status))
            {
                if (status == "completed")
                    query = query.Where(m => m.Statut == "Terminé" || m.Statut == "Termine" || m.Statut == null || m.Statut == "");
                else if (status == "pending")
                    query = query.Where(m => m.Statut != "Terminé" && m.Statut != "Termine" && m.Statut != null && m.Statut != "");
            }
        }

        var movements = await query.OrderByDescending(m => m.DateMouvement).ToListAsync();

        var typmovLabels = new Dictionary<int, string> { { 1, "Affectation" }, { 2, "Retour" }, { 3, "Transfert" }, { 4, "Intervention" } };
        var headers = new[] { "Date", "Matériel", "Type", "Employé", "Localisation", "Statut", "Commentaires" };
        var rows = movements.Select(m => new object?[]
        {
            m.DateMouvement,
            m.Materiel?.Libelle,
            typmovLabels.GetValueOrDefault(m.Typmov, m.Typmov.ToString()),
            m.EmployeMatNavigation is null ? "" : $"{m.EmployeMatNavigation.Prenom} {m.EmployeMatNavigation.Nom}",
            m.Localisation?.Nom,
            m.Statut ?? "Terminé",
            m.Commentaires
        });

        return ExportFile("mouvements", headers, rows, format);
    }

    [HttpGet("requests")]
    public async Task<IActionResult> ExportRequests(string? format, string? q, int? matricule, bool? my)
    {
        var headers = new[] { "Date", "Matériel", "Description", "Statut", "Demandeur" };
        var typmovRequest = await BuildRequestRowsAsync(q, matricule, my);
        return ExportFile("demandes", headers, typmovRequest, format);
    }

    [HttpGet("interventions")]
    public async Task<IActionResult> ExportInterventions(string? format, string? q)
    {
        var query = db.Interventions.AsNoTracking()
            .Include(i => i.Mat)
            .Include(i => i.Maint)
            .Include(i => i.Intervenant)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var needle = q.Trim().ToLower();
            query = query.Where(i =>
                (i.Commentaires != null && i.Commentaires.ToLower().Contains(needle)) ||
                (i.Mat != null && i.Mat.Libelle.ToLower().Contains(needle)));
        }

        var interventions = await query.OrderByDescending(i => i.Date).ToListAsync();

        var headers = new[] { "ID", "Date", "Matériel", "Mouvement", "Intervenant", "Commentaires" };
        var rows = interventions.Select(i => new object?[]
        {
            i.Id,
            i.Date.ToDateTime(TimeOnly.MinValue),
            i.Mat?.Libelle,
            i.MaintId,
            i.Intervenant is null ? "" : $"{i.Intervenant.Prenom} {i.Intervenant.Nom}",
            i.Commentaires
        });

        return ExportFile("interventions", headers, rows, format);
    }

    [HttpGet("localisations")]
    public async Task<IActionResult> ExportLocalisations(string? format)
    {
        var localisations = await db.Localistations.AsNoTracking().OrderBy(l => l.Id).ToListAsync();

        var headers = new[] { "ID", "Nom" };
        var rows = localisations.Select(l => new object?[] { l.Id, l.Nom });

        return ExportFile("localisations", headers, rows, format);
    }

    private async Task<List<object?[]>> BuildRequestRowsAsync(string? q, int? matricule, bool? my)
    {
        var movementQuery = db.Mouvements.AsNoTracking()
            .Include(m => m.Materiel)
            .Include(m => m.EmployeMatNavigation)
            .AsQueryable();

        if (my == true)
        {
            var adminMatricule = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            movementQuery = movementQuery.Where(m => m.Typmov == 4 && m.EmployeMat == adminMatricule);
        }
        else if (matricule.HasValue)
        {
            movementQuery = movementQuery.Where(m => m.Typmov == 4 && m.EmployeMat == matricule.Value);
        }
        else
        {
            movementQuery = movementQuery.Where(m => m.Typmov == 4 && m.Statut == "Pas encore intervenu");
        }

        if (!string.IsNullOrWhiteSpace(q))
        {
            var needle = q.Trim().ToLower();
            movementQuery = movementQuery.Where(m =>
                (m.Materiel != null && m.Materiel.Libelle.ToLower().Contains(needle)) ||
                (m.Commentaires != null && m.Commentaires.ToLower().Contains(needle)) ||
                (m.EmployeMatNavigation != null &&
                    (m.EmployeMatNavigation.Nom.ToLower().Contains(needle) ||
                     m.EmployeMatNavigation.Prenom.ToLower().Contains(needle))));
        }

        var movements = await movementQuery.OrderByDescending(m => m.DateMouvement).ToListAsync();

        return movements.Select(m => new object?[]
        {
            m.DateMouvement,
            m.Materiel?.Libelle,
            m.Commentaires,
            m.Statut ?? "Terminé",
            m.EmployeMatNavigation is null ? "" : $"{m.EmployeMatNavigation.Prenom} {m.EmployeMatNavigation.Nom}"
        }).ToList();
    }

    private IActionResult ExportFile(string entity, string[] headers, IEnumerable<object?[]> rows, string? format)
    {
        var isXlsx = string.Equals(format, "xlsx", StringComparison.OrdinalIgnoreCase);
        var extension = isXlsx ? "xlsx" : "csv";
        var contentType = isXlsx ? XlsxMime : CsvMime;
        var filename = $"sopal-gpi-{entity}-{DateTime.Now:yyyy-MM-dd}.{extension}";

        var bytes = isXlsx
            ? ExportService.BuildXlsx(entity, headers, rows)
            : ExportService.BuildCsv(headers, rows);

        return File(bytes, contentType, filename);
    }
}
