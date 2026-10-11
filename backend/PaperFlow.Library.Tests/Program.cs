using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using MongoDB.Driver;
using PaperFlow.Api.Contracts;
using PaperFlow.Api.Models;
using PaperFlow.Api.Security;

// Only a uniquely named temporary database is created/dropped. No real email or AI requests.
var dbName = "PaperFlowLibraryTests_" + Guid.NewGuid().ToString("N");
var frontendOrigin = Environment.GetEnvironmentVariable("PAPERFLOW_TEST_FRONTEND_ORIGIN") ?? "http://localhost:3000";
var mongo = new MongoClient("mongodb://localhost:27017");
var database = mongo.GetDatabase(dbName);
var users = database.GetCollection<UserRecord>("users");
var sessions = database.GetCollection<SessionRecord>("sessions");
var owner = new UserRecord { Email = "owner@example.invalid", EmailVerified = true, DisplayName = "Owner" };
var other = new UserRecord { Email = "other@example.invalid", EmailVerified = true, DisplayName = "Other" };
await users.InsertManyAsync([owner, other]);
var ownerToken = Crypto.NewToken(); var otherToken = Crypto.NewToken();
await sessions.InsertManyAsync([
    new() { UserId = owner.Id, TokenHash = Crypto.Sha256(ownerToken), ExpiresAt = DateTime.UtcNow.AddHours(1) },
    new() { UserId = other.Id, TokenHash = Crypto.Sha256(otherToken), ExpiresAt = DateTime.UtcNow.AddHours(1) }
]);
var start = new ProcessStartInfo("dotnet") { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
start.ArgumentList.Add(Path.Combine(AppContext.BaseDirectory, "PaperFlow.Api.dll"));
start.ArgumentList.Add("--urls"); start.ArgumentList.Add("http://127.0.0.1:5067");
start.Environment.Remove("PORT");
start.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
start.Environment["MongoDB__ConnectionString"] = "mongodb://localhost:27017";
start.Environment["MongoDB__DatabaseName"] = dbName;
start.Environment["Frontend__Origin"] = frontendOrigin;
start.Environment["Auth__CookieName"] = "paperflow_session";
using var process = new Process { StartInfo = start };
var output = new StringBuilder();
process.OutputDataReceived += (_, e) => { if (e.Data is not null) output.AppendLine(e.Data); };
process.ErrorDataReceived += (_, e) => { if (e.Data is not null) output.AppendLine(e.Data); };
using var client = new HttpClient { BaseAddress = new Uri("http://127.0.0.1:5067"), Timeout = TimeSpan.FromSeconds(10) };
void Check(bool value, string label) { if (!value) throw new Exception(label); }
async Task<HttpResponseMessage> Send(HttpMethod method, string path, string? token = null, HttpContent? content = null, string? origin = null)
{
    using var request = new HttpRequestMessage(method, path) { Content = content };
    request.Headers.Add("Origin", origin ?? frontendOrigin);
    if (token is not null) request.Headers.Add("Cookie", "paperflow_session=" + token);
    return await client.SendAsync(request);
}
MultipartFormDataContent Pdf(byte[] bytes)
{
    var body = new MultipartFormDataContent(); body.Add(new ByteArrayContent(bytes), "file", "sample.pdf"); return body;
}
try
{
    process.Start(); process.BeginOutputReadLine(); process.BeginErrorReadLine();
    var ready = false;
    for (var n = 0; n < 100; n++)
    {
        if (process.HasExited) throw new Exception(output.ToString());
        try { ready = (await client.GetAsync("/api/health")).IsSuccessStatusCode; } catch (HttpRequestException) { }
        if (ready) break;
        await Task.Delay(100);
    }
    Check(ready, "Test API starts");
    var bytes = Encoding.ASCII.GetBytes("%PDF-1.4\nTest PDF bytes\n%%EOF");
    Check((await Send(HttpMethod.Get, "/api/library")).StatusCode == HttpStatusCode.Unauthorized, "Guest cannot list");
    Check((await Send(HttpMethod.Post, "/api/library", content: Pdf(bytes))).StatusCode == HttpStatusCode.Unauthorized, "Guest cannot save PDF");
    Check(await database.GetCollection<LibraryDocument>("library_documents").CountDocumentsAsync(_ => true) == 0, "Guest writes no data");
    Check((await Send(HttpMethod.Post, "/api/library", ownerToken, Pdf(bytes), "https://untrusted.example")).StatusCode == HttpStatusCode.Forbidden, "Origin protection applies to library");
    Check((await Send(HttpMethod.Post, "/api/library", ownerToken, Pdf(Encoding.ASCII.GetBytes("not a PDF")))).StatusCode == HttpStatusCode.BadRequest, "Invalid PDF rejected");
    var upload = await Send(HttpMethod.Post, "/api/library", ownerToken, Pdf(bytes));
    Check(upload.StatusCode == HttpStatusCode.Created, "Member saves PDF");
    var item = await upload.Content.ReadFromJsonAsync<LibraryItem>();
    var id = item!.Id;
    foreach (var path in new[] { $"/api/library/{id}", $"/api/library/{id}/file" })
        Check((await Send(HttpMethod.Get, path, otherToken)).StatusCode == HttpStatusCode.NotFound, "Other account cannot read data");
    Check((await Send(HttpMethod.Delete, $"/api/library/{id}", otherToken)).StatusCode == HttpStatusCode.NotFound, "Other account cannot delete");
    var state = new ReadingState { Page = 2, Zoom = 1.5,
        Messages = [new() { Role = "user", Text = "Question" }, new() { Role = "assistant", Text = "Answer", Citations = [2] }],
        Actions = [new() { Mode = "translate", Source = "Source text", Result = "Translated text", Page = 2 }]
    };
    Check((await Send(HttpMethod.Put, $"/api/library/{id}/state", otherToken, JsonContent.Create(state))).StatusCode == HttpStatusCode.NotFound, "Other account cannot overwrite state");
    Check((await Send(HttpMethod.Put, $"/api/library/{id}/state", ownerToken, JsonContent.Create(state))).StatusCode == HttpStatusCode.NoContent, "Member saves reading state");
    var detail = await (await Send(HttpMethod.Get, $"/api/library/{id}", ownerToken)).Content.ReadFromJsonAsync<LibraryDetail>();
    Check(detail!.State.Page == 2 && detail.State.Zoom == 1.5 && detail.State.Messages[1].Text == "Answer" && detail.State.Actions[0].Result == "Translated text", "Reading state round-trips");
    Check((await (await Send(HttpMethod.Get, $"/api/library/{id}/file", ownerToken)).Content.ReadAsByteArrayAsync()).SequenceEqual(bytes), "PDF bytes restored exactly");
    var listed = await (await Send(HttpMethod.Get, "/api/library", ownerToken)).Content.ReadFromJsonAsync<List<LibraryItem>>();
    Check(listed!.Count == 1 && listed[0].Page == 2, "Library lists progress");
    var otherItems = await (await Send(HttpMethod.Get, "/api/library", otherToken)).Content.ReadFromJsonAsync<List<LibraryItem>>();
    Check(otherItems!.Count == 0, "Library isolated by owner");
    var large = new byte[17 * 1024 * 1024]; bytes.CopyTo(large, 0);
    var largeUpload = await Send(HttpMethod.Post, "/api/library", ownerToken, Pdf(large));
    Check(largeUpload.StatusCode == HttpStatusCode.Created, "PDF larger than BSON limit saves in chunks");
    var largeItem = await largeUpload.Content.ReadFromJsonAsync<LibraryItem>();
    Check((await (await Send(HttpMethod.Get, $"/api/library/{largeItem!.Id}/file", ownerToken)).Content.ReadAsByteArrayAsync()).SequenceEqual(large), "Large PDF round-trips");
    Check((await Send(HttpMethod.Delete, $"/api/library/{id}", ownerToken)).StatusCode == HttpStatusCode.NoContent, "Owner can delete");
    Check(await database.GetCollection<LibraryChunk>("library_chunks").CountDocumentsAsync(x => x.DocumentId == id) == 0, "Deleting PDF removes its chunks");
    Console.WriteLine("Library API checks passed: guest writes blocked, owner isolation, PDF/state restore, 17 MB chunked PDF, deletion and origin protection.");
    if (Environment.GetEnvironmentVariable("PAPERFLOW_BROWSER_FIXTURE") == "1")
    {
        await database.GetCollection<LibraryDocument>("library_documents").DeleteManyAsync(_ => true);
        await database.GetCollection<LibraryChunk>("library_chunks").DeleteManyAsync(_ => true);
        Console.WriteLine("FIXTURE:" + JsonSerializer.Serialize(new { ownerToken, otherToken }));
        Console.ReadLine();
    }
}
finally
{
    if (process.Id != 0 && !process.HasExited) { process.Kill(entireProcessTree: true); await process.WaitForExitAsync(); }
    if (!dbName.StartsWith("PaperFlowLibraryTests_", StringComparison.Ordinal)) throw new Exception("Unexpected test database");
    await mongo.DropDatabaseAsync(dbName);
}
