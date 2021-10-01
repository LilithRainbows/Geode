using System;
using System.Net;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using Geode.Habbo;
using Geode.Network;
using Geode.Habbo.Messages;
using Geode.Network.Protocol;
using System.Net.NetworkInformation;
using Geode.Habbo.Packages;
using System.Runtime.Loader;

namespace Geode.Extension
{
    public class GeodeExtension : IDisposable
    {
        public string Title { get; private set; }
        public string Description { get; private set; }
        public string Author { get; private set; }
        public bool UtilizingOnDoubleClick { get; private set; }
        public bool LeaveButtonVisible { get; private set; }

        public bool IsConnected { get; private set; } = false;
        public bool DisableEventHandlers = false;
        public event Action<DataInterceptedEventArgs> OnDataInterceptEvent;
        public event Action<HPacket> OnDoubleClickEvent;
        public event Action<HPacket> OnConnectedEvent;
        public event Action<HPacket> OnDisconnectedEvent;
        public event Action<string> OnCriticalErrorEvent;
        public event Action<int> OnEntitiesLoadedEvent;
        public event Action<int> OnWallItemsLoadedEvent;
        public event Action<int> OnFloorObjectsLoadedEvent;
        public event Action OnStoppingEvent;

        public bool MessagesInfo_Failed = false;
        private HNode _installer { get; set; }
        private GeodeExtension _container { get; set; }
        private Dictionary<ushort, Action<HPacket>> _extensionEvents { get; set; }
        public const ushort EXTENSION_INFO = 1;
        public const ushort MANIPULATED_PACKET = 2;
        public const ushort REQUEST_FLAGS = 3;
        public const ushort SEND_MESSAGE = 4;
        public const ushort EXTENSION_CONSOLE_LOG = 98;

        public Incoming In { get; private set; }
        public Outgoing Out { get; private set; }
        public string ClientVersion { get; private set; }
        public string ClientIdentifier { get; private set; }
        public string ClientType { get; private set; }
        public HotelEndPoint HotelServer { get; private set; }

        private IDictionary<int, HEntity> _entities { get; set; }
        public IDictionary<int, HEntity> Entities { get; set; }

        private IDictionary<int, HWallItem> _wallItems { get; set; }
        public IDictionary<int, HWallItem> WallItems { get; set; }

        private IDictionary<int, HFloorObject> _floorObjects { get; set; }
        public IDictionary<int, HFloorObject> FloorObjects { get; set; }

        public static IPEndPoint DefaultModuleServer { get; private set; }
        public List<HMessage> MessagesInfoIncoming { get; private set; }
        public List<HMessage> MessagesInfoOutgoing { get; private set; }

        private HMessage WaitForPacket_RequestedHMessage;
        private string WaitForPacket_ContainString;
        private DataInterceptedEventArgs WaitForPacket_ReturnedData;

        public GeodeExtension(string Title = "Geode extension", string Description = "", string Author = "", bool UtilizingOnDoubleClick = false, bool LeaveButtonVisible = false)
        {
            this.Title = Title;
            this.Description = Description;
            this.Author = Author;
            this.UtilizingOnDoubleClick = UtilizingOnDoubleClick;
            this.LeaveButtonVisible = LeaveButtonVisible;
            AssemblyLoadContext.Default.Unloading += AssemblyUnloading;
        }

        private void AssemblyUnloading(AssemblyLoadContext e)
        {
            if (DisableEventHandlers == false)
            {
                try { OnStoppingEvent.Invoke(); } catch { }; //Invoke event handler
            }
        }

        private string GetCommandLineArg(string RequestedParameter)
        {
            try
            {
                string[] CmdArgs = Environment.GetCommandLineArgs();
                for (int i = 0; i < CmdArgs.Count(); i++)
                {
                    if (CmdArgs[i] == RequestedParameter)
                    {
                        return CmdArgs[i + 1];
                    }
                }
            } catch
            {
                return string.Empty;
            }
            return string.Empty;
        }

        private Boolean IsNumeric(string valor)
        {
            int result;
            return int.TryParse(valor, out result);
        }

