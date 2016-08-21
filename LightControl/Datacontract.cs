namespace LightControl
{
    public partial class TCPConnected
    {
        /// <summary>
        /// TCPConnected authorization config
        /// </summary>
        struct Config
        {
            /// <summary>
            /// Authorization token
            /// </summary>
            public string token { get; set; }
        }

        /// <summary>
        /// TCPConnected Room state
        /// </summary>
        public struct RoomState
        {
            /// <summary>
            /// Light is off or on
            /// </summary>
            public bool On { get; set; }

            /// <summary>
            /// Level is 0-100 where 100 is full on. This
            /// is the average level all devices in a room configuration.
            /// </summary>
            public int Level { get; set; }
        }
    }
}
