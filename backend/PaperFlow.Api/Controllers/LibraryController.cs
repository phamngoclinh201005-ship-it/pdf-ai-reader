using System.Text;
using Microsoft.AspNetCore.Mvc;
using PaperFlow.Api.Contracts;
using PaperFlow.Api.Models;
using PaperFlow.Api.Services;

namespace PaperFlow.Api.Controllers;

[ApiController]
[Route("api/library")]
public class LibraryController(AuthService auth, LibraryStore library, IConfiguration config) : ControllerBase
{
    private Task<UserRecord?> Current(CancellationToken ct) => auth.CurrentAsync(Request.Cookies[config["Auth:CookieName"] ?? "paperflow_session"], ct);
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        Response.Headers.CacheControl = "no-store";
        var user = await Current(ct);
        return user is null ? Unauthorized() : Ok(await library.ListAsync(user.Id, ct));
    }
    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id, CancellationToken ct)
    {
        Response.Headers.CacheControl = "no-store";
        var user = await Current(ct);
        if (user is null) return Unauthorized();
        var document = await library.GetAsync(user.Id, id, ct);
        return document is null ? NotFound() : Ok(new LibraryDetail(document.Id, document.Name, document.Size, document.UpdatedAt, document.State));
    }
    [HttpGet("{id}/file")]
    public async Task<IActionResult> Download(string id, CancellationToken ct)
    {
        Response.Headers.CacheControl = "no-store";
        var user = await Current(ct);
        if (user is null) return Unauthorized();
        var bytes = await library.ReadAsync(user.Id, id, ct);
        return bytes is null ? NotFound() : File(bytes, "application/pdf", "document.pdf");
    }
    [HttpPost]
    [RequestSizeLimit(45 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 45 * 1024 * 1024)]
    public async Task<IActionResult> Upload(IFormFile file, CancellationToken ct)
    {
        var user = await Current(ct);
        if (user is null) return Unauthorized();
        if (file is null || file.Length < 5 || file.Length > 40 * 1024 * 1024) return BadRequest(new { message = "Chọn PDF hợp lệ, tối đa 40 MB." });
        var items = await library.ListAsync(user.Id, ct);
        if (items.Count >= 100 || items.Sum(x => x.Size) + file.Length > 200 * 1024 * 1024)
            return BadRequest(new { message = "Thư viện tối đa 100 tài liệu và 200 MB. Hãy xóa tài liệu không cần thiết." });
        using var input = file.OpenReadStream();
        var signature = new byte[5];
        await input.ReadExactlyAsync(signature, ct);
        if (Encoding.ASCII.GetString(signature) != "%PDF-") return BadRequest(new { message = "File không phải PDF hợp lệ." });
        input.Position = 0;
        var name = Path.GetFileName(file.FileName.Replace('\\', '/'));
        if (name.Length > 120) name = name[..120];
        var document = await library.UploadAsync(user.Id, name, input, file.Length, ct);
        return Created($"/api/library/{document.Id}", new LibraryItem(document.Id, document.Name, document.Size, document.UpdatedAt, 1));
    }
    [HttpPut("{id}/state")]
    [HttpPost("{id}/state")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> State(string id, ReadingState state, CancellationToken ct)
    {
        var user = await Current(ct);
        if (user is null) return Unauthorized();
        if (state.Messages is null || state.Actions is null || state.Highlights is null || state.Bookmarks is null ||
            state.Bookmarks.Any(b => b is null || string.IsNullOrWhiteSpace(b.Title)) ||
            state.Highlights.Any(h => h is null || h.Text is null || h.Rects is null || h.Rects.Any(r => r is null ||
                !double.IsFinite(r.X) || !double.IsFinite(r.Y) || !double.IsFinite(r.Width) || !double.IsFinite(r.Height) ||
                r.X + r.Width > 1.000001 || r.Y + r.Height > 1.000001)) ||
            state.Messages.Any(x => x is null || x.Text is null || x.Citations is null || x.Citations.Any(n => n < 1 || n > 2000)) ||
            state.Actions.Any(x => x is null || x.Source is null || x.Result is null) ||
            state.Messages.Sum(x => (long)Encoding.UTF8.GetByteCount(x.Text)) + state.Actions.Sum(x => (long)Encoding.UTF8.GetByteCount(x.Source) + Encoding.UTF8.GetByteCount(x.Result)) > 8 * 1024 * 1024)
            return BadRequest(new { message = "Lịch sử quá lớn để lưu. Hãy bắt đầu cuộc trò chuyện mới." });
        return await library.UpdateAsync(user.Id, id, state, ct) ? NoContent() : NotFound();
    }
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var user = await Current(ct);
        if (user is null) return Unauthorized();
        return await library.DeleteAsync(user.Id, id, ct) ? NoContent() : NotFound();
    }
}
