using System;
using System.Collections.Generic;

namespace SOPAL_GPI.Models;

public partial class TypMov
{
    public int Id { get; set; }

    public string Nom { get; set; } = null!;

    public virtual ICollection<Mouvement> Mouvements { get; set; } = new List<Mouvement>();
}
