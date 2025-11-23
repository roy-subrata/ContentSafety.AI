

using Azure;
using Azure.AI.ContentSafety;
using ContentSafety.Ai;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);
string Endpoint = Environment.GetEnvironmentVariable("ENDPOINT", EnvironmentVariableTarget.User) ?? throw new ArgumentNullException("Endpoint not found");
string key = Environment.GetEnvironmentVariable("KEY", EnvironmentVariableTarget.User) ?? throw new ArgumentNullException("Key not found");


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped(ctx => new BlocklistClient(new Uri(Endpoint), new AzureKeyCredential(key)));

builder.Services.AddScoped(ctx => new ContentSafetyClient(new Uri(Endpoint), new AzureKeyCredential(key)));
builder.Services.AddAntiforgery();
builder.Services.AddTransient<ContentSafetyManager>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseRouting();
app.UseAntiforgery();

app.MapGet("/analyze-text", async ([FromQuery] string text, [FromServices] ContentSafetyManager safetyManager) =>
{
    if (string.IsNullOrEmpty(text))
    {
        return Results.BadRequest("Query can not be empty!");
    }
    var response = await safetyManager.AnalyzeTextAync(text, ["TextBlockList"]);

    return Results.Ok(new
    {
        categoriesAnalysis = response.CategoriesAnalysis.Select(x => $"Category {x.Category} and Severity {x.Severity}").ToList(),
        blockListMatch = response.BlocklistsMatch.Select(x => $"Block Macth Text: {x.BlocklistItemText}")
    });
});

app.MapPost("/analyze-image", async ([FromForm] IFormFile request, [FromServices] ContentSafetyManager safetyManager) =>
{
    if (request == null || request.Length == 0)
        return Results.BadRequest("No file uploaded.");

    using var ms = new MemoryStream();
    await request.CopyToAsync(ms);
    var binaryData = new BinaryData(ms.ToArray());

    var response = await safetyManager.AnalyzeImageAsync(binaryData);
    return Results.Ok(new
    {
        categoriesAnalysis = response.CategoriesAnalysis
            .Select(x => $"Category {x.Category} and Severity {x.Severity}")
            .ToList(),
    });
})
.Accepts<IFormFile>("multipart/form-data")
.Produces(StatusCodes.Status200OK);

app.MapPost("/blockList", async ([FromBody] CreateBlockList request, [FromServices] ContentSafetyManager safetyManager) =>
{
    await safetyManager.AddOrUpdateBlockListAsync(request);
    return Results.Ok();
});

app.Run();
