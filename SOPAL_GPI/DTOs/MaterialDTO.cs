namespace SOPAL_GPI.DTOs;

public class MaterialDTO
{
    public string Libelle { get; set; } = string.Empty;
    public string Marque { get; set; } = string.Empty;
    public string Modele { get; set; } = string.Empty;
    public string NumSerie { get; set; } = string.Empty;
    public DateOnly DateAcquisition { get; set; }
    public bool Actif { get; set; }
    public int TypeMaterielId { get; set; } = 1;
    public string Commentaires { get; set; } = string.Empty;
}
