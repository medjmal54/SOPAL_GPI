using Microsoft.EntityFrameworkCore;
using SOPAL_GPI.Data;
using SOPAL_GPI.DTOs;
using SOPAL_GPI.Models;
using System.Globalization;
using System.Text.RegularExpressions;

namespace SOPAL_GPI.Services;

public class DashboardRepository(GpiContext db) : IDashboardRepository
{
    public async Task<DashboardData> GetDashboardAsync(DashboardFilter filter)
    {
        var types = await db.Matypes.AsNoTracking().OrderBy(t => t.Id).ToListAsync();
        var departments = await db.Employes.AsNoTracking()
            .Where(e => e.Service != null)
            .Select(e => e.Service!)
            .Distinct()
            .OrderBy(s => s)
            .ToListAsync();

        var selectedTypes = filter.TypeIds.Count > 0 ? filter.TypeIds : types.Select(t => t.Id).ToList();

        var materialsQuery = db.Materials.AsNoTracking()
            .Include(m => m.Etat)
            .AsQueryable()
            .Where(m => selectedTypes.Contains(m.TypeMaterielId));

        if (!string.Equals(filter.Department, "all", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(filter.Department))
            materialsQuery = materialsQuery.Where(m => m.EmployeMatNavigation != null && m.EmployeMatNavigation.Service == filter.Department);

        var materials = await materialsQuery.ToListAsync();
        var total = materials.Count;

        var assigned = materials.Count(m => m.EmployeMat != null);
        var broken = materials.Count(IsBroken);
        var maintenance = materials.Count(IsMaintenance);
        var stock = materials.Count(m => m.EmployeMat == null && !IsMaintenance(m) && !IsBroken(m));

        var categoryCount = materials.Select(m => m.TypeMaterielId).Distinct().Count();
        Func<int, string> percent = count => total > 0 ? $"{Math.Round(count / (double)total * 100)}% du total" : "0% du total";

        var metrics = new List<MetricDto>
        {
            new() { Label = "Total mat\u00e9riels", Value = total, Icon = "account_tree", Tone = "blue", Note = $"{categoryCount} cat\u00e9gorie{(categoryCount != 1 ? "s" : "")}" },
            new() { Label = "En stock", Value = stock, Icon = "inventory_2", Tone = "green", Note = percent(stock) },
            new() { Label = "Affect\u00e9s", Value = assigned, Icon = "person_pin_circle", Tone = "blue", Note = percent(assigned) },
            new() { Label = "Maintenance", Value = maintenance, Icon = "build_circle", Tone = "amber", Note = maintenance > 0 ? $"{maintenance} \u00e0 surveiller" : "Aucune alerte" },
            new() { Label = "Hors service", Value = broken, Icon = "cancel", Tone = "red", Note = broken > 0 ? "Contr\u00f4le requis" : "Aucun mat\u00e9riel" }
        };

        var materialIds = materials.Select(m => m.Id).ToList();
        DateTime? rangeStart = filter.Period > 0 ? DateTime.Today.AddDays(-(filter.Period - 1)) : null;

        var movementsQuery = db.Mouvements.AsNoTracking().AsQueryable();
        if (materialIds.Count > 0)
            movementsQuery = movementsQuery.Where(m => materialIds.Contains(m.MaterielId));
        else
            movementsQuery = movementsQuery.Where(m => false);
        if (rangeStart.HasValue)
            movementsQuery = movementsQuery.Where(m => m.DateMouvement >= rangeStart.Value);

        var movements = await movementsQuery.ToListAsync();

        var chart = BuildMovementChart(movements, rangeStart);

        var distribution = types
            .Select(t => new DistributionItemDto { Id = t.Id, Label = t.Libelle, Value = materials.Count(m => m.TypeMaterielId == t.Id) })
            .Where(entry => entry.Value > 0)
            .OrderByDescending(entry => entry.Value)
            .ToList();

        return new DashboardData
        {
            Metrics = metrics,
            MovementChart = chart,
            Distribution = distribution,
            CategoryCount = categoryCount,
            FilteredMaterialsTotal = total,
            Departments = departments,

        };
    }

    private static DashboardChartDto BuildMovementChart(List<Mouvement> movements, DateTime? rangeStart)
    {
        DateTime start = rangeStart ?? DateTime.Today.AddDays(-6);
        DateTime end = DateTime.Today;
        if (rangeStart.HasValue && rangeStart.Value > end) end = rangeStart.Value;
        TimeSpan totalSpan = end - start;
        TimeSpan step = totalSpan.TotalDays <= 0 ? TimeSpan.FromDays(1) : totalSpan / 7.0;
        if (step < TimeSpan.FromHours(1)) step = TimeSpan.FromDays(1);

        var affectations = new int[7];
        var retours = new int[7];
        foreach (var m in movements)
        {
            var idx = FindBucket(m.DateMouvement.Date, start.Date, step, end.Date);
            if (idx < 0 || idx > 6) continue;
            if (m.Typmov == 1) affectations[idx]++;
            if (m.Typmov == 2) retours[idx]++;
        }

        var labels = new List<string>(7);
        for (var i = 0; i < 7; i++)
        {
            var bucketStart = start.Add(step * i);
            labels.Add(bucketStart.ToString("dd MMM", CultureInfo.GetCultureInfo("fr-FR")));
        }

        return new DashboardChartDto
        {
            Labels = labels,
            Datasets =
            {
                new ChartDatasetDto { Label = "Affectations", Data = affectations.ToList(), BackgroundColor = "#9bbcff", BorderRadius = 3, MaxBarThickness = 28 },
                new ChartDatasetDto { Label = "Retours", Data = retours.ToList(), BackgroundColor = "#ffaaa6", BorderRadius = 3, MaxBarThickness = 28 }
            }
        };
    }

    private static int FindBucket(DateTime date, DateTime start, TimeSpan step, DateTime end)
    {
        for (var i = 0; i < 7; i++)
        {
            var bucketStart = start.Add(step * i);
            var bucketEnd = i == 6 ? end : start.Add(step * (i + 1));
            if (date >= bucketStart && (i == 6 ? date <= bucketEnd : date < bucketEnd)) return i;
        }
        return -1;
    }

    private static bool IsBroken(Material material) =>
        material.Actif == false ||
        Regex.IsMatch(material.Etat?.Libelle ?? "", @"hors|rebut|d\u00e9fect|defect|panne|irr\u00e9cup|irrecup", RegexOptions.IgnoreCase);

    private static bool IsMaintenance(Material material) =>
        !IsBroken(material) &&
        Regex.IsMatch(material.Etat?.Libelle ?? "", @"maint|r\u00e9par|repar|intervention|diagnostic", RegexOptions.IgnoreCase);
}

public interface IDashboardRepository
{
    Task<DashboardData> GetDashboardAsync(DashboardFilter filter);
}

