﻿using Geode.Network.Protocol;

namespace Geode.Habbo.Packages
{
    public class HAchievementLevel
    {
        public string BadgeId { get; }

        public int Level { get; set; }
        public int PointLimit { get; set; }

        public HAchievementLevel(string name, HPacket packet)
        {
            Level = packet.ReadInt32();
            PointLimit = packet.ReadInt32();

            BadgeId = "ACH_" + name + Level;
        }
    }
}
