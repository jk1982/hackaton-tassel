using phonetics.Models;

namespace phonetics.Helpers;

public static class PromptBuilder
{
    public static string BuildAnalysisPrompt(string name) =>
    $$$"""
Analyze the following transliterated name and do the following:
- Determine its most likely ethnic origin
- Analyze its ethnicity. If the name is from a language that has a distinct native script (e.g., Chinese, Hindi, Arabic, Vietnamese), convert it. If the name is from a language that uses the Latin alphabet (e.g., English, Spanish, German), or if you cannot confidently convert it, indicate that transliteration was not successful.

TransliteratedName: "{{{name}}}"

Provide the output as a JSON object with the following keys:
- "native_script": The converted name in its native script, or the original name if not converted.
- "transliteration_successful": A boolean (true/false) indicating if a meaningful conversion was performed.
- "details": A brief explanation of your decision.

Example 1 (Success):
Input Name: "Guanxiong"
Output: {{
  "native_script": "管雄",
  "ethnicity": "Chinese",
  "confidence": 0.95,
  "alternatives": ["South Asia"],
  "transliteration_successful": true,
  "details": "The name was converted to Chinese characters."
}}

Example 2 (Failure/Fallback):
Input Name: "John Smith"
Output: {{
  "native_script": "John Smith",
  "ethnicity": "English",
  "confidence": 0.9,
  "alternatives": ["German"],
  "transliteration_successful": false,
  "details": "The name is English and already in its native (Latin) script."
}}
""";

    public static string BuildRefinementPrompt(string original, OutputResponse response) =>
    $"""
Generate an improved native script name based on the output result from the previous analysis. Remove any honorifics or titles, and focus only on the core personal name. Consider the intonation and phonetics of the original name to create a more natural-sounding native script in the target language.

Respond with only the full optimized native name, and no additional explanation or formatting.

OriginalTransliteratedName: "{original}"
Ethnicity: "{response.Ethnicity}"
NativeScript: "{response.NativeScript}"
Alternatives: "{response.Alternatives}"

""";
}
