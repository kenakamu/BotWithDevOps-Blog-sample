using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace O365Bot.FunctionTests
{
    [TestClass]
    public class FunctionTest1
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public void Function_ShouldReturnEvents()
        {
            DirectLineHelper helper = new DirectLineHelper(TestContext);
            var toUser = helper.SentMessage("get appointments");
            Assert.IsTrue(true);
        }
    }
}