using System;
using System.ComponentModel.DataAnnotations;
using System.Text;
using KrogerScrape.Client;
using KrogerScrape.Support;

namespace KrogerScrape.Entities
{
    public class ResponseEntity
    {
        public long Id { get; set; }
        public long OperationEntityId { get; set; }
        [Required]
        public string RequestId { get; set; }
        public RequestType RequestType { get; set; }
        public DateTimeOffset CompletedTimestamp { get; set; }
        [Required]
        public string Method { get; set; }
        [Required]
        public string Url { get; set; }
        public CompressionType CompressionType { get; set; }
        [Required]
        public byte[] Bytes { get; set; }

        public OperationEntity OperationEntity { get; set; }

        public void Compress(string body)
        {
            var uncompressedBytes = Encoding.UTF8.GetBytes(body);
            var compressedBytes = CompressionUtility.Compress(uncompressedBytes);
            var isCompressed = compressedBytes.Length < uncompressedBytes.Length;

            CompressionType = isCompressed ? CompressionType.Gzip : CompressionType.None;
            Bytes = isCompressed ? compressedBytes : uncompressedBytes;
        }

        public string Decompress()
        {
            byte[] decompressed;
            switch (CompressionType)
            {
                case CompressionType.Gzip:
                    decompressed = CompressionUtility.Decompress(Bytes);
                    break;
                case CompressionType.None:
                    decompressed = Bytes;
                    break;
                default:
                    throw new NotImplementedException();
            }

            return Encoding.UTF8.GetString(decompressed);
        }
    }
}
