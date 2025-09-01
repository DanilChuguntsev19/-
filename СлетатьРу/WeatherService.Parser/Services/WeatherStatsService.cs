using System.Linq;
using WeatherService.Parser.Models;

namespace WeatherService.Parser.Services;

public interface IClimateAnalyzer
{
    MetropolisClimateReport GenerateMetropolisReport(List<DayRecord> dailyRecords, string metropolisName);
    MetropolisShowdown ConductMetropolisShowdown(MetropolisClimateReport moscowReport, MetropolisClimateReport petersburgReport);
    MetropolisClimateReport FilterReportByCriteria(List<DayRecord> dailyRecords, string metropolisName, DateTime? fromDate, DateTime? toDate, ClimatePeriod? period);
}

public class ClimateAnalyzer : IClimateAnalyzer
{
    public MetropolisClimateReport GenerateMetropolisReport(List<DayRecord> dailyRecords, string metropolisName)
    {
        var report = new MetropolisClimateReport { MetropolisName = metropolisName };

        report.FrostPeriod = CalculatePeriodMetrics(dailyRecords.Where(r => r.Period == ClimatePeriod.Frost).ToList(), ClimatePeriod.Frost);
        report.BloomPeriod = CalculatePeriodMetrics(dailyRecords.Where(r => r.Period == ClimatePeriod.Bloom).ToList(), ClimatePeriod.Bloom);
        report.HeatPeriod = CalculatePeriodMetrics(dailyRecords.Where(r => r.Period == ClimatePeriod.Heat).ToList(), ClimatePeriod.Heat);
        report.FallPeriod = CalculatePeriodMetrics(dailyRecords.Where(r => r.Period == ClimatePeriod.Fall).ToList(), ClimatePeriod.Fall);

        var yearGroups = dailyRecords.Select(r => r.YearGroup).Distinct().OrderBy(g => g);
        foreach (var yearGroup in yearGroups)
        {
            var groupRecords = dailyRecords.Where(r => r.YearGroup == yearGroup).ToList();
            var decadeReport = CalculateDecadeMetrics(groupRecords, yearGroup);
            report.DecadeReports.Add(decadeReport);
        }

        report.Summary = CalculateOverallMetrics(dailyRecords);

        return report;
    }

    public MetropolisShowdown ConductMetropolisShowdown(MetropolisClimateReport moscowReport, MetropolisClimateReport petersburgReport)
    {
        var showdown = new MetropolisShowdown();
        var details = new ShowdownDetails();

        details.FrostComparison = ComparePeriods(moscowReport.FrostPeriod, petersburgReport.FrostPeriod, "Москва", "Санкт-Петербург", ClimatePeriod.Frost);
        details.BloomComparison = ComparePeriods(moscowReport.BloomPeriod, petersburgReport.BloomPeriod, "Москва", "Санкт-Петербург", ClimatePeriod.Bloom);
        details.HeatComparison = ComparePeriods(moscowReport.HeatPeriod, petersburgReport.HeatPeriod, "Москва", "Санкт-Петербург", ClimatePeriod.Heat);
        details.FallComparison = ComparePeriods(moscowReport.FallPeriod, petersburgReport.FallPeriod, "Москва", "Санкт-Петербург", ClimatePeriod.Fall);

        details.WarmerMetropolis = moscowReport.Summary.MeanTemperature > petersburgReport.Summary.MeanTemperature ? "Москва" : "Санкт-Петербург";
        details.SunnierMetropolis = moscowReport.Summary.ClearSkyRatio > petersburgReport.Summary.ClearSkyRatio ? "Москва" : "Санкт-Петербург";

        var moscowScore = CalculateMetropolisScore(moscowReport);
        var petersburgScore = CalculateMetropolisScore(petersburgReport);

        if (moscowScore > petersburgScore)
        {
            showdown.Champion = "Москва";
            showdown.Verdict = "Москва одержала победу в климатическом поединке! Теплее, но порой капризно. Хотя поклонники Северной столицы наверняка не согласятся с таким вердиктом.";
        }
        else
        {
            showdown.Champion = "Санкт-Петербург";
            showdown.Verdict = "Санкт-Петербург выиграл по количеству ясных дней! Пасмурно, зато атмосферно. Хотя москвичи наверняка найдут что возразить.";
        }

        showdown.Breakdown = details;
        return showdown;
    }

    public MetropolisClimateReport FilterReportByCriteria(List<DayRecord> dailyRecords, string metropolisName, DateTime? fromDate, DateTime? toDate, ClimatePeriod? period)
    {
        var filteredRecords = dailyRecords.AsEnumerable();

        if (fromDate.HasValue)
            filteredRecords = filteredRecords.Where(r => r.DateStamp >= fromDate.Value);

        if (toDate.HasValue)
            filteredRecords = filteredRecords.Where(r => r.DateStamp <= toDate.Value);

        if (period.HasValue)
            filteredRecords = filteredRecords.Where(r => r.Period == period.Value);

        return GenerateMetropolisReport(filteredRecords.ToList(), metropolisName);
    }

    private PeriodMetrics CalculatePeriodMetrics(List<DayRecord> records, ClimatePeriod period)
    {
        if (!records.Any())
            return new PeriodMetrics { Period = period };

        return new PeriodMetrics
        {
            Period = period,
            MeanTemperature = records.Average(r => r.MeanTemp),
            TotalDays = records.Count,
            ClearSkyDays = records.Count(r => r.IsClearSky),
            RainyDays = records.Count(r => !r.IsClearSky)
        };
    }

    private DecadeMetrics CalculateDecadeMetrics(List<DayRecord> records, int decade)
    {
        if (!records.Any())
            return new DecadeMetrics { Decade = decade };

        return new DecadeMetrics
        {
            Decade = decade,
            MeanTemperature = records.Average(r => r.MeanTemp),
            TotalDays = records.Count,
            ClearSkyDays = records.Count(r => r.IsClearSky),
            RainyDays = records.Count(r => !r.IsClearSky)
        };
    }

    private OverallMetrics CalculateOverallMetrics(List<DayRecord> records)
    {
        if (!records.Any())
            return new OverallMetrics();

        return new OverallMetrics
        {
            MeanTemperature = records.Average(r => r.MeanTemp),
            TotalDays = records.Count,
            ClearSkyDays = records.Count(r => r.IsClearSky),
            RainyDays = records.Count(r => !r.IsClearSky)
        };
    }

    private PeriodComparison ComparePeriods(PeriodMetrics moscow, PeriodMetrics petersburg, string moscowName, string petersburgName, ClimatePeriod period)
    {
        var warmerMetropolis = moscow.MeanTemperature > petersburg.MeanTemperature ? moscowName : petersburgName;
        var temperatureGap = Math.Abs(moscow.MeanTemperature - petersburg.MeanTemperature);

        return new PeriodComparison
        {
            Period = period,
            WarmerMetropolis = warmerMetropolis,
            TemperatureGap = temperatureGap
        };
    }

    private double CalculateMetropolisScore(MetropolisClimateReport report)
    {
        var temperatureScore = (report.Summary.MeanTemperature + 30) / 60;
        var sunnyScore = report.Summary.ClearSkyRatio / 100;
        
        return temperatureScore * 0.6 + sunnyScore * 0.4;
    }
}
