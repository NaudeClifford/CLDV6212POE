using Microsoft.AspNetCore.Http;
using System.IO;
using System.Threading.Tasks;

namespace ABCRetailersFunctions.Helpers
{


    public static class FileHelper
    {
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
