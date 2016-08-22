using System;

namespace LightConsole
{
    public class ModifyLightArgs : EventArgs
    {
        public string Name { get; private set; }
        public int Level { get; private set; }
        public bool On { get; private set; }

        public ModifyLightArgs(string name, int level, bool on)
        {
            Name = name;
            Level = level;
            On = on;
        }
    }
}
