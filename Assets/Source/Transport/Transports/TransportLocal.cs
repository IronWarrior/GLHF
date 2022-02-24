using System;
using System.Collections.Generic;
using UnityEngine;

namespace GLHF.Transport
{   
    /// <summary>
    /// Dummy transport to spoof connections within a single instance of the app.
    /// </summary>
    public class TransportLocal : ITransport
    {
        public event Action OnPeerConnected;
        public event Action<int, float, byte[]> OnReceive;

        private int ID;
        private int sendSequenceNumber;

        private int lastReceivedSequenceNumber = -1;

        private struct Packet
        {
            public int SendingPeerID;
            public int SequenceNumber;
            public byte[] Data;
            public float SendTime;
        }

        private readonly List<Packet> pendingReceivedPackets = new List<Packet>();
        private readonly List<TransportLocal> peers = new List<TransportLocal>();

        private static TransportLocal listeningTransport;

        private struct LatencyPacket
        {
            public Packet Packet;
            public float Latency;
            public TransportLocal TargetPeer;
        }

        private readonly List<LatencyPacket> pendingSendPackets = new List<LatencyPacket>();

        private SimulatedLatency simulatedLatency;

        public void Listen(int _)
        {
            if (listeningTransport != null)
                throw new Exception("Already is a currently listening local transport.");

            listeningTransport = this;
        }

        public void Connect(string _, int __)
        {
            if (listeningTransport == null)
                throw new Exception("No listening transport.");

            listeningTransport.ConnectionRequest(this);
        }

        public void Update()
        {
            while (pendingReceivedPackets.Count > 0)
            {
                Packet packet = pendingReceivedPackets[pendingReceivedPackets.Count - 1];

                if (packet.SequenceNumber == lastReceivedSequenceNumber + 1)
                {
                    pendingReceivedPackets.RemoveAt(pendingReceivedPackets.Count - 1);
                    lastReceivedSequenceNumber++;

                    float rtt = (Time.unscaledTime - packet.SendTime) * 2;

                    OnReceive?.Invoke(packet.SendingPeerID, rtt, packet.Data);
                }
                else
                {
                    break;
                }
            }

            // Send packets with simulated latency.
            for (int i = 0; i < pendingSendPackets.Count; i++)
            {
                LatencyPacket packet = pendingSendPackets[i];

                if (Time.unscaledTime > packet.Latency + packet.Packet.SendTime)
                {
                    packet.TargetPeer.ReceiveInternal(packet.Packet);
                    pendingSendPackets.RemoveAt(i);
                    i--;
                }
            }
        }

        public void Shutdown()
        {
            if (listeningTransport == this)
                listeningTransport = null;
        }

        // TODO: Should be public, need to abstract TransportLocal.
        private void Send(TransportLocal peer, byte[] data)
        {
            Packet packet = new Packet()
            {
                SendingPeerID = peer.ID,
                Data = data,
                SequenceNumber = sendSequenceNumber,
                SendTime = Time.unscaledTime
            };

            sendSequenceNumber++;

            if (simulatedLatency.Equals(default(SimulatedLatency)))
                peer.ReceiveInternal(packet);
            else
            {
                float latency = UnityEngine.Random.Range(simulatedLatency.MinDelay, simulatedLatency.MaxDelay) / 1000f;

                LatencyPacket latencyPacket = new LatencyPacket()
                {
                    Packet = packet,
                    Latency = latency,
                    TargetPeer = peer
                };

                pendingSendPackets.Add(latencyPacket);
            }
        }

        public void SendToAll(byte[] data)
        {
            foreach (var peer in peers)
            {
                Send(peer, data);
            }
        }

        public void SetSimulatedLatency(SimulatedLatency simulatedLatency)
        {
            this.simulatedLatency = simulatedLatency;
        }

        private void ConnectionRequest(TransportLocal client)
        {
            // TODO: Decide reject/accept here.
            OnPeerConnectedInternal(client);
            client.OnPeerConnectedInternal(this);

            client.ID = peers.Count - 1;
        }

        private void OnPeerConnectedInternal(TransportLocal peer)
        {
            peers.Add(peer);

            OnPeerConnected?.Invoke();
        }

        private void ReceiveInternal(Packet packet)
        {
            pendingReceivedPackets.Insert(0, packet);
            pendingReceivedPackets.Sort(ComparePackets);
        }

        private int ComparePackets(Packet a, Packet b)
        {
            if (a.SequenceNumber < b.SequenceNumber)
            {
                return 1;
            }
            else if (a.SequenceNumber > b.SequenceNumber)
            {
                return -1;
            }
            else
            {
                return 0;
            }
        }
    }
}
