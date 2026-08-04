using SOPAL_GPI.Models;

namespace SOPAL_GPI.DTOs;

public class PaginatedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int Total { get; set; }
}

public class MouvementSearchResult
{
    public List<Mouvement> Items { get; set; } = new();
    public int Total { get; set; }
    public int TotalCompleted { get; set; }
    public int TotalPending { get; set; }
}

public class EmployeListItemDto
{
    public int Matricule { get; set; }
    public string Nom { get; set; } = string.Empty;
    public string Prenom { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Service { get; set; }
    public string Poste { get; set; } = string.Empty;
    public bool? Actif { get; set; }
    public string? Role { get; set; }
    public int MaterialCount { get; set; }
}
