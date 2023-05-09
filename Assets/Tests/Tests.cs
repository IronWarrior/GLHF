using NUnit.Framework;
using GLHF.Transport;

namespace GLHF.Tests
{
    public class Tests
    {
        [Test]
        public void TestTransportLocalCanSendAndReceiveData()
        {
            TransportLocal server = new TransportLocal();
            TransportLocal client = new TransportLocal();                

            server.Listen(1);
            client.Connect("", 1);

            byte[] receivedData = null;

            client.OnReceive += (_, _, data) => receivedData = data; 

            byte[] testData = new byte[] { 1, 2, 3 };
            server.Send(0, testData, DeliveryMethod.ReliableOrdered);
            client.Poll();

            Assert.AreEqual(testData, receivedData);
        }
    }
}
