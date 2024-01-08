namespace iTunes.SMTC.Utils
{
    public static class ImageUtils
    {
        public static byte[] ToBytes(this Image img)
        {
            ImageConverter converter = new();
            return (byte[])converter.ConvertTo(img, typeof(byte[]));
        }
    }
}
