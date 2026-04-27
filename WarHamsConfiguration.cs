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
        public string Owner; 
        
        [XmlIgnore] public float Progress; 
        [XmlIgnore] public bool IsContested;
        [XmlIgnore] public string CurrentCapper = "None"; // Отслеживание текущего захватчика
    }

    public class WarHamsConfiguration : IRocketPluginConfiguration
    {
        public ushort USA_KeyItemID;
        public ushort GER_KeyItemID;
        public int CaptureTimeSeconds;
        public int VP_IntervalSeconds;
        public int VP_PerZone;
        public int WinScore;
        
        public List<ZoneData> Zones;

        public void LoadDefaults()
        {
            USA_KeyItemID = 1001;
            GER_KeyItemID = 1002;
            CaptureTimeSeconds = 60;
            VP_IntervalSeconds = 30;
            VP_PerZone = 10;
            WinScore = 1000;
            Zones = new List<ZoneData>();
        }
    }
}
