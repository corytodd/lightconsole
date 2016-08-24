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

namespace LightControl
{

    /// <summary>
    /// TCPConnected gateway client. This handles authentication, device/room discovery,
    /// and lighting controls.
    /// </summary>
    public partial class TCPConnected
    {

        private static readonly ILogger m_logger = LoggingFactory.GetLogger();


        #region Fields
        private bool m_hasToken;
        private string m_token;
        private List<Room> m_rooms;
        #endregion

        /// <summary>
        /// Constructoir
        /// </summary>
        /// <param name="host">Resolvable host name or IP address</param>
        public TCPConnected(string host)
        {
            if (string.IsNullOrEmpty(host))
            {
                throw new ArgumentException("host parameter is invalid");
            }

            Host = host;
            m_hasToken = false;
            m_token = string.Empty;
            m_rooms = new List<Room>();
        }


        #region Properties
        public string Host { get; private set; }
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
            return await Task.Factory.StartNew<bool>(() =>
            {
                if (LoadToken())
                {
                    UpdateState();
                }
                return m_hasToken;
            });
        }

        /// <summary>
        /// Returns a copy of the the current room's states
        /// </summary>
        /// <returns></returns>
        public IList<Room> GetRooms()
        {
            return new List<Room>(m_rooms);
        }

        /// <summary>
        /// Updates the current state of all known devices connected to the gateway.
        /// </summary>
        public void UpdateState()
        {
            Task.Factory.StartNew(() =>
            {
                var StateString = string.Format(GetStateTemplate, m_token);

                var payload = string.Format(
                    RequestUrlEncodeStr, 
                    Commands.GWRBatch, 
                    Uri.EscapeUriString(StateString));


                var result = GWRequest(payload);

                if (result.Equals(PermissedDeniedStr))
                {
                    m_logger.Log("Permission denied: Invalid Token");
                }
                else
                {
                    // NewtonSoft expexts XmlDoc, not LINQ XML
                    XmlDocument rawXml = new XmlDocument();
                    rawXml.LoadXml(result);

                    string json = JsonConvert.SerializeXmlNode(rawXml);
                    GWRObject gwo = JsonConvert.DeserializeObject<GWRObject>(json);

                    if (!gwo.hasRooms())
                    {
                        m_logger.Log("GWO Response is malformed: {0}", json.ToString());
                    }
                    else
                    {
                        var rooms = gwo.gwrcmds.gwrcmd.gdata.gip.room;

                        // See if these are new rooms or state changes
                        foreach (Room room in rooms)
                        {
                            m_logger.Log("Found room: {0}", room.name);

                            var prev = m_rooms.Where(x => x.name.Equals(room.name)).FirstOrDefault();

                            // new room
                            if (prev == null)
                            {
                                m_rooms.Add(room);
                                RoomDiscovered(new RoomEventArgs(room));
                            }
                            else if (!prev.Equals(room))
                            {
                                // Replace previous state
                                var index = m_rooms.IndexOf(prev);
                                m_rooms[index] = room;

                                RoomStateChanged(new RoomEventArgs(room));
                            }

                        }
                    }
                }
            });
        }


        /// <summary>
        /// Turns on the the specified device
        /// </summary>
        /// <param name="deviceId">Device id string (did)</param>
        public void TurnOnDevice(string deviceId)
        {
            var DeviceCommand = string.Format(DeviceSendTemplate, m_token, deviceId, 1);
            var payload = string.Format(
                RequestUrlEncodeStr, 
                Commands.DeviceSendCommand, 
                Uri.EscapeUriString(DeviceCommand));

            GWRequest(payload);
        }


        /// <summary>
        /// Turns off the the specified device
        /// </summary>
        /// <param name="deviceId">Device id string (did)</param>
        public void TurnOffDevice(string deviceId)
        {
            var DeviceCommand = string.Format(DeviceSendTemplate, m_token, deviceId, 0);

            var payload = string.Format(
                RequestUrlEncodeStr, 
                Commands.DeviceSendCommand,
                Uri.EscapeUriString(DeviceCommand));

            GWRequest(payload);
        }


