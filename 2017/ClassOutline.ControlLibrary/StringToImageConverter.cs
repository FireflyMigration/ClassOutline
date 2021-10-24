using System;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace ClassOutline.ControlLibrary
{
    public class StringToImageConverter : IValueConverter
    {
        public object Convert(object value,
            Type targetType,
            object parameter,
            System.Globalization.CultureInfo culture)
        {
            // if value isn't null, we can safely do the conversion. 
            if (value != null)
            {
                string imageName = value.ToString();
                Uri uri = new Uri(String.Format("/Resources/{0}", imageName),
                    UriKind.Relative);
                return new BitmapImage(uri);
            }
            return null;
        }
        public object ConvertBack(object value,
            Type targetType,
            object parameter,
            System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}