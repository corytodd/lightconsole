using System;

namespace LightControl
{
    public class RoomEventArgs : EventArgs
    {
        public Room Room { get; private set; }

        public RoomEventArgs(Room room) { Room = room; }
    }
}
