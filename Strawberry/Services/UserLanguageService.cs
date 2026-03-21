namespace Strawberry.Services;

public class UserLanguageService
{
    private string _language = "en";

    public string Language => _language;

    public event Func<Task>? OnChange;

    public async Task SetLanguageAsync(string language)
    {
        if (language != "en" && language != "nl") language = "en";
        if (_language == language) return;
        _language = language;
        if (OnChange != null)
        {
            var handlers = OnChange.GetInvocationList()
                .Cast<Func<Task>>()
                .Select(h => h());
            await Task.WhenAll(handlers);
        }
    }
}
