using phonetics.Models;
using System.Text.Json;

namespace phonetics.Helpers;

public static class OutputParser
{
    public static OutputResponse? Parse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;

        var json = ExtractFirstJson(text);
        if (string.IsNullOrWhiteSpace(json)) return null;

        using var doc = JsonDocument.Parse(json);

        var alternativesArray = doc.RootElement.GetProperty("alternatives");
        string? alternatives = null;
        if (alternativesArray.ValueKind == JsonValueKind.Array && alternativesArray.GetArrayLength() > 0)
        {
            alternatives = alternativesArray!.ToString();
        }

        return new OutputResponse(
            NativeScript: doc.RootElement.GetProperty("native_script").GetString()!,
            Ethnicity: doc.RootElement.GetProperty("ethnicity").GetString()!,
            Confidence: doc.RootElement.GetProperty("confidence").GetDouble(),
            Alternatives: alternatives,
            TransliterationSuccessful: doc.RootElement.GetProperty("transliteration_successful").GetBoolean(),
            Details: doc.RootElement.GetProperty("details").GetString()!
        );
    }

    private static string? ExtractFirstJson(string input)
    {
        var start = input.IndexOf('{');
        var end = input.LastIndexOf('}');

        if (start == -1 || end == -1 || end <= start) return null;

        return input[start..(end + 1)];
    }
}