        /// <summary>
        /// Sets the specified device's output to the specified level
        /// </summary>
        /// <param name="deviceId">Device id string (did)</param>
        /// <param name="level">int level between 0 and 100</param>
        public void SetDeviceLevel(int deviceId, int level)
        {
            var DeviceLevelCommand = string.Format(DeviceSendLevelTempalte, m_token, deviceId, level);

            var payload = string.Format(
                RequestUrlEncodeStr, 
                Commands.DeviceSendCommand, 
                Uri.EscapeUriString(DeviceLevelCommand));

            GWRequest(payload);
        }


        public int GetRoomHueByName(string name)
        {
            var room = m_rooms.Where(x => x.name.Equals(name)).First();

            var color = room.color;

            // Grab the hex representation of red (chars 1-2) and convert to decimal (base 10).
            var r = Convert.ToInt32(color.Substring(0, 2), 16);
            var g = Convert.ToInt32(color.Substring(2, 2), 16);
            var b = Convert.ToInt32(color.Substring(4, 2), 16);

            m_logger.Log("Hue: {0}.{1}.{2}", r, g, b);

            var rgb = new Rgb { R = r, G = b, B = b };
            var hsv = rgb.To<Hsv>();

            return Convert.ToInt32(hsv.H * 182);
        }


        public RoomState GetRoomStateByName(string name)
        {
            var room = m_rooms.Where(x => x.name.Equals(name)).First();

            bool on = false;
            int deviceCount = 0;
            int levelTotal = 0;

            var devices = room.device;
            if (string.IsNullOrEmpty(room.device.did))
            {
                throw new NotImplementedException("List of devices is not implemented");
            }
            else
            {
                deviceCount++;
                if (!devices.state.Equals("0"))
                {
                    on = true;
                    levelTotal += Int32.Parse(devices.level);
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
            var RoomCommand = string.Format(RoomSendTemplate, m_token, roomId, 1);

            var payload = string.Format(
                RequestUrlEncodeStr,
                Commands.RoomSendCommand, 
                Uri.EscapeUriString(RoomCommand));

            GWRequest(payload);
        }


        /// <summary>
        /// Turns on the the specified room
        /// </summary>
        /// <param name="name">Room name</param>
        public void TurnOnRoomByName(string name)
        {
            var roomId = GetRIDByName(name);

            TurnOnRoom(roomId);
        }


        /// <summary>
        /// Turns on the the specified room to the specified level
        /// </summary>
        /// <param name="name">Room name</param>
        /// <param name="level">Int level 0 - 100</param>
        public void TurnOnRoomWithLevelByName(string name, int level)
        {
            var roomId = this.GetRIDByName(name);

            SetRoomLevel(roomId, level);
            TurnOnRoom(roomId);
        }


        /// <summary>
        /// Turn off power to room
        /// </summary>
        /// <param name="roomId">string room id (rid)</param>
        public void TurnOffRoom(string roomId)
        {
            var RoomCommand = string.Format(RoomSendTemplate, m_token, roomId, 0);

            var payload = string.Format(
                RequestUrlEncodeStr, 
                Commands.RoomSendCommand, 
                Uri.EscapeUriString(RoomCommand));

            GWRequest(payload);
        }


        /// <summary>
        /// Turns off power to room
        /// </summary>
        /// <param name="name">string room name</param>
        public void TurnOffRoomByName(string name)
        {
            var roomId = GetRIDByName(name);

            TurnOffRoom(roomId);
        }


        /// <summary>
        /// Sets room light level
        /// </summary>
        /// <param name="roomId">string room id (rid)</param>
        /// <param name="level">int level 0 - 100</param>
        public void SetRoomLevel(string roomId, int level)
        {
            var RoomLevelCommand = string.Format(RoomSendLevelTemplate, m_token, roomId, level);

            var payload = string.Format(
                RequestUrlEncodeStr,
                Commands.RoomSendCommand,
                Uri.EscapeUriString(RoomLevelCommand));

            GWRequest(payload);
        }


        /// <summary>
        /// Set room level
        /// </summary>
        /// <param name="name">string room name</param>
        /// <param name="level">int level 0 - 100</param>
        public void SetRoomLevelByName(string name, int level)
        {
            var roomId = GetRIDByName(name);

            SetRoomLevel(roomId, level);
        }
        #endregion

        #region Delegates
        protected virtual void RoomDiscovered(RoomEventArgs e)
        {
            EventHandler<RoomEventArgs> handler = OnRoomDiscovered;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        protected virtual void RoomStateChanged(RoomEventArgs e)
        {
            EventHandler<RoomEventArgs> handler = OnRoomStateChanged;
            if (handler != null)
            {
                handler(this, e);
            }
        }
        #endregion


        #region Private
        /// <summary>
        /// Post XML paylod to FGW host
        /// </summary>
        /// <param name="payload">XML string</param>
        /// <returns>XML response</returns>
        private string GWRequest(string payload)
        {
            if (!m_hasToken)
            {
                // @todo this really should be an error
                return "Missing auth token";
            }

            return postGWRXMLData(Host, payload);
        }


        /// <summary>
        /// Attempts to obtain an access token from the TCPConnected gateway
        /// </summary>
        /// <returns>bool True on success</returns>
        private bool SyncGateway()
        {
            var uuid = Guid.NewGuid();
            var user = uuid;
            var pass = uuid;

            var gLogInCommand = string.Format(LogInTemplate, user, pass);
            var payload = string.Format(RequestUrlEncodeStr, Commands.GWRLogin, Uri.EscapeUriString(gLogInCommand));

            var resp = postGWRXMLData(Host, payload);

            if (resp.Equals(NotInSyncModeStr))
            {
                m_logger.Log("Permission Denied: Gateway is not in Sync mode. Putton on Gateway to Sync");
                return false;
            }
            else
            {
                XElement xtoken = XElement.Parse(resp).Element("token");

                var cfg = new Config()
                {
                    token = xtoken.Value ?? "Missing token attribute"
                };

                string serialized = JsonConvert.SerializeObject(cfg);

                using (StreamWriter sr = new StreamWriter(ConfigPath))
                {
                    sr.Write(serialized);
                }

                return true;
            }

        }


        /// <summary>
        /// Loads auth token from disk, creates file if not found and requests new token
        /// </summary>
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
                        token = cfg.token;
                    }
                }

            }

