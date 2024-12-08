namespace FabricDeploymentHub.Services.Utils;
public static class ItemContentProcessor
{
    /// <summary>
    /// Replaces placeholders in the given text with values from the provided dictionaries.
    /// </summary>
    /// <param name="textContent">The textual content to process.</param>
    /// <param name="parameters">Key-value pairs for parameter replacements.</param>
    /// <param name="secrets">Key-value pairs for secret replacements.</param>
    /// <param name="settings">Key-value pairs for setting replacements.</param>
    /// <returns>The text content with placeholders replaced.</returns>
    public static string ReplacePlaceholders(
        string textContent,
        IDictionary<string, string> parameters,
        IDictionary<string, string> secrets,
        IDictionary<string, string> settings,
        ILogger logger)
    {
        textContent = ReplaceWithDictionary(textContent, "{{parameter_", parameters);
        textContent = ReplaceWithDictionary(textContent, "{{secret_", secrets);
        textContent = ReplaceWithDictionary(textContent, "{{setting_", settings);
        return textContent;
    }

    private static string ReplaceWithDictionary(string content, string prefix, IDictionary<string, string> values)
    {
        foreach (var kvp in values)
        {
            string placeholder = $"{prefix}{kvp.Key}}}";
            content = content.Replace(placeholder, kvp.Value);
        }
        return content;
    }
}