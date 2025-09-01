using System.Text.Json.Serialization;

namespace WeatherService.Parser.Models;

public class ClimateSnapshot
{
    [JsonPropertyName("latitude")]
    public double Latitude { get; set; }

    [JsonPropertyName("longitude")]
    public double Longitude { get; set; }

    [JsonPropertyName("daily")]
    public DailyMetrics Daily { get; set; } = new();

    [JsonPropertyName("daily_units")]
    public MetricUnits Units { get; set; } = new();
}

public class DailyMetrics
{
    [JsonPropertyName("time")]
    public List<string> TimeStamps { get; set; } = new();

    [JsonPropertyName("temperature_2m_max")]
    public List<double?> MaxTemp { get; set; } = new();

    [JsonPropertyName("temperature_2m_min")]
    public List<double?> MinTemp { get; set; } = new();

    [JsonPropertyName("precipitation_sum")]
    public List<double?> Rainfall { get; set; } = new();

    [JsonPropertyName("weathercode")]
    public List<int?> SkyCondition { get; set; } = new();
}

public class MetricUnits
{
    [JsonPropertyName("temperature_2m_max")]
    public string MaxTempUnit { get; set; } = string.Empty;

    [JsonPropertyName("temperature_2m_min")]
    public string MinTempUnit { get; set; } = string.Empty;

    [JsonPropertyName("precipitation_sum")]
    public string RainfallUnit { get; set; } = string.Empty;
}

public class DayRecord
{
    public DateTime DateStamp { get; set; }
    public double MaxTemp { get; set; }
    public double MinTemp { get; set; }
    public double MeanTemp => (MaxTemp + MinTemp) / 2;
    public double Rainfall { get; set; }
    public int SkyCondition { get; set; }
    public bool IsClearSky => Rainfall == 0;
    public ClimatePeriod Period => DeterminePeriod(DateStamp);
    public int YearGroup => GetYearGroup(DateStamp);

    private static ClimatePeriod DeterminePeriod(DateTime date)
    {
        var month = date.Month;
        return month switch
        {
            12 or 1 or 2 => ClimatePeriod.Frost,
            3 or 4 or 5 => ClimatePeriod.Bloom,
            6 or 7 or 8 => ClimatePeriod.Heat,
            9 or 10 or 11 => ClimatePeriod.Fall,
            _ => ClimatePeriod.Unknown
        };
    }

    private static int GetYearGroup(DateTime date)
    {
        var year = date.Year;
        return (year / 10) * 10;
    }
}

public enum ClimatePeriod
{
    Frost,
    Bloom,
    Heat,
    Fall,
    Unknown
}
