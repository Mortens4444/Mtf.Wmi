using Mtf.WmiHelper.Services;

namespace Mtf.WmiHelper.UnitTests.Services
{
    public class LocalDeviceIdentifierTests
    {
        [TestCase("127.0.0.1", true)]
        [TestCase("127.0.0.2", true)]
        [TestCase("192.168.1.10", false)]
        [TestCase("192.168.0.134", true)]
        [TestCase(".", true)]
        [TestCase("localhost", true)]
        [TestCase("::1", true)]
        [TestCase("256.256.256.256", false)]
        [TestCase("127.foobar", false)]
        public void TestLocalIpAddress(string ipAddress, bool expectedResult)
        {
            var result = LocalDeviceIdentifier.IsLocalMachine(ipAddress);
            Assert.That(result, Is.EqualTo(expectedResult));
        }
    }
}