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
        public event Action<int> OnPeerConnected;
        public event Action<int> OnPeerDisconnected;
        public event Action<int, float, byte[]> OnReceive;

        private int ID;

        private struct Packet
        {
            public int SendingPeerID;
            public int SequenceNumber;
            public byte[] Data;
            public float SendTime;
            public DeliveryMethod DeliveryMethod;
        }

        private class Channel
        {
            public readonly List<Packet> PendingReceived = new List<Packet>();
            public int LastReceivedSequenceNumber = -1;
            public int SendSequenceNumber;
        }

        private class Peer
        {
            public readonly TransportLocal Transport;

            public readonly Channel ReliableChannel = new Channel();
            public readonly Channel ReliableOrderedChannel = new Channel();

            public Peer(TransportLocal transport)
            {
                Transport = transport;
            }
        }

        private readonly Dictionary<int, Peer> peers = new Dictionary<int, Peer>();

        private static TransportLocal listeningTransport;

        private struct LatencyPacket
        {
            public Packet Packet;
            public float Latency;
            public TransportLocal TargetPeer;
        }

        private readonly List<LatencyPacket> pendingSendPackets = new List<LatencyPacket>();

        private SimulatedLatency simulatedLatency;

        public override string ToString()
        {
            return $"{(this == listeningTransport ? "Server" : "Client")} {ID}";
        }

        public void Listen(int _)
        {
            if (listeningTransport != null)
                throw new Exception("Already is a currently listening local transport.");

            listeningTransport = this;
        }

        public void Connect(string _, int __)
        {
            if (listeningTransport == null)
                throw new Exception("No listening local transport.");

            listeningTransport.ConnectionRequest(this);
        }

        public void Update()
        {
            foreach (var peer in peers.Values)
            {
                ProcessChannel(peer.ReliableOrderedChannel, true);
                ProcessChannel(peer.ReliableChannel, false);
            }

            // Send packets with simulated latency.
            for (int i = 0; i < pendingSendPackets.Count; i++)
            {
                LatencyPacket packet = pendingSendPackets[i];

                if (Time.time > packet.Latency + packet.Packet.SendTime)
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

            foreach (var peer in peers.Values)
            {
                peer.Transport.OnPeerDisconnectedInternal(this);
            }

            peers.Clear();
        }

        public void SetSimulatedLatency(SimulatedLatency simulatedLatency)
        {
            this.simulatedLatency = simulatedLatency;
        }

        public void Send(int peerId, byte[] data, DeliveryMethod deliveryMethod)
        {
            Peer peer = peers[peerId];
            TransportLocal transport = peer.Transport;

            Channel channel = deliveryMethod == DeliveryMethod.ReliableOrdered ? peer.ReliableOrderedChannel : peer.ReliableChannel;

            Packet packet = new Packet()
            {
                SendingPeerID = ID,
                Data = data,
                SequenceNumber = channel.SendSequenceNumber,
                SendTime = Time.time,
                DeliveryMethod = deliveryMethod
            };

            channel.SendSequenceNumber++;

            if (simulatedLatency.Equals(default(SimulatedLatency)))
                transport.ReceiveInternal(packet);
            else
            {
                float latency = UnityEngine.Random.Range(simulatedLatency.MinDelay, simulatedLatency.MaxDelay) / 1000f;

                LatencyPacket latencyPacket = new LatencyPacket()
                {
                    Packet = packet,
                    Latency = latency,
                    TargetPeer = transport
                };

                pendingSendPackets.Add(latencyPacket);
            }
        }

        public void SendToAll(byte[] data, DeliveryMethod deliveryMethod)
        {
            foreach (var peer in peers)
            {
                Send(peer.Key, data, deliveryMethod);
            }
        }

        private void ProcessChannel(Channel channel, bool ordered)
        {
            while (channel.PendingReceived.Count > 0)
            {
                Packet packet = channel.PendingReceived[channel.PendingReceived.Count - 1];

                if (!ordered || packet.SequenceNumber == channel.LastReceivedSequenceNumber + 1)
                {
                    channel.PendingReceived.RemoveAt(channel.PendingReceived.Count - 1);
                    channel.LastReceivedSequenceNumber++;

                    float rtt = (Time.time - packet.SendTime) * 2;

                    OnReceive?.Invoke(packet.SendingPeerID, rtt, packet.Data);
                }
                else
                {
                    break;
                }
            }
        }

        private void ConnectionRequest(TransportLocal client)
        {
            // TODO: Decide reject/accept here.
            client.ID = peers.Count;

            client.peers.Add(ID, new Peer(this));
            peers.Add(client.ID, new Peer(client));

            OnPeerConnectedInternal(client);
            client.OnPeerConnectedInternal(this);
        }

        private void OnPeerConnectedInternal(TransportLocal peer)
        {
            OnPeerConnected?.Invoke(peer.ID);
        }

        private void OnPeerDisconnectedInternal(TransportLocal peer)
        {
            peers.Remove(peer.ID);

            OnPeerDisconnected?.Invoke(peer.ID);
        }

        private void ReceiveInternal(Packet packet)
        {
            Peer peer = peers[packet.SendingPeerID];

            if (packet.DeliveryMethod == DeliveryMethod.ReliableOrdered)
            {
                peer.ReliableOrderedChannel.PendingReceived.Insert(0, packet);
                peer.ReliableOrderedChannel.PendingReceived.Sort(ComparePackets);
            }
            else
            {
                peer.ReliableChannel.PendingReceived.Insert(0, packet);
            }
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