        public void Start(int ConnectionPort = 0)
        {
            try
            {
                if(IsNumeric(GetCommandLineArg("-p")))
                {
                    ConnectionPort = int.Parse(GetCommandLineArg("-p")); //Prioritize the CmdArg port
                }

                //Remove when automatic port detection is fixed
                if (ConnectionPort == 0)
                {
                    ConnectionPort = 9092;
                }
                //

                //if (ConnectionPort == 0)
                //{
                //    ConnectionPort = GetReadyToConnectGEarthPort();
                //}
                //if (ConnectionPort == 0)
                //{
                //    OnCriticalError("Could not find an available G-Earth port");
                //    return;
                //}

                DefaultModuleServer = new IPEndPoint(IPAddress.Parse("127.0.0.1"), ConnectionPort);
                _container = this;

                _extensionEvents = new Dictionary<ushort, Action<HPacket>>
                {
                    [1] = _container.OnDoubleClick,
                    [2] = _container.OnInfoRequest,
                    [3] = _container.OnPacketIntercept,
                    [4] = _container.OnFlagsCheck,
                    [5] = _container.OnConnected,
                    [6] = _container.OnDisconnected,
                    [7] = _container.OnInitialized
                };

                _entities = new ConcurrentDictionary<int, HEntity>();
                Entities = new ReadOnlyDictionary<int, HEntity>(_entities);

                _wallItems = new ConcurrentDictionary<int, HWallItem>();
                WallItems = new ReadOnlyDictionary<int, HWallItem>(_wallItems);

                _floorObjects = new ConcurrentDictionary<int, HFloorObject>();
                FloorObjects = new ReadOnlyDictionary<int, HFloorObject>(_floorObjects);

                _installer = HNode.ConnectAsync(DefaultModuleServer).GetAwaiter().GetResult();
                if (_installer == null) { OnCriticalError("Connection failed"); return; }
                SocketDisconnectionWatcher();
                bool HandleInstallerDataOK = false;
                int HandleInstallerDataRetries = 10;
                do
                {
                    try
                    {
                        HandleInstallerDataRetries -= 1;
                        Task handleInstallerDataTask = HandleInstallerDataAsync();
                        HandleInstallerDataOK = true;
                    }
                    catch
                    {
                        HandleInstallerDataOK = false;
                    }
                } while (HandleInstallerDataOK == false && HandleInstallerDataRetries > 0);
                if (HandleInstallerDataOK == false)
                {
                    OnCriticalError("HandleInstallerData failed");
                    return;
                }
            }
            catch
            {
                OnCriticalError("Generic start error");
                return;
            }
        }

        public static int GetReadyToConnectGEarthPort()
        {
            int DefaultPort = 9092;
            int DefaultMaxTries = 50;
            int CurrentPort = DefaultPort;
            int CurrentTry = 0;
            bool StopReached = false;
            while (!StopReached)
            {
                string RemotePortConnectionStatus = IsConnectedToRemotePort(CurrentPort);
                if (RemotePortConnectionStatus == "NO")
                {
                    return CurrentPort;
                }
                CurrentPort += 1;
                CurrentTry += 1;
                if (CurrentTry >= DefaultMaxTries)
                {
                    StopReached = true;
                }
            }
            return 0;
        }

        public static string IsConnectedToRemotePort(int port)
        {
            IPGlobalProperties properties = IPGlobalProperties.GetIPGlobalProperties();
            TcpConnectionInformation[] connections = properties.GetActiveTcpConnections();
            foreach (TcpConnectionInformation c in connections)
            {
                if (c.RemoteEndPoint.Port == port)
                {
                    return "YES";
                }
            }
            if (IsPortListening(port))
            {
                return "NO";
            }
            else
            {
                return "NO_UNLISTENING";
            }
        }

        public static bool IsPortListening(int port)
        {
            IPGlobalProperties ipProperties = IPGlobalProperties.GetIPGlobalProperties();
            IPEndPoint[] ipEndPoints = ipProperties.GetActiveTcpListeners();
            foreach (IPEndPoint endPoint in ipEndPoints)
            {
                if (endPoint.Port == port)
                {
                    return true;
                }
            }
            return false;
        }

        public void Stop()
        {
            if (DisableEventHandlers == false)
            {
                try { OnStoppingEvent.Invoke(); } catch { }; //Invoke event handler
            }
            Dispose();
        }

