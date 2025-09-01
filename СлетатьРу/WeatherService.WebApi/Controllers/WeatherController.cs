using Microsoft.AspNetCore.Mvc;
using WeatherService.Parser.Models;
using WeatherService.Parser.Services;
using CsvHelper;
using System.Globalization;

namespace WeatherService.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WeatherController : ControllerBase
{
    private readonly IClimateDataProvider _climateProvider;
    private readonly IClimateAnalyzer _climateAnalyzer;
    private readonly ILogger<WeatherController> _logger;

    private static readonly Dictionary<string, (double Latitude, double Longitude)> MetropolisCoordinates = new()
    {
        { "moscow", (55.7558, 37.6176) },
        { "petersburg", (59.9311, 30.3609) }
    };

    public WeatherController(
        IClimateDataProvider climateProvider,
        IClimateAnalyzer climateAnalyzer,
        ILogger<WeatherController> logger)
    {
        _climateProvider = climateProvider;
        _climateAnalyzer = climateAnalyzer;
        _logger = logger;
    }

    [HttpGet("metropolis/{metropolis}/report")]
    public async Task<ActionResult<MetropolisClimateReport>> GetMetropolisReport(string metropolis)
    {
        try
        {
            if (!MetropolisCoordinates.ContainsKey(metropolis.ToLower()))
            {
                return BadRequest("Поддерживаются только мегаполисы: moscow, petersburg");
            }

            var coordinates = MetropolisCoordinates[metropolis.ToLower()];
            var endDate = DateTime.Today;
            var startDate = endDate.AddMonths(-6);

            var dailyRecords = await _climateProvider.RetrieveDailyRecordsAsync(
                coordinates.Latitude, 
                coordinates.Longitude, 
                startDate, 
                endDate);

            var report = _climateAnalyzer.GenerateMetropolisReport(dailyRecords, metropolis);
            
            _logger.LogInformation("Сформирован отчет для мегаполиса {Metropolis}: {Days} дней", metropolis, dailyRecords.Count);
            
            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при формировании отчета для мегаполиса {Metropolis}", metropolis);
            return StatusCode(500, "Внутренняя ошибка сервера");
        }
    }

    [HttpGet("showdown")]
    public async Task<ActionResult<MetropolisShowdown>> ConductShowdown()
    {
        try
        {
            var endDate = DateTime.Today;
            var startDate = endDate.AddMonths(-6);

            var moscowCoordinates = MetropolisCoordinates["moscow"];
            var moscowRecords = await _climateProvider.RetrieveDailyRecordsAsync(
                moscowCoordinates.Latitude, 
                moscowCoordinates.Longitude, 
                startDate, 
                endDate);
            
            if (moscowRecords.Count == 0)
            {
                return StatusCode(503, "Не удалось получить данные для Москвы. Попробуйте позже.");
            }
            
            var moscowReport = _climateAnalyzer.GenerateMetropolisReport(moscowRecords, "Москва");

            var petersburgCoordinates = MetropolisCoordinates["petersburg"];
            var petersburgRecords = await _climateProvider.RetrieveDailyRecordsAsync(
                petersburgCoordinates.Latitude, 
                petersburgCoordinates.Longitude, 
                startDate, 
                endDate);
                
            if (petersburgRecords.Count == 0)
            {
                return StatusCode(503, "Не удалось получить данные для Санкт-Петербурга. Попробуйте позже.");
            }
            
            var petersburgReport = _climateAnalyzer.GenerateMetropolisReport(petersburgRecords, "Санкт-Петербург");

            var showdown = _climateAnalyzer.ConductMetropolisShowdown(moscowReport, petersburgReport);
            
            _logger.LogInformation("Проведен климатический поединок. Победитель: {Champion}", showdown.Champion);
            
            return Ok(showdown);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Ошибка сети при проведении поединка");
            return StatusCode(503, "Ошибка подключения к климатическому сервису. Проверьте интернет-соединение.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при проведении климатического поединка");
            return StatusCode(500, "Внутренняя ошибка сервера");
        }
    }

