using WeatherService.Parser.Services;

var builder = WebApplication.CreateBuilder(args);

// Добавляем сервисы в контейнер
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Настройка CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Регистрация сервисов
builder.Services.AddHttpClient();
builder.Services.AddScoped<IClimateDataProvider, ClimateDataProvider>();
builder.Services.AddScoped<IClimateAnalyzer, ClimateAnalyzer>();
builder.Services.AddHostedService<ClimateDataUpdateService>();

// Настройка логирования
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.AddDebug();
});

var app = builder.Build();

// Настройка pipeline HTTP запросов
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

// Добавляем эндпоинт для проверки здоровья
app.MapGet("/health", () => "Climate Analysis Service is running!");

app.Run();
