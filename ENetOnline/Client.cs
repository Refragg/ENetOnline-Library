using System.Threading.Tasks;
using ENet;

namespace ENetOnline
{
    public class Client
    {
        private bool _isStarted = false;
        
        private Host _client = new Host();
        
        private Peer _serverPeer;

        private Event _networkEvent;

        private Task _listeningThread;

        public ClientState ServerClient
        {
            get
            {
                return new ClientState(_serverPeer);
            }
        }

        public delegate void ClientEventHandler(object sender, ClientEventArgs e);
        
        public delegate void PacketEventHandler(object sender, PacketEventArgs e);

        public event ClientEventHandler ClientConnected;
        public event ClientEventHandler ClientDisconnected;
        public event ClientEventHandler ClientTimedOut;
        public event PacketEventHandler PacketReceived;

        /// <summary>
        /// Creates a ENet client object to connect to other servers
        /// </summary>
        public Client()
        {
            if (!LibraryState.IsInitialized)
                Library.Initialize();
            
            _client = new Host();
            _client.Create();
        }

        /// <summary>
        /// Connect to an other server synchronously
        /// </summary>
        /// <param name="hostOrIp">The server address to connect to</param>
        /// <param name="port">The port to connect to</param>
        /// <returns>Whether it succesfully connected or not</returns>
        public void ConnectAsync(string hostOrIp, ushort port)
        {
            Address address = new Address();

            address.SetHost(hostOrIp);
            address.Port = port;
            _serverPeer = _client.Connect(address);
        }

        public void Disconnect(uint data)
        {
            _serverPeer.Disconnect(data);
        }

        /// <summary>
        /// Begin the client's continuous listen operation
        /// </summary>
        public void StartListening()
        {
            if (_isStarted)
                return;

            _isStarted = true;
            
            _listeningThread = Task.Run(ListeningThread);
        }

        /// <summary>
        /// Request the client to stop it's listening operation and waits for it to exit
        /// </summary>
        public void StopListening()
        {
            if (!_isStarted)
                return;
            
            _isStarted = false;
            
            _listeningThread.Wait();
        }

        public bool Send(ref byte[] packetBytes)
        {
            Packet packet = default(Packet);
            packet.Create(packetBytes);

            return _serverPeer.Send(0, ref packet);
        }

        private void ListeningThread()
        {
            while (_isStarted)
            {
                bool polled = false;

                while (!polled)
                {
                    if (_client.CheckEvents(out _networkEvent) <= 0)
                    {
                        if (_client.Service(5, out _networkEvent) <= 0)
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
                            ClientConnected?.Invoke(this, new ClientEventArgs(new ClientState(_networkEvent.Peer)));
                            break;

                        case EventType.Disconnect:
                            ClientDisconnected?.Invoke(this, new ClientEventArgs(new ClientState(_networkEvent.Peer)));
                            break;

                        case EventType.Timeout:
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
            
            _client.Flush();
        }
    }
}