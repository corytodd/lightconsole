using EasyHttp.Http;
using Newtonsoft.Json;
using System.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using System.Xml;
using ColorMine.ColorSpaces;
using System.Threading.Tasks;
using System.Threading;

namespace LightControl
{

    /// <summary>
    /// TCPConnected gateway client. This handles authentication, device/room discovery,
    /// and lighting controls.
    /// </summary>
    public partial class TcpConnected
    {

        private static readonly ILogger MLogger = LoggingFactory.GetLogger();


        #region Fields
        private bool _mHasToken;
        private string _mToken;
        private readonly HashSet<Room> _mRooms;
        #endregion

        /// <summary>
        /// Constructoir
        /// </summary>
        /// <param name="host">Resolvable host name or IP address</param>
        public TcpConnected(string host)
        {
            if (string.IsNullOrEmpty(host))
            {
                throw new ArgumentException("host parameter is invalid");
            }

            Host = host;
            _mHasToken = false;
            _mToken = string.Empty;
            _mRooms = new HashSet<Room>();
            PollRate = 30;
        }


        #region Properties
        public string Host { get; }

        /// <summary>
        /// Poll rate in seconds to query for room updates
        /// </summary>
        public int PollRate { get; set; }
        #endregion

        #region Events
        /// <summary>
        /// Raised when a new room is discovered
        /// </summary>
        public event EventHandler<RoomEventArgs> OnRoomDiscovered;

        /// <summary>
        /// Raised when a room's state changes
        /// </summary>
        public event EventHandler<RoomEventArgs> OnRoomStateChanged;
        #endregion

        #region Methods
        /// <summary>
        /// Initialize the client's authorization tokens
        /// </summary>
        public async Task<bool>InitAsync()
        {
            return await Task.Factory.StartNew(() =>
            {
                if (!LoadToken())
                {
                    return false;
                }

                int retry = 5;
                while (retry-- > 0)
                {
                    try
                    {
                        UpdateState();
                        break;
                    }
                    catch (InvalidToken)
                    {
                        LoadToken();
                    }
                    catch (MalformedGwr)
                    {
                        // just try again
                    }
                }

                UpdateState();
                return true;
            });
        }

        /// <summary>
        /// Returns a copy of the the current room's states
        /// </summary>
        /// <returns></returns>
        public IList<Room> GetRooms()
        {
            return new List<Room>(_mRooms);
        }


        /// <summary>
        /// Turns on the the specified device
        /// </summary>
        /// <param name="deviceId">Device id string (did)</param>
        public void TurnOnDevice(string deviceId)
        {
            var deviceCommand = string.Format(DeviceSendTemplate, _mToken, deviceId, 1);
            var payload = string.Format(
                RequestUrlEncodeStr, 
                Commands.DeviceSendCommand, 
                Uri.EscapeUriString(deviceCommand));

            GwRequest(payload);
        }


        /// <summary>
        /// Turns off the the specified device
        /// </summary>
        /// <param name="deviceId">Device id string (did)</param>
        public void TurnOffDevice(string deviceId)
        {
            var deviceCommand = string.Format(DeviceSendTemplate, _mToken, deviceId, 0);

            var payload = string.Format(
                RequestUrlEncodeStr, 
                Commands.DeviceSendCommand,
                Uri.EscapeUriString(deviceCommand));

            GwRequest(payload);
        }


        /// <summary>
        /// Sets the specified device's output to the specified level
        /// </summary>
        /// <param name="deviceId">Device id string (did)</param>
        /// <param name="level">int level between 0 and 100</param>
        public void SetDeviceLevel(int deviceId, int level)
        {
            var deviceLevelCommand = string.Format(DeviceSendLevelTempalte, _mToken, deviceId, level);

            var payload = string.Format(
                RequestUrlEncodeStr, 
                Commands.DeviceSendCommand, 
                Uri.EscapeUriString(deviceLevelCommand));

            GwRequest(payload);
        }


        public int GetRoomHueByName(string name)
        {
            var room = _mRooms.First(x => x.Name.Equals(name));

            var color = room.Color;

            // Grab the hex representation of red (chars 1-2) and convert to decimal (base 10).
            var r = Convert.ToInt32(color.Substring(0, 2), 16);
            var g = Convert.ToInt32(color.Substring(2, 2), 16);
            var b = Convert.ToInt32(color.Substring(4, 2), 16);

            MLogger.Log("Hue: {0}.{1}.{2}", r, g, b);

            var rgb = new Rgb { R = r, G = b, B = b };
            var hsv = rgb.To<Hsv>();

            return Convert.ToInt32(hsv.H * 182);
        }


