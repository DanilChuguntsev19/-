namespace WeatherService.Parser.Models;

public class MetropolisClimateReport
{
    public string MetropolisName { get; set; } = string.Empty;
    public PeriodMetrics FrostPeriod { get; set; } = new();
    public PeriodMetrics BloomPeriod { get; set; } = new();
    public PeriodMetrics HeatPeriod { get; set; } = new();
    public PeriodMetrics FallPeriod { get; set; } = new();
    public List<DecadeMetrics> DecadeReports { get; set; } = new();
    public OverallMetrics Summary { get; set; } = new();
}

public class PeriodMetrics
{
    public ClimatePeriod Period { get; set; }
    public double MeanTemperature { get; set; }
    public int TotalDays { get; set; }
    public int ClearSkyDays { get; set; }
    public int RainyDays { get; set; }
    public double ClearSkyRatio => TotalDays > 0 ? (double)ClearSkyDays / TotalDays * 100 : 0;
    public double RainyRatio => TotalDays > 0 ? (double)RainyDays / TotalDays * 100 : 0;
}

public class DecadeMetrics
{
    public int Decade { get; set; }
    public double MeanTemperature { get; set; }
    public int TotalDays { get; set; }
    public int ClearSkyDays { get; set; }
    public int RainyDays { get; set; }
    public double ClearSkyRatio => TotalDays > 0 ? (double)ClearSkyDays / TotalDays * 100 : 0;
    public double RainyRatio => TotalDays > 0 ? (double)RainyDays / TotalDays * 100 : 0;
}

public class OverallMetrics
{
    public double MeanTemperature { get; set; }
    public int TotalDays { get; set; }
    public int ClearSkyDays { get; set; }
    public int RainyDays { get; set; }
    public double ClearSkyRatio => TotalDays > 0 ? (double)ClearSkyDays / TotalDays * 100 : 0;
    public double RainyRatio => TotalDays > 0 ? (double)RainyDays / TotalDays * 100 : 0;
}

public class MetropolisShowdown
{
    public string Champion { get; set; } = string.Empty;
    public string Verdict { get; set; } = string.Empty;
    public ShowdownDetails Breakdown { get; set; } = new();
}

public class ShowdownDetails
{
    public PeriodComparison FrostComparison { get; set; } = new();
    public PeriodComparison BloomComparison { get; set; } = new();
    public PeriodComparison HeatComparison { get; set; } = new();
    public PeriodComparison FallComparison { get; set; } = new();
    public string WarmerMetropolis { get; set; } = string.Empty;
    public string SunnierMetropolis { get; set; } = string.Empty;
}

public class PeriodComparison
{
    public ClimatePeriod Period { get; set; }
    public string WarmerMetropolis { get; set; } = string.Empty;
    public double TemperatureGap { get; set; }
}