        public void OnEntitiesLoaded(int count)
        {
            if (DisableEventHandlers == false)
            {
                try { OnEntitiesLoadedEvent.Invoke(count); } catch { }; //Invoke event handler
            }
        }
        public void OnWallItemsLoaded(int count)
        {
            if (DisableEventHandlers == false)
            {
                try { OnWallItemsLoadedEvent.Invoke(count); } catch { }; //Invoke event handler
            }
        }
        public void OnFloorObjectsLoaded(int count)
        {
            if (DisableEventHandlers == false)
            {
                try { OnFloorObjectsLoadedEvent.Invoke(count); } catch { }; //Invoke event handler
            }
        }

        public virtual void OnFlagsCheck(HPacket packet)
        { }
        public virtual void OnDoubleClick(HPacket packet)
        {
            if (DisableEventHandlers == false)
            {
                try { OnDoubleClickEvent.Invoke(packet); } catch { }; //Invoke event handler
            }
        }
        public virtual void OnInfoRequest(HPacket packet)
        {
            var infoResponsePacket = new EvaWirePacket(EXTENSION_INFO);
            AssemblyName moduleAssemblyName = Assembly.GetAssembly(_container.GetType()).GetName();
            infoResponsePacket.Write(Title ?? moduleAssemblyName.Name); // Title
            infoResponsePacket.Write(Author ?? string.Empty); // Author
            infoResponsePacket.Write(moduleAssemblyName.Version.ToString()); // Version
            infoResponsePacket.Write(Description ?? string.Empty);
            infoResponsePacket.Write(UtilizingOnDoubleClick); // UtilizingOnDoubleClick

            infoResponsePacket.Write(!string.IsNullOrEmpty(GetCommandLineArg("-f"))); // IsInstalledExtension
            infoResponsePacket.Write(GetCommandLineArg("-f")); // FileName
            infoResponsePacket.Write(GetCommandLineArg("-c")); // Cookie

            infoResponsePacket.Write(LeaveButtonVisible); // LeaveButtonVisible
            infoResponsePacket.Write(!string.IsNullOrEmpty(GetCommandLineArg("-f"))); // DeleteButtonVisible

            _installer.SendPacketAsync(infoResponsePacket);
        }
        public virtual void OnPacketIntercept(HPacket packet)
        {
            int stringifiedInteceptionDataLength = packet.ReadInt32();
            string stringifiedInterceptionData = Encoding.GetEncoding("latin1").GetString(packet.ReadBytes(stringifiedInteceptionDataLength));

            var dataInterceptedArgs = new DataInterceptedEventArgs(stringifiedInterceptionData);
            OnDataIntercept(dataInterceptedArgs);
        }

        private async Task WaitForPacketReturnAsync()
        {
            while (WaitForPacket_RequestedHMessage is not null)
            {
                await Task.Delay(1);
            }
        }
        private async Task SocketDisconnectionWatcher()
        {
            while (_installer.IsConnected)
            {
                _installer.SendAsync(0);
                await Task.Delay(500);
            }
            Environment.Exit(0); //OnCriticalError("Socket disconnected");
        }
        public virtual async Task<DataInterceptedEventArgs> WaitForPacketAsync(HMessage RequestedMessage, int TimeOut, string ContainString = "")
        {
            WaitForPacket_RequestedHMessage = RequestedMessage;
            WaitForPacket_ContainString = ContainString;
            WaitForPacket_ReturnedData = null;
            await Task.WhenAny(WaitForPacketReturnAsync(), Task.Delay(TimeOut));
            WaitForPacket_RequestedHMessage = null;
            return WaitForPacket_ReturnedData;
        }
        public virtual void OnDataIntercept(DataInterceptedEventArgs data)
        {
            if (WaitForPacket_RequestedHMessage is not null)
            {
                if (WaitForPacket_RequestedHMessage.Id == data.Packet.Id && WaitForPacket_RequestedHMessage.IsOutgoing == data.IsOutgoing)
                {
                    bool SendWaitForPacketData = true;
                    if (WaitForPacket_ContainString != "")
                    {
                        SendWaitForPacketData = data.Packet.ToString().Contains(WaitForPacket_ContainString);
                    }
                    if (SendWaitForPacketData)
                    {
                        WaitForPacket_ReturnedData = data;
                        WaitForPacket_RequestedHMessage = null;
                    }
                }
            }

            if (IsConnected && DisableEventHandlers == false)
            {
                try { OnDataInterceptEvent.Invoke(data); } catch { };//Invoke event handler
            }
            if (MessagesInfo_Failed == false)
            {
                HandleGameObjects(data);
            }

            string stringified = data.ToString(true);
            _installer.SendPacketAsync(MANIPULATED_PACKET, stringified.Length, Encoding.GetEncoding("latin1").GetBytes(stringified));
        }

