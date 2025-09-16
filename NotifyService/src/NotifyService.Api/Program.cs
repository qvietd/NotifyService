using Microsoft.Extensions.Options;
using NotifyService.Api.Hubs;
using NotifyService.Application;
using NotifyService.Infrastructure;
using NotifyService.Infrastructure.Configuration;
using NotifyService.NotifyService.Api.HealthCheck;

var builder = WebApplication.CreateBuilder(args);
// Add Core services
builder.Services.AddApplication();

// Add Infrastructure services
builder.Services.AddInfrastructure(builder.Configuration);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
var redisConfig = builder.Configuration.GetSection("Redis").Get<RedisConfig>();
// Add SignalR
builder.Services.AddSignalR().AddStackExchangeRedis(redisConfig.ConnectionString);

builder.Services.AddHealthChecks()
    .AddCheck<RabbitMQHealthCheck>("rabbitmq")
    .AddCheck<MongoDBHealthCheck>("mongodb")
    .AddCheck<RedisHealthCheck>("redis");

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.WithOrigins("http://127.0.0.1:5500")
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
    });
});

var app = builder.Build();

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");
app.UseHttpsRedirection();
app.UseRouting();

app.MapControllers();
app.MapHealthChecks("/health");
app.MapHub<NotificationHub>("/notificationHub");

app.Run();