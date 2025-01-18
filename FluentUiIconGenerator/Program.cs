// See https://aka.ms/new-console-template for more information

using System.Drawing;
using System.Drawing.Imaging;
using System.Text.Json;
using System.Text.Json.Serialization;

Console.WriteLine("Hello, World!");

// hardcoded! sad!
const string emojiRoot = @"G:\GitHub\Csharp\FolderIconifier\FolderIconifier\Icons\Emoji";

// also hardcoded! sad!
foreach (var emojiDir in Directory.GetDirectories(@"G:\GitHub\Misc\fluentui-emoji\assets"))
{
    if (!Directory.Exists(Path.Combine(emojiDir, "3D")))
    {
        Console.WriteLine($"Skin tone emoji: {emojiDir}");
        continue;
    }
    
    var metadataPath = Path.Combine(emojiDir, "metadata.json");
    var metadata = JsonSerializer.Deserialize<EmoteMetadata>(File.ReadAllText(metadataPath))!;
    var file = Directory.GetFiles(Path.Combine(emojiDir, "3D"), "*.png")[0];
    if (!IconHelper.ConvertToIcon(file, Path.Combine(emojiRoot, metadata.Cldr + ".ico"), 256))
    {
        throw new InvalidOperationException($"Failed to convert {file} to ico");
    }
}

public record EmoteMetadata(
    [property: JsonPropertyName("cldr")] string Cldr,
    [property: JsonPropertyName("fromVersion")] string FromVersion,
    [property: JsonPropertyName("glyph")] string Glyph,
    [property: JsonPropertyName("group")] string Group,
    [property: JsonPropertyName("keywords")] string[] Keywords,
    [property: JsonPropertyName("mappedToEmoticons")] string[] MappedToEmoticons,
    [property: JsonPropertyName("tts")] string Tts,
    [property: JsonPropertyName("unicode")] string Unicode
);

/// <summary>
/// Provides helper methods for imaging
/// </summary>
public static class IconHelper
{
    /// <summary>
    /// Converts a PNG image to a icon (ico)
    /// </summary>
    /// <param name="input">The input stream</param>
    /// <param name="output">The output stream</param>
    /// <param name="size">Needs to be a factor of 2 (16x16 px by default)</param>
    /// <param name="preserveAspectRatio">Preserve the aspect ratio</param>
    /// <returns>Wether or not the icon was succesfully generated</returns>
    public static bool ConvertToIcon(Stream input, Stream output, int size = 16, bool preserveAspectRatio = false)
    {
        if (Image.FromStream(input) is not Bitmap inputBitmap)
            return false;

        int width = size, height = size;
        if (preserveAspectRatio)
        {
            if (inputBitmap.Width > inputBitmap.Height)
                height = (int)(((float)inputBitmap.Height / inputBitmap.Width) * size);
            else
                width = (int)(((float)inputBitmap.Width / inputBitmap.Height) * size);
        }

        var newBitmap = new Bitmap(inputBitmap, new Size(width, height));

        // save the resized png into a memory stream for future use
        using var memoryStream = new MemoryStream();
        newBitmap.Save(memoryStream, ImageFormat.Png);

        var iconWriter = new BinaryWriter(output);

        // 0-1 reserved, 0
        iconWriter.Write((byte)0);
        iconWriter.Write((byte)0);

        // 2-3 image type, 1 = icon, 2 = cursor
        iconWriter.Write((short)1);

        // 4-5 number of images
        iconWriter.Write((short)1);

        // image entry 1
        // 0 image width
        iconWriter.Write((byte)width);
        // 1 image height
        iconWriter.Write((byte)height);

        // 2 number of colors
        iconWriter.Write((byte)0);

        // 3 reserved
        iconWriter.Write((byte)0);

        // 4-5 color planes
        iconWriter.Write((short)0);

        // 6-7 bits per pixel
        iconWriter.Write((short)32);

        // 8-11 size of image data
        iconWriter.Write((int)memoryStream.Length);

        // 12-15 offset of image data
        iconWriter.Write(6 + 16);

        // write image data
        // png data must contain the whole png data file
        iconWriter.Write(memoryStream.ToArray());

        iconWriter.Flush();

        return true;
    }

    /// <summary>
    /// Converts a PNG image to a icon (ico)
    /// </summary>
    /// <param name="inputPath">The input path</param>
    /// <param name="outputPath">The output path</param>
    /// <param name="size">Needs to be a factor of 2 (16x16 px by default)</param>
    /// <param name="preserveAspectRatio">Preserve the aspect ratio</param>
    /// <returns>Wether or not the icon was succesfully generated</returns>
    public static bool ConvertToIcon(string inputPath, string outputPath, int size = 16, bool preserveAspectRatio = false)
    {
        using (FileStream inputStream = new FileStream(inputPath, FileMode.Open))
        using (FileStream outputStream = new FileStream(outputPath, FileMode.Create))
        {
            return ConvertToIcon(inputStream, outputStream, size, preserveAspectRatio);
        }
    }
}