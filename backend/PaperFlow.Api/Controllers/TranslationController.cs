using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PaperFlow.Api.Contracts;
using PaperFlow.Api.Services;

namespace PaperFlow.Api.Controllers;

[ApiController]
[Route("api/translate")]
[EnableRateLimiting("ai")]
[RequestSizeLimit(30000)]
public class TranslationController : ControllerBase
{
    private readonly GeminiService _gemini;

    public TranslationController(GeminiService gemini)
    {
        _gemini = gemini;
    }

    [HttpPost]
    public async Task<IActionResult> Translate(
        [FromBody] TranslateRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
            return BadRequest(new {
                message = "Vui lòng nhập văn bản."
            });

        if (request.Text.Length > 5000)
            return BadRequest(new {
                message = "Văn bản quá dài."
            });

        var mode = (request.Mode ?? "translate").ToLowerInvariant();

        if (mode != "translate" && mode != "explain" && mode != "summarize")
            return BadRequest(new {
                message = "Mode không hợp lệ."
            });

        var instruction = mode switch
        {
            "explain" => "Giải thích đoạn văn bằng tiếng Việt dễ hiểu, làm rõ thuật ngữ và giữ nguyên công thức.",
            "summarize" => "Tóm tắt đoạn văn bằng tiếng Việt, giữ các ý chính và số liệu, không thêm thông tin.",
            _ => "Dịch đoạn văn sang tiếng Việt chính xác, giữ công thức và ý nghĩa. Chỉ trả bản dịch."
        };

        var result = await _gemini.GenerateAsync(
            $"{instruction}\n\n{request.Text}",
            cancellationToken);

        return Ok(new { result, mode });
    }
}