        public RoomState GetRoomStateByName(string name)
        {
            var room = _mRooms.First(x => x.Name.Equals(name));

            bool on = false;
            int deviceCount = 0;
            int levelTotal = 0;

            var devices = room.Device;
            if (string.IsNullOrEmpty(room.Device.Did))
            {
                throw new NotImplementedException("List of devices is not implemented");
            }
            else
            {
                deviceCount++;
                if (!devices.State.Equals("0"))
                {
                    on = true;
                    levelTotal += Int32.Parse(devices.Level);
                }
            }

            return new RoomState()
            {
                On = on,
                Level = levelTotal / deviceCount
            };

        }


        /// <summary>
        /// Turns on the the specified room
        /// </summary>
        /// <param name="roomId">Room id string (rid)</param>
        public void TurnOnRoom(string roomId)
        {
            var roomCommand = string.Format(RoomSendTemplate, _mToken, roomId, 1);

            var payload = string.Format(
                RequestUrlEncodeStr,
                Commands.RoomSendCommand, 
                Uri.EscapeUriString(roomCommand));

            GwRequest(payload);
        }


        /// <summary>
        /// Turns on the the specified room
        /// </summary>
        /// <param name="name">Room name</param>
        public void TurnOnRoomByName(string name)
        {
            var roomId = GetRidByName(name);

            TurnOnRoom(roomId);
        }


        /// <summary>
        /// Turns on the the specified room to the specified level
        /// </summary>
        /// <param name="name">Room name</param>
        /// <param name="level">Int level 0 - 100</param>
        public void TurnOnRoomWithLevelByName(string name, int level)
        {
            var roomId = GetRidByName(name);

            SetRoomLevel(roomId, level);
            TurnOnRoom(roomId);
        }


        /// <summary>
        /// Turn off power to room
        /// </summary>
        /// <param name="roomId">string room id (rid)</param>
        public void TurnOffRoom(string roomId)
        {
            var roomCommand = string.Format(RoomSendTemplate, _mToken, roomId, 0);

            var payload = string.Format(
                RequestUrlEncodeStr, 
                Commands.RoomSendCommand, 
                Uri.EscapeUriString(roomCommand));

            GwRequest(payload);
        }


        /// <summary>
        /// Turns off power to room
        /// </summary>
        /// <param name="name">string room name</param>
        public void TurnOffRoomByName(string name)
        {
            var roomId = GetRidByName(name);

            TurnOffRoom(roomId);
        }


        /// <summary>
        /// Sets room light level
        /// </summary>
        /// <param name="roomId">string room id (rid)</param>
        /// <param name="level">int level 0 - 100</param>
        public void SetRoomLevel(string roomId, int level)
        {
            var roomLevelCommand = string.Format(RoomSendLevelTemplate, _mToken, roomId, level);

            var payload = string.Format(
                RequestUrlEncodeStr,
                Commands.RoomSendCommand,
                Uri.EscapeUriString(roomLevelCommand));

            GwRequest(payload);
        }


        /// <summary>
        /// Set room level
        /// </summary>
        /// <param name="name">string room name</param>
        /// <param name="level">int level 0 - 100</param>
        public void SetRoomLevelByName(string name, int level)
        {
            var roomId = GetRidByName(name);

            SetRoomLevel(roomId, level);
        }
        #endregion

        #region Delegates
        protected virtual void RoomDiscovered(RoomEventArgs e)
        {
            EventHandler<RoomEventArgs> handler = OnRoomDiscovered;
            handler?.Invoke(this, e);
        }

        protected virtual void RoomStateChanged(RoomEventArgs e)
        {
            EventHandler<RoomEventArgs> handler = OnRoomStateChanged;
            handler?.Invoke(this, e);
        }
        #endregion


        #region Private
        // ReSharper disable once UnusedMember.Local
        private void UpdateStateTask()
        {
            Task.Factory.StartNew(() =>
            {
                while(_mHasToken)
                {
                    UpdateState();

                    Thread.Sleep(PollRate * 1000);
                }

            });
        }

