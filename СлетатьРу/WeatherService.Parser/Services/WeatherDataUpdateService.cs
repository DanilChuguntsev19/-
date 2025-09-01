using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using WeatherService.Parser.Models;

namespace WeatherService.Parser.Services;

public interface IClimateDataUpdateService
{
    Task UpdateClimateDataAsync();
}

public class ClimateDataUpdateService : BackgroundService, IClimateDataUpdateService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ClimateDataUpdateService> _logger;
    private readonly TimeSpan _updateInterval = TimeSpan.FromHours(6);

    private static readonly Dictionary<string, (double Latitude, double Longitude)> MetropolisCoordinates = new()
    {
        { "moscow", (55.7558, 37.6176) },
        { "petersburg", (59.9311, 30.3609) }
    };

    public ClimateDataUpdateService(IServiceProvider serviceProvider, ILogger<ClimateDataUpdateService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Сервис обновления климатических данных запущен");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await UpdateClimateDataAsync();
                _logger.LogInformation("Климатические данные успешно обновлены");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обновлении климатических данных");
            }

            await Task.Delay(_updateInterval, stoppingToken);
        }
    }

    public async Task UpdateClimateDataAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var climateProvider = scope.ServiceProvider.GetRequiredService<IClimateDataProvider>();

        var endDate = DateTime.Today.AddDays(-1);
        var startDate = endDate.AddDays(-30);

        foreach (var metropolis in MetropolisCoordinates)
        {
            try
            {
                _logger.LogInformation("Обновление данных для мегаполиса {Metropolis}", metropolis.Key);
                
                var dailyRecords = await climateProvider.RetrieveDailyRecordsAsync(
                    metropolis.Value.Latitude, 
                    metropolis.Value.Longitude, 
                    startDate, 
                    endDate);

                _logger.LogInformation("Получено {Count} дней данных для мегаполиса {Metropolis}", dailyRecords.Count, metropolis.Key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обновлении данных для мегаполиса {Metropolis}", metropolis.Key);
            }
        }
    }
}
