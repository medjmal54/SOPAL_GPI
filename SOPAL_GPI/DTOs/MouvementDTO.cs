using SOPAL_GPI.Models;

namespace SOPAL_GPI.DTOs;

public class MouvementDTO
{
    public int typmov { get; set; }
    public int materielId { get; set; }
    public int LocalisationId { get; set; }
    public int AncienEtatmatId { get; set; }
    public int NouvEtatmatId { get; set; }
    public int? EmployeMat { get; set; }
    public string? commentaires { get; set; } = string.Empty;
}