        /// <summary>
        /// Updates the current state of all known devices connected to the gateway.
        /// </summary>
        private void UpdateState()
        {

            var stateString = string.Format(GetStateTemplate, _mToken);

            var payload = string.Format(
                RequestUrlEncodeStr,
                Commands.GwrBatch,
                Uri.EscapeUriString(stateString));


            var result = GwRequest(payload);

            if (result.Equals(PermissedDeniedStr))
            {
                MLogger.Log("Permission denied: Invalid Token");
                throw new InvalidToken();
            }
            else if(string.IsNullOrEmpty(result))
            {
                throw new TcpGatewayUnavailable();
            }
            else
            {
                // NewtonSoft expexts XmlDoc, not LINQ XML
                XmlDocument rawXml = new XmlDocument();
                rawXml.LoadXml(result);

                string json = JsonConvert.SerializeXmlNode(rawXml);
                GwrObject gwo = JsonConvert.DeserializeObject<GwrObject>(json);

                if (!gwo.HasRooms())
                {
                    MLogger.Log("GWO Response is malformed: {0}", json);
                    throw new MalformedGwr();
                }
                else
                {
                    var rooms = gwo.Gwrcmds.Gwrcmd.Gdata.Gip.Room;

                    // See if these are new rooms or state changes
                    foreach (Room room in rooms)
                    {
                        MLogger.Log("Found room: {0}", room.Name);

                        var prev = _mRooms.FirstOrDefault(x => x.Name.Equals(room.Name));
                        _mRooms.Add(room);

                        // new room
                        if (prev == null)
                        {
                            RoomDiscovered(new RoomEventArgs(room));
                        }
                        else if (!prev.Equals(room))
                        {
                            RoomStateChanged(new RoomEventArgs(room));
                        }

                    }
                }
            }
        }

        /// <summary>
        /// Post XML paylod to FGW host
        /// </summary>
        /// <param name="payload">XML string</param>
        /// <returns>XML response</returns>
        private string GwRequest(string payload)
        {
            if (!_mHasToken)
            {
                // @todo this really should be an error
                return "Missing auth token";
            }

            return PostGwrxmlData(Host, payload);
        }


        /// <summary>
        /// Attempts to obtain an access token from the TCPConnected gateway
        /// </summary>
        /// <returns>bool True on success</returns>
        /// <exception cref="NotInSyncModeException">Thrown if authorization sync fails due to
        /// a 404 message from the gateway</exception>
        private bool SyncGateway()
        {
            var uuid = Guid.NewGuid();
            var user = uuid;
            var pass = uuid;

            var gLogInCommand = string.Format(LogInTemplate, user, pass);
            var payload = string.Format(RequestUrlEncodeStr, Commands.GwrLogin, Uri.EscapeUriString(gLogInCommand));

            var resp = PostGwrxmlData(Host, payload);

            if (resp.Equals(NotInSyncModeStr))
            {
                throw new NotInSyncModeException();
            }
            else if(string.IsNullOrEmpty(resp))
            {
                throw new TcpGatewayUnavailable();
            }
            else
            {
                var xtoken = XElement.Parse(resp).Element("token");

                if (xtoken == null) return true;
                var cfg = new Config()
                {
                    Token = xtoken.Value
                };

                var serialized = JsonConvert.SerializeObject(cfg);

                using (var sr = new StreamWriter(ConfigPath))
                {
                    sr.Write(serialized);
                }

                return true;
            }

        }


        /// <summary>
        /// Loads auth token from disk, creates file if not found and requests new token
        /// </summary>
        /// <exception cref="NotInSyncModeException"></exception>
        private bool LoadToken()
        {
            string token = string.Empty;

            if (File.Exists(ConfigPath))
            {

                using (StreamReader r = new StreamReader(ConfigPath))
                {
                    string json = r.ReadToEnd();
                    if (!string.IsNullOrEmpty(json))
                    {

                        Config cfg = JsonConvert.DeserializeObject<Config>(json);
                        token = cfg.Token;
                    }
                }

            }

            if (string.IsNullOrEmpty(token))
            {
                MLogger.Log("No token found, attempting to get token");
                _mHasToken = SyncGateway();
            }
            else
            {
                _mHasToken = true;
                _mToken = token;
            }

            return _mHasToken;
        }

        /// <summary>
        /// Finds the id associated with room string name.
        /// @todo what to do if name does not exist?
        /// </summary>
        /// <param name="name">String name of room</param>
        /// <returns>string room id</returns>
        private string GetRidByName(string name)
        {
            var room = _mRooms.First(x => x.Name.Equals(name));
            return room.Rid;
        }

        /// <summary>
        /// Posts XML data to the TCPConnected GWR path
        /// </summary>
        /// <param name="hostIp"></param>
        /// <param name="requestXml"></param>
        /// <returns></returns>
        private static string PostGwrxmlData(string hostIp, string requestXml)
        {
            var http = new HttpClient();
            http.Request.Accept = HttpContentTypes.ApplicationXml;

            var url = $"https://{hostIp}/gwr/gop.php";
            MLogger.Log("PostXMLData Req: \n{0}", requestXml);

            try
            {
                HttpResponse resp = http.Post(url, requestXml, HttpContentTypes.ApplicationXml);
                MLogger.Log("PostXMLData Resp: \n{0}", XDocument.Parse(resp.RawText).ToString());
                return resp.RawText;

            }
            catch (System.Net.WebException e)
            {
                MLogger.Log("Failed to make server request: {0}", e.Message);
                return "";
            }



        }    
        #endregion
    }
}
