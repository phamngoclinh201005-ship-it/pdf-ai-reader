using System.ComponentModel.DataAnnotations;

namespace PaperFlow.Api.Contracts;

public class ReadingState
{
    [Range(1, 2000)] public int Page { get; set; } = 1;
    [Range(0.25, 4)] public double Zoom { get; set; } = 1;
    [MaxLength(200)] public List<SavedMessage> Messages { get; set; } = [];
    [MaxLength(100)] public List<SavedAction> Actions { get; set; } = [];
    [MaxLength(200)] public List<SavedHighlight> Highlights { get; set; } = [];
    [MaxLength(200)] public List<SavedBookmark> Bookmarks { get; set; } = [];
}
public class SavedBookmark
{
    [Required, MaxLength(64)] public string Id { get; set; } = "";
    [Range(1, 2000)] public int Page { get; set; }
    [Required, MaxLength(120)] public string Title { get; set; } = "";
}
public class SavedHighlight
{
    [Required, MaxLength(64)] public string Id { get; set; } = "";
    [Range(1, 2000)] public int Page { get; set; }
    [Required, MaxLength(5000)] public string Text { get; set; } = "";
    [MaxLength(2000)] public string Note { get; set; } = "";
    [Required, MinLength(1), MaxLength(200)] public List<HighlightRect> Rects { get; set; } = [];
}
public class HighlightRect
{
    [Range(0, 1)] public double X { get; set; }
    [Range(0, 1)] public double Y { get; set; }
    [Range(0.000001, 1)] public double Width { get; set; }
    [Range(0.000001, 1)] public double Height { get; set; }
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