        public virtual void OnInitialized(HPacket packet)
        {
            _installer.SendPacketAsync(REQUEST_FLAGS);
        }
        public virtual void OnConnected(HPacket packet)
        {
            HotelServer = HotelEndPoint.Parse(packet.ReadUTF8(), packet.ReadInt32());
            ClientVersion = packet.ReadUTF8();
            ClientIdentifier = packet.ReadUTF8();
            ClientType = packet.ReadUTF8();
            try
            {
                MessagesInfoIncoming = new List<HMessage>();
                MessagesInfoOutgoing = new List<HMessage>();
                Out = new Outgoing(new List<HMessage>());
                In = new Incoming(new List<HMessage>());
                int MessagesInfoLenght = packet.ReadInt32();
                foreach (var i in Enumerable.Range(0, MessagesInfoLenght))
                {
                    int CurrentMessageID = packet.ReadInt32();
                    string CurrentMessageHash = packet.ReadUTF8();
                    string CurrentMessageName = packet.ReadUTF8();
                    string CurrentMessageStructure = packet.ReadUTF8();
                    bool CurrentMessageIsOutgoing = packet.ReadBoolean();
                    string CurrentMessageSource = packet.ReadUTF8();
                    if (string.IsNullOrWhiteSpace(CurrentMessageHash) || CurrentMessageHash == "NULL")
                    {
                        CurrentMessageHash = CurrentMessageName;
                    }
                    CurrentMessageHash = CurrentMessageSource + "_" + CurrentMessageHash;
                    HMessage CurrentHMessage = new HMessage((ushort)CurrentMessageID, CurrentMessageName, CurrentMessageIsOutgoing);
                    if (CurrentMessageIsOutgoing)
                    {
                        MessagesInfoOutgoing.Add(CurrentHMessage);
                    }
                    else
                    {
                        MessagesInfoIncoming.Add(CurrentHMessage);
                    }
                }
                List<HMessage> GeodeOut = new List<HMessage>();
                List<HMessage> GeodeIn = new List<HMessage>();
                foreach (PropertyInfo GeodeOutProperty in Out.GetType().GetProperties())
                {
                    try
                    {
                        if (GeodeOutProperty.PropertyType == typeof(HMessage))
                        {
                            GeodeOut.Add(MessagesInfoOutgoing.First(x => x.Name == GeodeOutProperty.Name));
                        }
                    }
                    catch { Console.WriteLine("MessageInfo not found for: " + GeodeOutProperty.Name); }
                }
                foreach (PropertyInfo GeodeInProperty in In.GetType().GetProperties())
                {
                    try
                    {
                        if (GeodeInProperty.PropertyType == typeof(HMessage))
                        {
                            GeodeIn.Add(MessagesInfoIncoming.First(x => x.Name == GeodeInProperty.Name));
                        }
                    }
                    catch { Console.WriteLine("MessageInfo not found for: " + GeodeInProperty.Name); }
                }
                Out = new Outgoing(GeodeOut);
                In = new Incoming(GeodeIn);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Critical MessagesInfo exception: " + ex.Message);
                MessagesInfo_Failed = true;
            }

            IsConnected = true;
            if (DisableEventHandlers == false)
            {
                try { OnConnectedEvent.Invoke(packet); } catch { };//Invoke event handler
            }
        }
        public virtual void OnDisconnected(HPacket packet)
        {
            IsConnected = false;
            _entities.Clear();
            _wallItems.Clear();
            _floorObjects.Clear();
            if (DisableEventHandlers == false)
            {
                try { OnDisconnectedEvent.Invoke(packet); } catch { }; //Invoke event handler
            }
        }
        public virtual void OnCriticalError(string error_desc)
        {
            Dispose();
            if (DisableEventHandlers == false)
            {
                try { OnCriticalErrorEvent.Invoke(error_desc); } catch { }; //Invoke event handler
            }
        }

