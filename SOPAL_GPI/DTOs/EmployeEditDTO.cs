namespace SOPAL_GPI.DTOs;

public class EmployeEditDTO
{
    public string Nom { get; set; } = string.Empty;
    public string Prenom { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Service { get; set; } = string.Empty;
    public string? Poste { get; set; } = string.Empty;
    public bool? Actif {  get; set; }
    public string Role {  get; set; } = string.Empty;
}
