using System;
using System.Collections.Generic;

namespace SOPAL_GPI.Models;

public partial class Etatmat
{
    public int Id { get; set; }

    public string Libelle { get; set; } = null!;

    public virtual ICollection<Material> Materials { get; set; } = new List<Material>();

    public virtual ICollection<Mouvement> Mouvements { get; set; } = new List<Mouvement>();
}
