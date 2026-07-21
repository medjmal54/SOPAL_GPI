using System;
using System.Collections.Generic;

namespace SOPAL_GPI.Models;

public partial class Employe
{
    public int Matricule { get; set; }

    public string Nom { get; set; } = null!;

    public string Prenom { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string? Service { get; set; }

    public string Poste { get; set; } = null!;

    public bool? Actif { get; set; }

    public string? Role { get; set; }

    public virtual ICollection<Intervention> Interventions { get; set; } = new List<Intervention>();

    public virtual ICollection<Material> Materials { get; set; } = new List<Material>();

    public virtual ICollection<Mouvement> Mouvements { get; set; } = new List<Mouvement>();
}
