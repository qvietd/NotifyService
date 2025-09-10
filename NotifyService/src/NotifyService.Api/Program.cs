using NotifyService.Api.Middleware;
using NotifyService.Application;
using NotifyService.Infrastructure;
using NotifyService.Infrastructure.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Configuration
builder.Services.Configure<RabbitMQConfig>(builder.Configuration.GetSection("RabbitMQ"));
builder.Services.Configure<MongoDBConfig>(builder.Configuration.GetSection("MongoDB"));
builder.Services.Configure<RedisConfig>(builder.Configuration.GetSection("Redis"));
builder.Services.Configure<EmailConfig>(builder.Configuration.GetSection("Email"));

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add Core services
builder.Services.AddApplication();

// Add Infrastructure services
builder.Services.AddInfrastructure(builder.Configuration);

// Add SignalR
builder.Services.AddSignalR();

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
app.MapHub<NotificationHub>("/notificationHub");

app.Run();