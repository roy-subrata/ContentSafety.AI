
using System.Text.Json;
using Azure.AI.ContentSafety;
using Azure.Core;
namespace ContentSafety.Ai;

public class ContentSafetyManager(
    ILogger<ContentSafetyManager> logger,
    ContentSafetyClient contentSafetyClient,
    BlocklistClient blockListClient
)
{
    public async Task<AnalyzeTextResult> AnalyzeTextAync(string text, string[] blockListNames = null, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Calling Ai for analyze text {0}", text);
        var textOption = new AnalyzeTextOptions(text);
        if (blockListNames?.Any() == true)
        {
            foreach (var name in blockListNames)
            {
                if (!string.IsNullOrWhiteSpace(name))
                    textOption.BlocklistNames.Add(name);
            }
        }
        var response = await contentSafetyClient.AnalyzeTextAsync(textOption, cancellationToken);
        return response.Value;
    }
    public async Task<AnalyzeImageResult> AnalyzeImageAsync(BinaryData imageBinary, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Calling AI for image analysis");

        var imageOptions = new AnalyzeImageOptions(new ContentSafetyImageData(imageBinary));
        var response = await contentSafetyClient.AnalyzeImageAsync(imageOptions, cancellationToken);
        return response.Value;
    }

    public async Task AddOrUpdateBlockListAsync(CreateBlockList createBlock)
    {
        var requestContent = RequestContent.Create(JsonSerializer.Serialize(new
        {
            Description = ""
        }));

        await blockListClient.CreateOrUpdateTextBlocklistAsync(createBlock.Name, requestContent);
        IEnumerable<TextBlocklistItem> blockList = [];
        if (createBlock.BlockItems.Any())
        {
            blockList = createBlock.BlockItems.Select(x => new TextBlocklistItem(x.Text)
            {
                Description = x.Description
            });
        }

        var blocklistOptions = new AddOrUpdateTextBlocklistItemsOptions(blockList);

        await blockListClient.AddOrUpdateBlocklistItemsAsync(createBlock.Name, blocklistOptions);
    }

}
public record BlockItem(string Text, string Description);
public record CreateBlockList(string Name, string Description, BlockItem[] BlockItems);