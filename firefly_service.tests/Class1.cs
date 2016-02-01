using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClassOutline;
using NUnit.Framework;

namespace _service.tests
{
    [TestFixture]
    public class ItemKindImageMapServiceTests
    {
        [Test]
        [Ignore]
        public void signif()
        {
            var vsix =
                @"C:\Users\Kieron\Documents\Visual Studio 2013\Projects\FireflyDocumentOutlineSolution-0.2\VSPackage1\bin\Debug\ClassOutlineExtension.vsix";
            var pfx =
                @"C:\Users\Kieron\Documents\Visual Studio 2013\Projects\FireflyDocumentOutlineSolution-0.2\kieron.pfx";

            SignVSIX.SignVSIX.Sign(vsix , pfx, "smash19");

        }
        [Test]
        public void gets_expected_image_name()
        {
            var sourceData = new Dictionary<string, string>()
            {
                {"P", "Property"}
            };
            var svc = getService(sourceData);
            var expected = "Property";
            var kind = "P";
            var actual = svc.getImageKey(kind);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void default_key_returned_for_unknown_image()
        {
            var sourceData = new Dictionary<string, string>()
            {
                {"P", "Property"}
            };
            var svc = getService(sourceData);
            var expected = svc.DefaultKey;
            var kind = "X";
            var actual = svc.getImageKey(kind);
            Assert.AreEqual(expected, actual);
            
        }
        private IItemKindImageMapService getService(Dictionary<string, string> sourceData)
        {
            return new ItemKindImageMapService(sourceData);
        }
    }
}
