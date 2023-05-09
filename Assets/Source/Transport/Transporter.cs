using System;
using System.Collections.Generic;
using UnityEngine;

namespace GLHF.Transport
{
    public class Transporter
    {
        public event Action<int> OnPeerConnected;
        public event Action<int> OnPeerDisconnected;
        public event Action<int, float, byte[]> OnReceive;

        private readonly ITransport transport;

        public Transporter(ITransport transport)
        {
            this.transport = transport;

            transport.OnReceive += OnTransportReceive;
            transport.OnPeerConnected += OnTrasportPeerConnected;
            transport.OnPeerDisconnected += OnTransportPeerDisconnected;
        }
        
        public void Listen(int port)
        {
            transport.Listen(port);
        }

        public void Connect(string ip, int port)
        {
            transport.Connect(ip, port);
        }

        public void Shutdown()
        {
            transport.Shutdown();
        }

        public void Send(int peerId, byte[] data, DeliveryMethod deliveryMethod)
        {
            transport.Send(peerId, data, deliveryMethod);
        }
        
        public void SendToAll(byte[] data, DeliveryMethod deliveryMethod)
        {
            transport.SendToAll(data, deliveryMethod);
        }
        
        public void Poll()
        {
            transport.Poll();

            if (simulatingLatency)
            {
                for (int i = 0; i < pendingMessages.Count; i++)
                {
                    var tuple = pendingMessages[i];
                    
                    if (Time.time > tuple.Item1)
                    {
                        Message message = tuple.Item2;

                        OnReceive?.Invoke(message.peerId, message.rtt, message.data);

                        pendingMessages.RemoveAt(i);
                        i--;
                    }
                }
            }
        }

        private void OnTransportReceive(int peerId, float rtt, byte[] data)
        {
            if (simulatingLatency)
            {
                // Replace with something Unity agnostic.
                float arrivalTime = Time.time + (float)random.Next(latency.MinDelay, latency.MaxDelay) / 1000;

                pendingMessages.Add((arrivalTime, new Message
                {
                    peerId = peerId,
                    rtt = rtt,
                    data = data
                }));
            }
            else
            {
                OnReceive?.Invoke(peerId, rtt, data);
            }
        }

        private void OnTrasportPeerConnected(int peerId)
        {
            OnPeerConnected?.Invoke(peerId);
        }

        private void OnTransportPeerDisconnected(int peerId)
        {
            OnPeerDisconnected?.Invoke(peerId);
        }

        #region Latency Simulation
        public void SetSimulatedLatency(SimulatedLatency latency)
        {
            this.latency = latency;
        }
        
        public struct SimulatedLatency
        {
            public int MinDelay, MaxDelay;
        }
        
        private SimulatedLatency latency;
        private bool simulatingLatency => latency.Equals(default(SimulatedLatency)) == false;

        private readonly System.Random random = new System.Random();
        private readonly List<(float, Message)> pendingMessages = new List<(float, Message)>();

        private struct Message
        {
            public int peerId;
            public float rtt;
            public byte[] data;
        }
        #endregion
    }
}