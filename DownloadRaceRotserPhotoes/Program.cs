using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;

class Program
{
    private static readonly HttpClient client = new HttpClient();
    private const string BaseUrl = "https://raceroster.com/galleries/api/galleries/cm8vcfh8d00sx10pjdurnb31b/photos";
    private const string GroupId = "cm8vcodmi022811ps3vnddxko";
    private const int TotalPages = 350;
    private const string OutputFolder = "/Users/wasfi/Downloads/RacePhotos";
    private const int BatchSize = 10;

    static async Task Main(string[] args)
    {
        Directory.CreateDirectory(OutputFolder);
        var downloadTasks = new List<Task>();

        for (int page = 1; page <= TotalPages; page++)
        {
            Console.WriteLine($"\n📄 Fetching page {page} of {TotalPages}...");
            string url = $"{BaseUrl}?groupId={GroupId}&page={page}";

            try
            {
                var photos = await FetchPhotoListAsync(url);

                foreach (var photo in photos)
                {
                    string fileName = photo.GetProperty("fileName").GetString();
                    string imageUrl = photo.GetProperty("processedFile").GetProperty("uri").GetString();
                    string filePath = Path.Combine(OutputFolder, fileName);

                    if (File.Exists(filePath))
                    {
                        Console.WriteLine($"⏭ Skipped (already exists): {fileName}");
                        continue;
                    }

                    downloadTasks.Add(DownloadImageAsync(imageUrl, filePath));

                    // Run in batches
                    if (downloadTasks.Count == BatchSize)
                    {
                        await Task.WhenAll(downloadTasks);
                        downloadTasks.Clear();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error fetching page {page}: {ex.Message}");
            }
        }

        // Await any remaining downloads
        if (downloadTasks.Count > 0)
        {
            await Task.WhenAll(downloadTasks);
        }

        Console.WriteLine("\n✅ Done downloading images.");
    }

    private static async Task<JsonElement[]> FetchPhotoListAsync(string url)
    {
        var response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(content);
        var photoList = doc.RootElement.GetProperty("data").GetProperty("data");

        var elements = new List<JsonElement>();
        foreach (var item in photoList.EnumerateArray())
        {
            elements.Add(item.Clone());
        }

        return elements.ToArray();
    }

    private static async Task DownloadImageAsync(string imageUrl, string filePath)
    {
        try
        {
            var response = await client.GetAsync(imageUrl);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync();
            await using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
            await stream.CopyToAsync(fileStream);

            Console.WriteLine($"✔ Downloaded: {Path.GetFileName(filePath)}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠ Failed to download {imageUrl}: {ex.Message}");
        }
    }
}