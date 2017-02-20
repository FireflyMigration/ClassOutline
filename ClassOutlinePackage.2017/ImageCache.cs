using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ClassOutline
{
    public class ImageCache
    {
        private string _imageFolder;

        private Dictionary<string, ImageSource> _imageSources;

        public ImageCache(string imageFolder)
        {
            _imageFolder = imageFolder;
            initImageCache();
        }

        public ImageSource getImageSource(string key)
        {
            var foundkey = findMatchingKey(key);
            if (foundkey == null) return null;

            return _imageSources[foundkey ];
            
        }

        private void initImageCache()
        {
            // load all images in the "iamges" folder
            
            _imageSources = new Dictionary<string, ImageSource>();

            if (!string.IsNullOrEmpty(_imageFolder ) && Directory.Exists(_imageFolder))
            {

                foreach (var f in Directory.GetFiles(_imageFolder, "*.png", SearchOption.TopDirectoryOnly))
                {
                    if (f != null)
                    {
                        var filePath = Path.Combine(_imageFolder, f);

                        var bmp = new BitmapImage(new Uri(filePath));


                        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(f);
                        _imageSources.Add(fileNameWithoutExtension, bmp);
                    }
                }
            }
            _imageSources.Add(".UIController", new BitmapImage(new Uri(@"/Resources/UIController.png", UriKind.Relative)));
            _imageSources.Add(".FlowUIController",
                new BitmapImage(new Uri(@"/Resources/UIController.png", UriKind.Relative)));
            _imageSources.Add(".AbstractUIController",
                new BitmapImage(new Uri(@"/Resources/UIController.png", UriKind.Relative)));
        }

        private string findMatchingKey(string key)
        {
            try
            {


                var foundKey = _imageSources.Keys.FirstOrDefault(x => Regex.IsMatch(key, x, RegexOptions.IgnoreCase));
                if (foundKey == null) return null;

                return foundKey;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
            return null;

        }
        public bool ContainsKey(string key)
        {
            return findMatchingKey(key) != null;

        }
    }
}