        public Task<int> SendToClientAsync(byte[] data)
        {
            return _installer.SendPacketAsync(SEND_MESSAGE, false, data.Length, data);
        }
        public Task<int> SendToClientAsync(HPacket packet)
        {
            return SendToClientAsync(packet.ToBytes());
        }
        public Task<int> SendToClientAsync(ushort id, params object[] values)
        {
            return SendToClientAsync(EvaWirePacket.Construct(id, values));
        }

        public Task<int> SendToServerAsync(byte[] data)
        {
            return _installer.SendPacketAsync(SEND_MESSAGE, true, data.Length, data);
        }
        public Task<int> SendToServerAsync(HPacket packet)
        {
            return SendToServerAsync(packet.ToBytes());
        }
        public Task<int> SendToServerAsync(ushort id, params object[] values)
        {
            return SendToServerAsync(EvaWirePacket.Construct(id, values));
        }

        public HMessages GetMessages(bool isOutgoing) => isOutgoing ? (HMessages)Out : In;
        public HMessage GetMessage(ushort id, bool isOutgoing) => GetMessages(isOutgoing).GetMessage(id);
        public HMessage GetMessage(string identifier, bool isOutgoing) => GetMessages(isOutgoing).GetMessage(identifier);

        private async Task HandleInstallerDataAsync()
        {
            await Task.Yield();
            try
            {
                HPacket packet = await _installer.ReceivePacketAsync().ConfigureAwait(true);
                if (packet == null) { throw new Exception("Empty packet input"); }

                Task handleInstallerDataTask = HandleInstallerDataAsync();
                if (_extensionEvents.TryGetValue(packet.Id, out Action<HPacket> handler))
                {
                    handler(packet);
                }
            }
            catch { throw new Exception("Wrong packet input"); }
        }
        private void HandleGameObjects(DataInterceptedEventArgs data)
        {
            data.Packet.Position = 0;
            if (In.Users.Match(data))
            {
                HEntity[] entities = HEntity.Parse(data.Packet);
                foreach (HEntity entity in entities)
                {
                    _entities[entity.Index] = entity;
                }
                _container.OnEntitiesLoaded(entities.Length);
            }
            else if (In.Items.Match(data))
            {
                HWallItem[] wallItems = HWallItem.Parse(data.Packet);
                foreach (HWallItem wallItem in wallItems)
                {
                    _wallItems[wallItem.Id] = wallItem;
                }
                _container.OnWallItemsLoaded(wallItems.Length);
            }
            else if (In.ItemAdd.Match(data))
            {
                HWallItem wallItem = new HWallItem(data.Packet);
                _wallItems[wallItem.Id] = wallItem;
            }
            else if (In.ItemRemove.Match(data))
            {
                int wallItemId = int.Parse(data.Packet.ReadUTF8());
                data.Packet.Position = 0;
                _wallItems.Remove(wallItemId);
            }
            else if (In.Objects.Match(data))
            {
                HFloorObject[] floorObjects = HFloorObject.Parse(data.Packet);
                foreach (HFloorObject floorItem in floorObjects)
                {
                    _floorObjects[floorItem.Id] = floorItem;
                }
                _container.OnFloorObjectsLoaded(floorObjects.Length);
            }
            else if (In.ObjectAdd.Match(data))
            {
                HFloorObject floorObject = new HFloorObject(data.Packet);
                _floorObjects[floorObject.Id] = floorObject;
            }
            else if (In.ObjectRemove.Match(data))
            {
                int floorObjectId = int.Parse(data.Packet.ReadUTF8());
                data.Packet.Position = 0;
                _floorObjects.Remove(floorObjectId);
            }
            else if (In.FloorHeightMap.Match(data))
            {
                _entities.Clear();
                _wallItems.Clear();
                _floorObjects.Clear();
            }
            data.Packet.Position = 0;
        }
        public void Dispose()
        {
            try { Dispose(true); } catch { Console.WriteLine("WARNING: Dispose event failed."); }
        }
        protected virtual void Dispose(bool disposing)
        {
            try
            {
                if (disposing)
                {
                    _installer.Dispose();
                }
            }
            catch { Console.WriteLine("WARNING: Installer dispose event failed."); }
            _container.OnDisconnected(null);
        }
    }
}