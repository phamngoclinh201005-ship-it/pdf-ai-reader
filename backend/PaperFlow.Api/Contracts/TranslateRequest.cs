namespace PaperFlow.Api.Contracts;

public class TranslateRequest
{
    public string Text { get; set; } = "";
    public string Mode { get; set; } = "translate";
}
