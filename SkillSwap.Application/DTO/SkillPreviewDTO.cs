using SkillSwap.Domain.Enums;

/// <summary>
/// Lightweight DTO for exposing skill names in match suggestions.
/// </summary>
public class SkillPreviewDTO
{
    public int Id { get; set; }
    public string Name { get; set; }
    public SkillCategory Category { get; set; }
}

