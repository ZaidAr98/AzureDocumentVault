using System.Security.Cryptography;

namespace DocumentVault.dtos
{
    public class DocumentResponseDto
    {
        public string Id { get; set; }
        public string FileName { get; set; }
        public string ContentType { get; set; }
        public long FileSize { get; set; }
        public List<string> Tags { get; set; }
        public DateTime UploadDate { get; set; }

        public string FileSizeDisplay => FormatFileSize(FileSize);



        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int orders = 0;

            while(len >= 1024 && orders < sizes.Length - 1)
            {
                orders++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[orders]}";

        }
    }
}
