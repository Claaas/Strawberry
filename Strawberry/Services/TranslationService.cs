using System.Collections.Concurrent;
using System.Text.Json;

namespace Strawberry.Services;

public class TranslationService
{
    private readonly string _translationsRoot;
    private readonly ConcurrentDictionary<string, Dictionary<string, string>> _cache = new();

    public TranslationService(IWebHostEnvironment env)
    {
        _translationsRoot = Path.Combine(env.ContentRootPath, "Translations");
        LoadLanguage("en");
        LoadLanguage("nl");
    }

    public string T(string language, string key, params object[] args)
    {
        var dict = _cache.TryGetValue(language, out var d) ? d : _cache.GetValueOrDefault("en") ?? new();
        if (!dict.TryGetValue(key, out var value)) return key;
        return args.Length == 0 ? value : string.Format(value, args);
    }

    public void LoadLanguage(string language)
    {
        if (_cache.ContainsKey(language)) return;
        var path = Path.Combine(_translationsRoot, $"{language}.json");
        if (!File.Exists(path)) { _cache[language] = new(); return; }
        var json = File.ReadAllText(path);
        _cache[language] = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new();
    }
}
