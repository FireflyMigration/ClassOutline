using System.Collections.Generic;
using System.Linq;
using ClassOutline;
using ClassOutline.Services;
using NUnit.Framework;

namespace VSPackage1_UnitTests
{
    [TestFixture]
    public class regiontest
    {
        [Test]
        public void can_find_simple_regions()
        {
            var code = "this is some\n" +
                       "void t() {\n" +
                       " #region mybum\n" +
                       " int x " +
                       " #endregion\n" +
                       "}";
            var c = new RegionParser();
           

            var result = c.GetRegions(code);
            Assert.IsNotNull(result);

        }

        [Test]
        public void can_handle_nested_regions()
        {
            var code = "this is some\n" +
                       "void t() {\n" +
                       " #region mybum\n" +
                       " int x \n" +
                       " #region nestedregion1\n" +
                       " var s = fred\n" +
                       "#endregion\n" +
                       " #endregion\n" +
                       "}";
            var c = new RegionParser();


            var result = c.GetRegions(code);
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count());
            Assert.AreEqual("mybum", result.First().Name);
            Assert.AreEqual("nestedregion1", result.First().NestedRegions.First().Name);
        }
        [Test]
        public void can_handle_multiple_regions()
        {
            var code = "this is some\n" +
                     "void t() {\n" +
                     " #region mybum\n" +
                     " int x \n " +
                     " #region nestedregion1\n" +
                     " var s = fred\n" +
                     "#endregion\n" +
                     " #region nestedregion2\n" +
                     " var s = fred\n" +
                     "#endregion\n" +
                     
                     " #endregion\n" +
                     "}";
            var c = new RegionParser();


            var result = c.GetRegions(code);
            Assert.IsNotNull(result);
            Assert.AreEqual("mybum", result.First().Name);
            var expected = new string[] {"nestedregion1", "nestedregion2"};
            var actual = result.First().NestedRegions.Select(x => x.Name);
          
           var actual2 =  result.SelectMany(x => x.NestedRegions);
  CollectionAssert.AreEqual(expected, actual.ToList());
        }
    }
}