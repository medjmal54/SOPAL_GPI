using System;
using System.Collections.Generic;

namespace SOPAL_GPI.Models;

public partial class Intervention
{
    public int Id { get; set; }

    public string? Commentaires { get; set; }

    public DateOnly Date { get; set; }

    public int MatId { get; set; }

    public int? IntervenantId { get; set; }

    public int MaintId { get; set; }

    public virtual Employe? Intervenant { get; set; }

    public virtual Mouvement Maint { get; set; } = null!;

    public virtual Material Mat { get; set; } = null!;
}
