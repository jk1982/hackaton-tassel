namespace phonetics.Models;

public record NameRequestDto
{
    public string Name { get; init; } = string.Empty;
    public string VoiceId { get; init; } = string.Empty;
    public string Model { get; init; } = "chatgpt";
    public bool SinglePrompt { get; init; } = false;
}

