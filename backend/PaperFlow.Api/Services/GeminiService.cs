
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace PaperFlow.Api.Services;

public class GeminiService
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;

    public GeminiService(
        HttpClient http,
        IConfiguration config)
    {
        _http = http;
        _config = config;
    }

    public async Task<string> GenerateAsync(
        string prompt,
        CancellationToken cancellationToken = default)
    {
        var apiKey = _config["Gemini:ApiKey"];
        var model = _config["Gemini:Model"];

        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException(
                "Gemini API key is not configured.");

        if (string.IsNullOrWhiteSpace(model))
            throw new InvalidOperationException(
                "Gemini model is not configured.");

        var url =
            $"https://generativelanguage.googleapis.com/v1beta/models/{Uri.EscapeDataString(model)}:generateContent";

        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    role = "user",
                    parts = new[] { new { text = prompt } }
                }
            },
            generationConfig = new
            {
                temperature = 0.2
            }
        };

        using var request = new HttpRequestMessage(
            HttpMethod.Post, url);

        request.Headers.Add("x-goog-api-key", apiKey);
        request.Content = JsonContent.Create(requestBody);

        using var response = await _http.SendAsync(
            request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            // Không trả nguyên nội dung lỗi của provider
            // cho người dùng vì có thể chứa thông tin nội bộ.
            throw new HttpRequestException(
                $"Gemini returned HTTP {(int)response.StatusCode}.",
                null,
                response.StatusCode);
        }

        using var stream =
            await response.Content.ReadAsStreamAsync(
                cancellationToken);

        using var json = await JsonDocument.ParseAsync(
            stream,
            cancellationToken: cancellationToken);

        if (!json.RootElement.TryGetProperty(
                "candidates", out var candidates)
            || candidates.GetArrayLength() == 0)
        {
            throw new InvalidOperationException(
                "Gemini returned no candidates.");
        }

        var parts = candidates[0]
            .GetProperty("content")
            .GetProperty("parts");

        var texts = new List<string>();

        foreach (var part in parts.EnumerateArray())
        {
            if (part.TryGetProperty(
                    "text", out var textElement))
            {
                texts.Add(textElement.GetString() ?? "");
            }
        }

        var result = string.Join("\n", texts).Trim();

        if (string.IsNullOrWhiteSpace(result))
            throw new InvalidOperationException(
                "Gemini returned an empty response.");

        return result;
    }
}
