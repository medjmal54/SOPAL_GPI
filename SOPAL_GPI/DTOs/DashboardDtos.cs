using SOPAL_GPI.Models;

namespace SOPAL_GPI.DTOs;

public class DashboardFilter
{
    public int Period { get; set; } = 30;
    public string Department { get; set; } = "all";
    public List<int> TypeIds { get; set; } = new();
}

public class MetricDto
{
    public string Label { get; set; } = string.Empty;
    public int Value { get; set; }
    public string Icon { get; set; } = string.Empty;
    public string Tone { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;
}

public class ChartDatasetDto
{
    public string Label { get; set; } = string.Empty;
    public List<int> Data { get; set; } = new();
    public string BackgroundColor { get; set; } = string.Empty;
    public int BorderRadius { get; set; } = 3;
    public int MaxBarThickness { get; set; } = 28;
}

public class DashboardChartDto
{
    public List<string> Labels { get; set; } = new();
    public List<ChartDatasetDto> Datasets { get; set; } = new();
}

public class DistributionItemDto
{
    public int Id { get; set; }
    public string Label { get; set; } = string.Empty;
    public int Value { get; set; }
}

public class DashboardData
{
    public List<MetricDto> Metrics { get; set; } = new();
    public DashboardChartDto MovementChart { get; set; } = new();
    public List<DistributionItemDto> Distribution { get; set; } = new();
    public int CategoryCount { get; set; }
    public int FilteredMaterialsTotal { get; set; }
    public List<string> Departments { get; set; } = new();
    public List<Matype> Types { get; set; } = new();
}
