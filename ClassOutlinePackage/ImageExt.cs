using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Media.Imaging;

namespace ClassOutline
{
    public static class ImageExt
    {
        public static BitmapImage ToBitmapImage(this Image source)
        {
            // ImageSource ...

            var bi = new BitmapImage();

            bi.BeginInit();

            var ms = new MemoryStream();

            // Save to a memory stream...

            source.Save(ms, ImageFormat.Bmp);

            // Rewind the stream...

            ms.Seek(0, SeekOrigin.Begin);

            // Tell the WPF image to use this stream...

            bi.StreamSource = ms;

            bi.EndInit();

            return bi;
        }

     
    }
}