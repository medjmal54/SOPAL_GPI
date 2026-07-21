using System;
using System.Collections.Generic;

namespace SOPAL_GPI.Models;

public partial class Matype
{
    public int Id { get; set; }

    public string Libelle { get; set; } = null!;

    public virtual ICollection<Material> Materials { get; set; } = new List<Material>();
}
