using System.Collections.Generic;
using System.Threading.Tasks;
using ENet;

namespace ENetOnline
{
    public class Server
    {
        private bool _isStarted = false;
        
        private Host _host = new Host();

        private Event _networkEvent;

        private Task _listeningThread;
        
        private List<Peer> _connectedPeers = new List<Peer>();

        /// <summary>
        /// Gets the currently connected clients, this can be a expensive operation
        /// </summary>
        public ClientState[] ConnectedClients
        {
            get
            {
                ClientState[] clients = new ClientState[_connectedPeers.Count];

                for (int i = 0; i < clients.Length; i++)
                {
                    clients[i] = new ClientState(_connectedPeers[i]);
                }

                return clients;
            }
        }
        
        public delegate void ClientEventHandler(object sender, ClientEventArgs e);
        
        public delegate void PacketEventHandler(object sender, PacketEventArgs e);

        public event ClientEventHandler ClientConnected;
        public event ClientEventHandler ClientDisconnected;
        public event ClientEventHandler ClientTimedOut;
        public event PacketEventHandler PacketReceived;
        

        /// <summary>
        /// Creates a ENet server object
        /// </summary>
        /// <param name="listeningPort">The port used for incoming packets</param>
        /// <param name="clientsLimit">The max number of clients that can connect simultaneously</param>
        public Server(ushort listeningPort, int clientsLimit)
        {
            if (!LibraryState.IsInitialized)
                Library.Initialize();

            Address address = new Address();
            address.Port = listeningPort;
            _host.Create(address, clientsLimit);
        }

        /// <summary>
        /// Begin the server's continuous listen operation
        /// </summary>
        public void StartListening()
        {
            if (_isStarted)
                return;

            _isStarted = true;
            
            _listeningThread = Task.Run(ListeningThread);
        }

        /// <summary>
        /// Request the server to stop it's listening operation and waits for it to exit
        /// </summary>
        public void StopListening()
        {
            if (!_isStarted)
                return;
            
            _isStarted = false;
            
            _listeningThread.Wait();
        }
        
        public bool Send(ref byte[] packetBytes, ClientState client)
        {
            Packet packet = default(Packet);
            packet.Create(packetBytes);

            int index = _connectedPeers.FindIndex(x => x.IP == client.IpAddress);
            if (index == -1)
                return false;

            return _connectedPeers[index].Send(0, ref packet);
        }

        private void ListeningThread()
        {
            while (_isStarted)
            {
                bool polled = false;

                while (!polled)
                {
                    if (_host.CheckEvents(out _networkEvent) <= 0)
                    {
                        if (_host.Service(5, out _networkEvent) <= 0)
                        {
                            break;
                        }

                        polled = true;
                    }

                    switch (_networkEvent.Type)
                    {
                        case EventType.None:
                            break;

                        case EventType.Connect:
                            _connectedPeers.Add(_networkEvent.Peer);
                            ClientConnected?.Invoke(this, new ClientEventArgs(new ClientState(_networkEvent.Peer)));
                            break;

                        case EventType.Disconnect:
                            _connectedPeers.Remove(_networkEvent.Peer);
                            ClientDisconnected?.Invoke(this, new ClientEventArgs(new ClientState(_networkEvent.Peer)));
                            break;

                        case EventType.Timeout:
                            _connectedPeers.Remove(_networkEvent.Peer);
                            ClientTimedOut?.Invoke(this, new ClientEventArgs(new ClientState(_networkEvent.Peer)));
                            break;

                        case EventType.Receive:
                            byte[] payload = new byte[_networkEvent.Packet.Length];
                            _networkEvent.Packet.CopyTo(payload);
                            PacketReceived?.Invoke(this, new PacketEventArgs(ref payload, new ClientState(_networkEvent.Peer)));
                            _networkEvent.Packet.Dispose();
                            break;
                    }
                }
                
            }
            
            _host.Flush();
        }
    }
}