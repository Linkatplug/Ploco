using System.Text.Json;

namespace PlocoSync.Server.Services;

/// <summary>
/// Service for storing and retrieving the shared application state (database snapshot)
/// </summary>
public class StateStorageService
{
    private readonly string _storagePath;
    private readonly string _stateFilePath;
    private readonly string _metadataFilePath;
    private readonly ILogger<StateStorageService> _logger;

    public StateStorageService(IConfiguration configuration, ILogger<StateStorageService> logger)
    {
        _logger = logger;
        
        // Get storage path from configuration or use default
        _storagePath = configuration["StateStorage:Path"] 
            ?? Path.Combine(Directory.GetCurrentDirectory(), "StateStorage");
        
        _stateFilePath = Path.Combine(_storagePath, "shared_ploco.db");
        _metadataFilePath = Path.Combine(_storagePath, "state_metadata.json");
        
        // Ensure storage directory exists
        EnsureStorageDirectory();
    }

    private void EnsureStorageDirectory()
    {
        if (!Directory.Exists(_storagePath))
        {
            Directory.CreateDirectory(_storagePath);
            _logger.LogInformation($"Created state storage directory: {_storagePath}");
        }
    }

    /// <summary>
    /// Gets the current shared state (database snapshot) from storage
    /// </summary>
    /// <returns>Database bytes or null if no state exists</returns>
    public async Task<byte[]?> GetStateAsync()
    {
        try
        {
            if (!File.Exists(_stateFilePath))
            {
                _logger.LogInformation("No state file exists yet");
                return null;
            }

            var bytes = await File.ReadAllBytesAsync(_stateFilePath);
            _logger.LogInformation($"Loaded state: {bytes.Length} bytes");
            return bytes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load state");
            throw;
        }
    }

    /// <summary>
    /// Saves the shared state (database snapshot) to storage
    /// </summary>
    /// <param name="dbBytes">Database file bytes</param>
    /// <param name="userName">User who saved the state</param>
    public async Task SaveStateAsync(byte[] dbBytes, string userName)
    {
        try
        {
            EnsureStorageDirectory();

            // Save database file
            await File.WriteAllBytesAsync(_stateFilePath, dbBytes);
            _logger.LogInformation($"Saved state: {dbBytes.Length} bytes from user {userName}");

            // Save metadata
            var metadata = new StateMetadata
            {
                LastSavedUtc = DateTime.UtcNow,
                SavedBy = userName,
                SizeBytes = dbBytes.Length,
                Version = "1.0"
            };

            var metadataJson = JsonSerializer.Serialize(metadata, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
            await File.WriteAllTextAsync(_metadataFilePath, metadataJson);
            
            _logger.LogInformation($"Saved metadata for user {userName}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to save state from user {userName}");
            throw;
        }
    }

    /// <summary>
    /// Gets the metadata about the current state
    /// </summary>
    public async Task<StateMetadata?> GetMetadataAsync()
    {
        try
        {
            if (!File.Exists(_metadataFilePath))
            {
                return null;
            }

            var json = await File.ReadAllTextAsync(_metadataFilePath);
            return JsonSerializer.Deserialize<StateMetadata>(json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load metadata");
            return null;
        }
    }

    /// <summary>
    /// Checks if a state exists
    /// </summary>
    public bool StateExists()
    {
        return File.Exists(_stateFilePath);
    }

    /// <summary>
    /// Deletes the current state (for testing/reset purposes)
    /// </summary>
    public async Task DeleteStateAsync()
    {
        try
        {
            if (File.Exists(_stateFilePath))
            {
                File.Delete(_stateFilePath);
                _logger.LogInformation("Deleted state file");
            }

            if (File.Exists(_metadataFilePath))
            {
                File.Delete(_metadataFilePath);
                _logger.LogInformation("Deleted metadata file");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete state");
            throw;
        }
    }
}

/// <summary>
/// Metadata about the stored state
/// </summary>
public class StateMetadata
{
    public DateTime LastSavedUtc { get; set; }
    public string SavedBy { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string Version { get; set; } = string.Empty;
}
