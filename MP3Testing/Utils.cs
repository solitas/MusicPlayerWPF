using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace MP3Testing
{
    class Utils
    {
        [DllImport("gdi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DeleteObject(IntPtr value);
        public static BitmapSource GetImageStream(Image myImage)
        {
            var bitmap = new Bitmap(myImage);
            IntPtr bmpPt = bitmap.GetHbitmap();
            BitmapSource bitmapSource =
             System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                   bmpPt,
                   IntPtr.Zero,
                   Int32Rect.Empty,
                   BitmapSizeOptions.FromEmptyOptions());

            //freeze bitmapSource and clear memory to avoid memory leaks
            bitmapSource.Freeze();
            DeleteObject(bmpPt);

            return bitmapSource;
        }

        public static string ConvertEncoding(string str)
        {
            Encoding convertEnc = Encoding.GetEncoding("iso-8859-1");
            Encoding encKr = Encoding.GetEncoding("euc-kr");
            Encoding destEnc = Encoding.UTF8;

            byte[] sourceBytes = convertEnc.GetBytes(str);

            byte[] encBytes = Encoding.Convert(encKr, destEnc, sourceBytes);

            return destEnc.GetString(encBytes);
        }
    }
}
