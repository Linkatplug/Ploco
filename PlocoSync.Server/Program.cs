using PlocoSync.Server.Hubs;
using PlocoSync.Server.Services;

var builder = WebApplication.CreateBuilder(args);

// Ajouter les services
builder.Services.AddSignalR();
builder.Services.AddSingleton<SessionManager>();
builder.Services.AddSingleton<StateStorageService>();

// Configurer CORS pour permettre les connexions depuis le client WPF
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// Configurer le pipeline HTTP
app.UseCors();

// Mapper le hub SignalR
app.MapHub<PlocoSyncHub>("/syncHub");

// Endpoints de santé et info
app.MapGet("/", () => new
{
    Service = "PlocoSync Server",
    Status = "Running",
    Version = "1.0.0",
    Timestamp = DateTime.UtcNow
});

app.MapGet("/health", () => new
{
    Status = "Healthy",
    Timestamp = DateTime.UtcNow
});

app.MapGet("/sessions", (SessionManager sessionManager) =>
{
    var sessions = sessionManager.GetAllSessions();
    return new
    {
        TotalSessions = sessions.Count,
        MasterId = sessionManager.GetCurrentMasterId(),
        Sessions = sessions.Select(s => new
        {
            s.UserId,
            s.UserName,
            s.IsMaster,
            s.ConnectedAt,
            s.LastHeartbeat
        })
    };
});

app.Run("http://*:5000");
