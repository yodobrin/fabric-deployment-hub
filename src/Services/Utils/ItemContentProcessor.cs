using System.Text.RegularExpressions;

namespace FabricDeploymentHub.Services.Utils;

public static class ItemContentProcessor
{
    public static MetaSection ExtractMetadataFromContent(string content, ILogger logger)
    {
        try
        {
            // Extract the META block using regex
            var metaBlockMatch = Regex.Match(
                content,
                @"# META \{[\s\S]*?# META \}",
                RegexOptions.Multiline
            );

            if (!metaBlockMatch.Success)
            {
                logger.LogWarning("No valid META block found in the content.");
                throw new JsonException("META block not found.");
            }

            // Remove the "# META" prefix from each line and clean up the JSON block
            var metaJson = string.Join(
                "\n",
                metaBlockMatch.Value
                    .Split('\n')
                    .Select(line => line.Replace("# META", "").Trim())
            );

            logger.LogInformation("Extracted META JSON: {MetaJson}", metaJson);

            // Deserialize the JSON into the MetaSection class
            var metadata = JsonSerializer.Deserialize<MetaSection>(
                metaJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            if (metadata == null)
            {
                logger.LogError("Deserialization resulted in a null MetaSection object.");
                throw new JsonException("Deserialization failed.");
            }

            return metadata;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to extract metadata from content.");
            throw; // Re-throw the exception for the caller to handle
        }
    }

    public static string InjectMetadataIntoContent(
        string content,
        MetaSection updatedMetadata,
        ILogger logger
    )
    {
        try
        {
            // Serialize the updated metadata back to JSON
            var updatedMetadataJson = JsonSerializer.Serialize(
                updatedMetadata,
                new JsonSerializerOptions { 
                    WriteIndented = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull 
                    }
            );

            // Format the JSON with the "# META" prefix
            var formattedMetadata = string.Join(
                "\n",
                updatedMetadataJson.Split('\n').Select(line => $"# META {line}")
            );

            // Regex pattern to match only the first # META block
            string pattern = @"# META \{\s*[\s\S]*?# META \}";

            // Use Regex.Match to find the first occurrence of the # META section
            var match = Regex.Match(content, pattern, RegexOptions.Multiline);

            if (!match.Success)
            {
                logger.LogWarning("No valid META block found in the content.");
                throw new InvalidOperationException("META block not found.");
            }

            // Replace the first # META section with the formatted metadata
            string updatedContent = content.Replace(match.Value, formattedMetadata);

            return updatedContent;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to inject metadata into content.");
            throw;
        }
    }

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