            if (string.IsNullOrEmpty(token))
            {
                m_logger.Log("No token found, attempting to get token");
                m_hasToken = SyncGateway();
            }
            else
            {
                m_hasToken = true;
                m_token = token;
            }

            return m_hasToken;
        }

        /// <summary>
        /// Finds the id associated with room string name.
        /// @todo what to do if name does not exist?
        /// </summary>
        /// <param name="name">String name of room</param>
        /// <returns>string room id</returns>
        private string GetRIDByName(string name)
        {
            var room = m_rooms.Where(x => x.name.Equals(name)).First();
            return room.rid;
        }

        /// <summary>
        /// Posts XML data to the TCPConnected GWR path
        /// </summary>
        /// <param name="hostIP"></param>
        /// <param name="requestXml"></param>
        /// <returns></returns>
        private static string postGWRXMLData(string hostIP, string requestXml)
        {
            var http = new HttpClient();
            http.Request.Accept = HttpContentTypes.ApplicationXml;

            var url = string.Format("https://{0}/gwr/gop.php", hostIP);
            m_logger.Log("PostXMLData Req: \n{0}", requestXml);


            HttpResponse resp = http.Post(url, requestXml, HttpContentTypes.ApplicationXml);

            m_logger.Log("PostXMLData Resp: \n{0}", XDocument.Parse(resp.RawText).ToString());

            return resp.RawText;

        }    
        #endregion
    }
}
