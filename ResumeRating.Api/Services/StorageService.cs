using Newtonsoft.Json;

namespace ResumeRating.Api.Services;

public interface IStorageService
{
    Task<T?> LoadAsync<T>(string key) where T : class;
    Task SaveAsync<T>(string key, T data) where T : class;
    Task<List<T>> LoadListAsync<T>(string collectionName) where T : class;
    Task AppendToListAsync<T>(string collectionName, T item) where T : class;
    Task SaveListAsync<T>(string collectionName, List<T> items) where T : class;
    Task SaveFileAsync(string fileName, Stream content);
    string GetUploadPath(string fileName);
}

public class FileStorageService : IStorageService
{
    private readonly string _basePath;
    private readonly string _uploadsPath;

    public FileStorageService(IConfiguration configuration)
    {
        var configPath = configuration.GetValue<string>("Storage:DataPath");
        _basePath = string.IsNullOrWhiteSpace(configPath) ? Path.Combine(Directory.GetCurrentDirectory(), "App_Data") : configPath;
        _uploadsPath = Path.Combine(_basePath, "Uploads");
        Directory.CreateDirectory(_basePath);
        Directory.CreateDirectory(_uploadsPath);
    }

    public async Task<T?> LoadAsync<T>(string key) where T : class
    {
        var filePath = GetFilePath(key);
        if (!File.Exists(filePath)) return null;
        var json = await File.ReadAllTextAsync(filePath);
        return JsonConvert.DeserializeObject<T>(json);
    }

    public async Task SaveAsync<T>(string key, T data) where T : class
    {
        var filePath = GetFilePath(key);
        var json = JsonConvert.SerializeObject(data, Formatting.Indented);
        await File.WriteAllTextAsync(filePath, json);
    }

    public async Task<List<T>> LoadListAsync<T>(string collectionName) where T : class
    {
        var filePath = GetFilePath(collectionName);
        if (!File.Exists(filePath)) return [];
        var json = await File.ReadAllTextAsync(filePath);
        return JsonConvert.DeserializeObject<List<T>>(json) ?? [];
    }

    public async Task AppendToListAsync<T>(string collectionName, T item) where T : class
    {
        var list = await LoadListAsync<T>(collectionName);
        list.Add(item);
        await SaveAsync(collectionName, list);
    }

    public async Task SaveListAsync<T>(string collectionName, List<T> items) where T : class
    {
        await SaveAsync(collectionName, items);
    }

    public async Task SaveFileAsync(string fileName, Stream content)
    {
        var sanitized = SanitizeFileName(fileName);
        var filePath = Path.Combine(_uploadsPath, sanitized);
        using var fileStream = new FileStream(filePath, FileMode.Create);
        await content.CopyToAsync(fileStream);
    }

    public string GetUploadPath(string fileName)
    {
        var sanitized = SanitizeFileName(fileName);
        return Path.Combine(_uploadsPath, sanitized);
    }

    private string GetFilePath(string key)
    {
        var sanitized = SanitizeFileName(key);
        return Path.Combine(_basePath, $"{sanitized}.json");
    }

    private static string SanitizeFileName(string fileName)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return string.Join("_", fileName.Split(invalid, StringSplitOptions.RemoveEmptyEntries));
    }
}
