using System;
using System.Collections.Generic;

namespace SOPAL_GPI.Models;

public partial class Material
{
    public int Id { get; set; }

    public string? QrCode { get; set; }

    public string Libelle { get; set; } = null!;

    public string Marque { get; set; } = null!;

    public string Modele { get; set; } = null!;

    public string NumSerie { get; set; } = null!;

    public DateOnly DateAcquisition { get; set; }

    public bool Actif { get; set; }

    public string Commentaires { get; set; } = null!;

    public DateTime DateModif { get; set; }

    public int EtatId { get; set; }

    public int TypeMaterielId { get; set; }

    public int LocalisationId { get; set; }

    public int? EmployeMat { get; set; }

    public virtual Employe? EmployeMatNavigation { get; set; }

    public virtual Etatmat Etat { get; set; } = null!;

    public virtual ICollection<Intervention> Interventions { get; set; } = new List<Intervention>();

    public virtual Localistation Localisation { get; set; } = null!;

    public virtual ICollection<Mouvement> Mouvements { get; set; } = new List<Mouvement>();

    public virtual Matype TypeMateriel { get; set; } = null!;
}