    [HttpGet("metropolis/{metropolis}/filter")]
    public async Task<ActionResult<MetropolisClimateReport>> FilterReport(
        string metropolis,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] ClimatePeriod? period)
    {
        try
        {
            if (!MetropolisCoordinates.ContainsKey(metropolis.ToLower()))
            {
                return BadRequest("Поддерживаются только мегаполисы: moscow, petersburg");
            }

            var coordinates = MetropolisCoordinates[metropolis.ToLower()];
            var endDate = to ?? DateTime.Today;
            var startDate = from ?? endDate.AddMonths(-6);

            var dailyRecords = await _climateProvider.RetrieveDailyRecordsAsync(
                coordinates.Latitude, 
                coordinates.Longitude, 
                startDate, 
                endDate);

            var filteredReport = _climateAnalyzer.FilterReportByCriteria(
                dailyRecords, metropolis, from, to, period);
            
            _logger.LogInformation("Сформирован отфильтрованный отчет для мегаполиса {Metropolis}", metropolis);
            
            return Ok(filteredReport);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при фильтрации отчета для мегаполиса {Metropolis}", metropolis);
            return StatusCode(500, "Внутренняя ошибка сервера");
        }
    }

    [HttpGet("metropolis/{metropolis}/export")]
    public async Task<IActionResult> ExportReport(string metropolis)
    {
        try
        {
            if (!MetropolisCoordinates.ContainsKey(metropolis.ToLower()))
            {
                return BadRequest("Поддерживаются только мегаполисы: moscow, petersburg");
            }

            var coordinates = MetropolisCoordinates[metropolis.ToLower()];
            var endDate = DateTime.Today;
            var startDate = endDate.AddMonths(-6);

            var dailyRecords = await _climateProvider.RetrieveDailyRecordsAsync(
                coordinates.Latitude, 
                coordinates.Longitude, 
                startDate, 
                endDate);

            var report = _climateAnalyzer.GenerateMetropolisReport(dailyRecords, metropolis);

            using var memoryStream = new MemoryStream();
            using var writer = new StreamWriter(memoryStream);
            using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

            csv.WriteField("Мегаполис");
            csv.WriteField("Климатический период");
            csv.WriteField("Средняя температура");
            csv.WriteField("Всего дней");
            csv.WriteField("Ясных дней");
            csv.WriteField("Дождливых дней");
            csv.WriteField("Процент ясных дней");
            csv.NextRecord();

            WritePeriodData(csv, report.MetropolisName, "Мороз", report.FrostPeriod);
            WritePeriodData(csv, report.MetropolisName, "Цветение", report.BloomPeriod);
            WritePeriodData(csv, report.MetropolisName, "Жара", report.HeatPeriod);
            WritePeriodData(csv, report.MetropolisName, "Листопад", report.FallPeriod);

            foreach (var decade in report.DecadeReports)
            {
                csv.WriteField(report.MetropolisName);
                csv.WriteField($"Десятилетие {decade.Decade}-е");
                csv.WriteField(decade.MeanTemperature.ToString("F1"));
                csv.WriteField(decade.TotalDays);
                csv.WriteField(decade.ClearSkyDays);
                csv.WriteField(decade.RainyDays);
                csv.WriteField(decade.ClearSkyRatio.ToString("F1"));
                csv.NextRecord();
            }

            writer.Flush();
            memoryStream.Position = 0;

            var fileName = $"climate_report_{metropolis}_{DateTime.Now:yyyyMMdd}.csv";
            
            _logger.LogInformation("Экспорт отчета для мегаполиса {Metropolis} завершен", metropolis);
            
            return File(memoryStream.ToArray(), "text/csv", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при экспорте отчета для мегаполиса {Metropolis}", metropolis);
            return StatusCode(500, "Внутренняя ошибка сервера");
        }
    }

    private static void WritePeriodData(CsvWriter csv, string metropolis, string periodName, PeriodMetrics metrics)
    {
        csv.WriteField(metropolis);
        csv.WriteField(periodName);
        csv.WriteField(metrics.MeanTemperature.ToString("F1"));
        csv.WriteField(metrics.TotalDays);
        csv.WriteField(metrics.ClearSkyDays);
        csv.WriteField(metrics.RainyDays);
        csv.WriteField(metrics.ClearSkyRatio.ToString("F1"));
        csv.NextRecord();
    }
}
