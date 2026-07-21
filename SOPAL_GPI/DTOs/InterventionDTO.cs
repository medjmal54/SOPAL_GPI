namespace SOPAL_GPI.DTOs;

public class InterventionDTO
{
    public string? Commentaires { get; set; }

    public DateOnly Date { get; set; }

    public int MatId { get; set; }

    public int? IntervenantId { get; set; }

    public int MaintId { get; set; }
}

public class BatchInterventionDTO
{
    public string? Commentaires { get; set; }

    public DateOnly Date { get; set; }

    public List<BatchInterventionItem> Items { get; set; } = new();
}

public class BatchInterventionItem
{
    public int MatId { get; set; }

    public int MaintId { get; set; }
}
