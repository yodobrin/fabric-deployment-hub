namespace FabricDeploymentHub.Services.Utils;

public static class ItemContentProcessor
{
    public static string ReplacePlaceholders(
        string textContent,
        IDictionary<string, string> variables,
        ILogger logger
    )
    {
        logger.LogInformation("Starting placeholder replacements...");

        // Perform a single pass to replace all placeholders
        var (updatedText, replacedKeys, missedKeys) = ReplaceWithCombinedDictionary(
            textContent,
            variables,
            "# replaced during planning",
            logger
        );

        if (replacedKeys.Any())
        {
            logger.LogInformation($"Placeholders replaced: {string.Join(", ", replacedKeys)}");
        }
        else
        {
            logger.LogInformation("No placeholders replaced.");
        }

        if (missedKeys.Any())
        {
            logger.LogWarning(
                $"Placeholders not matched in content: {string.Join(", ", missedKeys)}"
            );
        }

        logger.LogInformation($"Total placeholders replaced: {replacedKeys.Count}");

        return updatedText;
    }

    private static (string, List<string>, List<string>) ReplaceWithCombinedDictionary(
        string content,
        IDictionary<string, string> variables,
        string commentSuffix,
        ILogger logger
    )
    {
        var replacedKeys = new List<string>();
        var missedKeys = new List<string>();

        foreach (var kvp in variables)
        {
            // Construct the placeholder using the new format
            string placeholder = $"{kvp.Key}"; // New format: _parameter_param_name
            logger.LogInformation("Checking placeholder: {Placeholder}", placeholder);

            if (content.Contains(placeholder))
            {
                // Inject replacement with appropriate comment
                string replacementValue = $"{kvp.Value} {commentSuffix}";
                content = content.Replace(placeholder, replacementValue);
                replacedKeys.Add(kvp.Key);
                logger.LogInformation(
                    "Replaced placeholder: {Placeholder} with value: {ReplacementValue}",
                    placeholder,
                    replacementValue
                );
            }
            else
            {
                // Log unmatched placeholders
                logger.LogWarning("Unmatched placeholder: {Placeholder}", placeholder);
                missedKeys.Add(placeholder);
            }
        }

        return (content, replacedKeys, missedKeys);
    }
}
