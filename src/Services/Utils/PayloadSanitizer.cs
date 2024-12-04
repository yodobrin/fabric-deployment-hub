using System.Text.Json.Nodes;

namespace FabricDeploymentHub.Services.Utils;

public static class PayloadSanitizer
{
    /// <summary>
    /// Recursively sanitizes an object by redacting specific fields.
    /// </summary>
    /// <param name="payload">The object to sanitize.</param>
    /// <param name="fieldsToRedact">A list of field names to redact.</param>
    /// <returns>A sanitized object.</returns>
    public static object Sanitize(object payload, IEnumerable<string> fieldsToRedact)
    {
        try
        {
            // Serialize the payload to JSON
            var json = JsonSerializer.Serialize(payload);

            // Parse JSON into a mutable JSON object
            var root = JsonNode.Parse(json);

            // Apply sanitization
            SanitizeNode(root, fieldsToRedact);

            // Return the sanitized JSON object
            return root!;
        }
        catch (Exception ex)
        {
            // If sanitization fails, return the original payload as a fallback
            Console.WriteLine($"Sanitization failed: {ex.Message}");
            return payload;
        }
    }

    /// <summary>
    /// Recursively sanitizes a JSON node.
    /// </summary>
    /// <param name="node">The JSON node to sanitize.</param>
    /// <param name="fieldsToRedact">A list of field names to redact.</param>
    private static void SanitizeNode(JsonNode? node, IEnumerable<string> fieldsToRedact)
    {
        if (node is JsonObject jsonObject)
        {
            foreach (var field in fieldsToRedact)
            {
                if (jsonObject.ContainsKey(field))
                {
                    jsonObject[field] = "<redacted>";
                }
            }

            // Recurse into child objects
            foreach (var child in jsonObject)
            {
                SanitizeNode(child.Value, fieldsToRedact);
            }
        }
        else if (node is JsonArray jsonArray)
        {
            // Recurse into array elements
            foreach (var element in jsonArray)
            {
                SanitizeNode(element, fieldsToRedact);
            }
        }
    }
}