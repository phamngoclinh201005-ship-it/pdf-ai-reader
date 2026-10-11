using System.ComponentModel.DataAnnotations;

namespace PaperFlow.Api.Contracts;

public class ReadingState
{
    [Range(1, 2000)] public int Page { get; set; } = 1;
    [Range(0.25, 4)] public double Zoom { get; set; } = 1;
    [MaxLength(200)] public List<SavedMessage> Messages { get; set; } = [];
    [MaxLength(100)] public List<SavedAction> Actions { get; set; } = [];
}
public class SavedMessage
{
    [Required, RegularExpression("^(user|assistant)$")] public string Role { get; set; } = "";
    [Required, MaxLength(12000)] public string Text { get; set; } = "";
    [MaxLength(2000)] public List<int> Citations { get; set; } = [];
}
public class SavedAction
{
    [Required, RegularExpression("^(translate|explain|summarize)$")] public string Mode { get; set; } = "translate";
    [Required, MaxLength(5000)] public string Source { get; set; } = "";
    [Required, MaxLength(12000)] public string Result { get; set; } = "";
    [Range(1, 2000)] public int Page { get; set; } = 1;
}
public record LibraryItem(string Id, string Name, long Size, DateTime UpdatedAt, int Page);
public record LibraryDetail(string Id, string Name, long Size, DateTime UpdatedAt, ReadingState State);
