using ENet;

namespace ENetOnline
{
    public struct ClientState
    {
        public string IpAddress { get; }
        
        public PeerState ConnectionState { get; }
        
        public ulong PacketsLost { get; }
        public ulong PacketsSent { get; }
        
        public uint LastReceiveTime { get; }
        public uint LastSendTime { get; }
        
        public uint Ping { get; }
        
        internal ClientState(Peer peer)
        {
            IpAddress = peer.IP;
            ConnectionState = peer.State;
            PacketsLost = peer.PacketsLost;
            PacketsSent = peer.PacketsSent;
            LastReceiveTime = peer.LastReceiveTime;
            LastSendTime = peer.LastSendTime;
            Ping = peer.RoundTripTime;
        }
    }
}