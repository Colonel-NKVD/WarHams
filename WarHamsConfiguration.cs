using Rocket.API;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;

namespace WarHams
{
    public class ZoneData
    {
        public string Id;
        public string Name;
        public Vector3 Position;
        public float Radius;
        public string Owner; // USA, GER, Neutral
        
        [XmlIgnore] public float Progress; // Положительное - USA, отрицательное - GER
        [XmlIgnore] public bool IsContested;
    }

    public class WarHamsConfiguration : IRocketPluginConfiguration
    {
        public ushort USA_KeyItemID;
        public ushort GER_KeyItemID;
        public int CaptureTimeSeconds;
        public int VP_IntervalSeconds;
        public int VP_PerZone;
        public int WinScore;
        public string MessageColorUSA;
        public string MessageColorGER;
        
        public List<ZoneData> Zones;

        public void LoadDefaults()
        {
            USA_KeyItemID = 1001;
            GER_KeyItemID = 1002;
            CaptureTimeSeconds = 60;
            VP_IntervalSeconds = 30;
            VP_PerZone = 10;
            WinScore = 1000;
            MessageColorUSA = "cyan";
            MessageColorGER = "orange";
            Zones = new List<ZoneData>();
        }
    }
}
