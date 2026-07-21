using System;
using System.Collections.Generic;

namespace SOPAL_GPI.Models;

public partial class Mouvement
{
    public int Id { get; set; }

    public DateTime DateMouvement { get; set; }

    public int Typmov { get; set; }

    public int MaterielId { get; set; }

    public int LocalisationId { get; set; }

    public int AncienEtatmatId { get; set; }

    public int NouvEtatmatId { get; set; }

    public int? EmployeMat { get; set; }

    public string? Commentaires { get; set; }

    public string? Statut { get; set; }

    public virtual Employe? EmployeMatNavigation { get; set; }

    public virtual ICollection<Intervention> Interventions { get; set; } = new List<Intervention>();

    public virtual Localistation Localisation { get; set; } = null!;

    public virtual Material Materiel { get; set; } = null!;

    public virtual Etatmat NouvEtatmat { get; set; } = null!;

    public virtual TypMov TypmovNavigation { get; set; } = null!;
}
