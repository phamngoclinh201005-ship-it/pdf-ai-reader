namespace PaperFlow.Api.Contracts;

public class ChatRequest
{
    public string Message { get; set; } = "";
    public string Context { get; set; } = "";
}
