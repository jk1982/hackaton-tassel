namespace phonetics.Models;

public record OutputResponse(string NativeScript, string Ethnicity, double Confidence, string Alternatives, bool TransliterationSuccessful, string Details, bool usesGemini = false, bool singlePrompt = false);
