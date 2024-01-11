using System.Drawing.Imaging;
using Windows.Storage.Streams;

namespace iTunes.SMTC.Utils
{
    public static class ImageUtils
    {
        public static byte[] ToBytes(this Image img)
        {
            using var memoryStream = new MemoryStream();
            img.Save(memoryStream, ImageFormat.Jpeg);
            return memoryStream.ToArray();
        }

        public static async Task<byte[]> ToBytes(this RandomAccessStreamReference _ref)
        {
            try
            {
                using var stream = await _ref.OpenReadAsync();
                using var roStream = stream.AsStreamForRead();

                byte[] arr = new byte[roStream.Length];

                await roStream.ReadAsync(arr);

                return arr;
            }
            catch { }

            return null;
        }
    }
}
