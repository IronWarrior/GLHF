using NUnit.Framework;
using GLHF.Transport;

namespace GLHF.Tests
{
    public class Tests
    {
        [Test]
        public void TransportLocal_CanSendAndReceiveData()
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

            client.Shutdown();
            server.Shutdown();

            Assert.AreEqual(testData, receivedData);
        }

        [Test]
        public void Mean_ReturnsZeroWhenNoValuesInserted()
        {
            RollingStandardDeviation rsd = new RollingStandardDeviation(3);

            rsd.Insert(0);
            rsd.Insert(0);
            rsd.Insert(0);

            rsd.Insert(2.0f);
            rsd.Insert(4.0f);
            rsd.Insert(6.0f);

            Assert.AreEqual(4, rsd.Mean(), 0.001f);
        }

        [Test]
        public void CalculateStandardDeviation_CalculatesCorrectly()
        {
            RollingStandardDeviation rsd = new RollingStandardDeviation(3);

            rsd.Insert(0);
            rsd.Insert(0);
            rsd.Insert(0);
            
            rsd.Insert(2.0f);
            rsd.Insert(4.0f);
            rsd.Insert(6.0f);

            Assert.AreEqual(1.632f, rsd.CalculateStandardDeviation(), 0.001f);
        }
    }
}
