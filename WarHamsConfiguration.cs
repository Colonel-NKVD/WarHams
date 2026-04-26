using Rocket.API;
using System.Collections.Generic;
using UnityEngine;

namespace WarHams
{
    public class ZoneData
    {
        public string Id;
        public string Name;
        public Vector3 Position;
        public float Radius;
        public string Owner; // "USA", "GER", "Neutral"
    }

    public class WarHamsConfiguration : IRocketPluginConfiguration
    {
        public ushort USA_KeyItemID;
        public ushort GER_KeyItemID;
        public int CaptureTimeSeconds;
        public int VP_GenerationIntervalSeconds;
        public int VP_PerZone;
        public int ScoreToWin;
        public List<ZoneData> Zones;

        public void LoadDefaults()
        {
            USA_KeyItemID = 1001; // ID предмета для USA
            GER_KeyItemID = 1002; // ID предмета для GER
            CaptureTimeSeconds = 60;
            VP_GenerationIntervalSeconds = 10;
            VP_PerZone = 5;
            ScoreToWin = 1000;
            Zones = new List<ZoneData>();
        }
    }
}
