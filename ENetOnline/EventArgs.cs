using System;

namespace ENetOnline
{
    public class PacketEventArgs : EventArgs
    {
        public byte[] Payload { get; }
        
        public ClientState Client { get; }
        
        internal PacketEventArgs(ref byte[] payload, ClientState clientState)
        {
            Payload = payload;
            Client = clientState;
        }
    }

    public class ClientEventArgs : EventArgs
    {
        public ClientState Client { get; }
        
        internal ClientEventArgs(ClientState clientState)
        {
            Client = clientState;
        }
    }
}