using System.Text.Json;
using Microsoft.Extensions.Logging;
using WeatherService.Parser.Models;

namespace WeatherService.Parser.Services;

public interface IClimateDataProvider
{
    Task<ClimateSnapshot> FetchHistoricalClimateAsync(double latitude, double longitude, DateTime startDate, DateTime endDate);
    Task<List<DayRecord>> RetrieveDailyRecordsAsync(double latitude, double longitude, DateTime startDate, DateTime endDate);
}

public class ClimateDataProvider : IClimateDataProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ClimateDataProvider> _logger;
    private const string ApiEndpoint = "https://archive-api.open-meteo.com/v1/archive";

    public ClimateDataProvider(HttpClient httpClient, ILogger<ClimateDataProvider> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<ClimateSnapshot> FetchHistoricalClimateAsync(
        double latitude,
        double longitude,
        DateTime startDate,
        DateTime endDate)
    {
        try
        {
            if (startDate > DateTime.Now || endDate > DateTime.Now)
            {
                endDate = DateTime.Now;
                startDate = endDate.AddDays(-30);
            }

            var requestUrl = $"{ApiEndpoint}?latitude={latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}" +
                            $"&longitude={longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}" +
                            $"&start_date={startDate:yyyy-MM-dd}" +
                            $"&end_date={endDate:yyyy-MM-dd}" +
                            $"&daily=temperature_2m_max,temperature_2m_min,precipitation_sum,weathercode";

            _logger.LogInformation("Отправка запроса к Climate API: {Url}", requestUrl);

            var response = await _httpClient.GetAsync(requestUrl);
            response.EnsureSuccessStatusCode();

            var jsonResponse = await response.Content.ReadAsStringAsync();
            _logger.LogDebug("Получен ответ от API: {Json}", jsonResponse);

            var climateData = JsonSerializer.Deserialize<ClimateSnapshot>(jsonResponse);

            if (climateData == null)
                throw new InvalidOperationException("Не удалось обработать данные о климате");

            return climateData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении климатических данных для координат {Latitude}, {Longitude}", latitude, longitude);
            throw;
        }
    }

    public async Task<List<DayRecord>> RetrieveDailyRecordsAsync(
        double latitude,
        double longitude,
        DateTime startDate,
        DateTime endDate)
    {
        var climateData = await FetchHistoricalClimateAsync(latitude, longitude, startDate, endDate);
        var dailyRecords = new List<DayRecord>();

        for (int i = 0; i < climateData.Daily.TimeStamps.Count; i++)
        {
            if (DateTime.TryParse(climateData.Daily.TimeStamps[i], out var date))
            {
                var dayRecord = new DayRecord
                {
                    DateStamp = date,
                    MaxTemp = climateData.Daily.MaxTemp?[i] ?? 0,
                    MinTemp = climateData.Daily.MinTemp?[i] ?? 0,
                    Rainfall = climateData.Daily.Rainfall?[i] ?? 0,
                    SkyCondition = climateData.Daily.SkyCondition?[i] ?? 0
                };

                dailyRecords.Add(dayRecord);
            }
        }

        return dailyRecords;
    }
}
