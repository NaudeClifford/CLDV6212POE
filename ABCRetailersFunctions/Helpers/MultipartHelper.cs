using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;
using System.Text;

namespace ABCRetailersFunctions.Helpers
{
    public static class MultipartHelper
    {
        public class MultipartFormData
        {
            public Dictionary<string, string> Text { get; set; } = new();
            public List<MultipartFile> Files { get; set; } = new();
        }

        public class MultipartFile
        {
            public string FileName { get; set; }
            public Stream Data { get; set; }
        }

        public static async Task<MultipartFormData> ParseAsync(Stream body, string contentType)
        {
            var formData = new MultipartFormData();

            var mediaTypeHeader = MediaTypeHeaderValue.Parse(contentType);
            var boundary = HeaderUtilities.RemoveQuotes(mediaTypeHeader.Boundary).Value;

            if (string.IsNullOrEmpty(boundary))
                throw new InvalidDataException("Missing content-type boundary.");

            var reader = new MultipartReader(boundary, body);
            MultipartSection? section;

            while ((section = await reader.ReadNextSectionAsync()) != null)
            {
                var hasContentDispositionHeader =
                    ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out var contentDisposition);

                if (!hasContentDispositionHeader)
                    continue;

                if (contentDisposition.IsFileDisposition())
                {
                    var fileName = contentDisposition.FileName.Value ?? contentDisposition.FileNameStar.Value;
                    using var memoryStream = new MemoryStream();
                    await section.Body.CopyToAsync(memoryStream);
                    memoryStream.Position = 0;

                    formData.Files.Add(new MultipartFile
                    {
                        FileName = fileName,
                        Data = memoryStream
                    });
                }
                else if (contentDisposition.IsFormDisposition())
                {
                    var key = contentDisposition.Name.Value?.Trim('"');
                    using var readerStream = new StreamReader(section.Body, Encoding.UTF8);
                    var value = await readerStream.ReadToEndAsync();
                    if (key != null)
                        formData.Text[key] = value;
                }
            }

            return formData;
        }

        public static async Task<byte[]> ReadFileAsync(IFormFile file)
        {
            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            return memoryStream.ToArray();
        }

        public static async Task<string> SaveFileAsync(IFormFile file, string path)
        {
            var fileName = Path.GetFileName(file.FileName);
            var filePath = Path.Combine(path, fileName);

            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);

            return filePath;
        }

        public static bool IsSupportedImage(IFormFile file)
        {
            var supportedTypes = new[] { "image/jpeg", "image/png", "image/gif" };
            return supportedTypes.Contains(file.ContentType.ToLower());
        }

        public static bool IsSupportedDocument(IFormFile file)
        {
            var supportedTypes = new[] { "application/pdf", "image/jpeg", "image/png" };
            return supportedTypes.Contains(file.ContentType.ToLower());
        }
    }
}
