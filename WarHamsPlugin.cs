using Rocket.Core.Plugins;
using Rocket.Unturned;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using SDG.Unturned;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WarHams
{
    public class WarHamsPlugin : RocketPlugin<WarHamsConfiguration>
    {
        public static WarHamsPlugin Instance { get; private set; }

        public bool IsMatchRunning = false;
        public int ScoreUSA = 0;
        public int ScoreGER = 0;
        public int MatchTimeRemaining = 0;

        // Кэш для прогресса захвата: Key - ZoneId, Value - прогресс (положительный - USA, отрицательный - GER)
        private Dictionary<string, int> ZoneCaptureProgress = new Dictionary<string, int>();

        protected override void Load()
        {
            Instance = this;
            
            foreach (var zone in Configuration.Instance.Zones)
            {
                ZoneCaptureProgress[zone.Id] = 0;
                // Инициализируем прогресс в зависимости от владельца
                if (zone.Owner == "USA") ZoneCaptureProgress[zone.Id] = Configuration.Instance.CaptureTimeSeconds;
                if (zone.Owner == "GER") ZoneCaptureProgress[zone.Id] = -Configuration.Instance.CaptureTimeSeconds;
            }

            StartCoroutine(CaptureRoutine());
            StartCoroutine(MatchRoutine());
            
            Rocket.Core.Logging.Logger.Log("WarHams Plugin loaded professionally.");
        }

        protected override void Unload()
        {
            StopAllCoroutines();
            Instance = null;
        }

        // --- Логика захвата точек (Тик каждую секунду) ---
        private IEnumerator CaptureRoutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(1f);

                if (!IsMatchRunning) continue;

                foreach (var zone in Configuration.Instance.Zones)
                {
                    int usaCount = 0;
                    int gerCount = 0;
                    float radiusSqr = zone.Radius * zone.Radius;

                    // Оптимизированный перебор игроков
                    foreach (var steamPlayer in Provider.clients)
                    {
                        if (steamPlayer.player == null || steamPlayer.player.life.isDead) continue;

                        float distSqr = (steamPlayer.player.transform.position - zone.Position).sqrMagnitude;
                        if (distSqr <= radiusSqr)
                        {
                            string faction = GetPlayerFaction(steamPlayer.player);
                            if (faction == "USA") usaCount++;
                            else if (faction == "GER") gerCount++;
                        }
                    }

                    ProcessCaptureLogic(zone, usaCount, gerCount);
                }
            }
        }

        private void ProcessCaptureLogic(ZoneData zone, int usaCount, int gerCount)
        {
            if (usaCount > 0 && gerCount > 0) return; // Contested
            if (usaCount == 0 && gerCount == 0) return; // Empty

            if (!ZoneCaptureProgress.ContainsKey(zone.Id)) ZoneCaptureProgress[zone.Id] = 0;

            int currentProgress = ZoneCaptureProgress[zone.Id];
            int capTime = Configuration.Instance.CaptureTimeSeconds;
            string previousOwner = zone.Owner;

            if (usaCount > 0)
            {
                if (currentProgress < capTime) currentProgress++;
            }
            else if (gerCount > 0)
            {
                if (currentProgress > -capTime) currentProgress--;
            }

            ZoneCaptureProgress[zone.Id] = currentProgress;

            // Проверка смены владельца
            if (currentProgress >= capTime && zone.Owner != "USA")
            {
                zone.Owner = "USA";
                UnturnedChat.Say($"Точка {zone.Name} захвачена силами USA!", Color.blue);
                Configuration.Save();
            }
            else if (currentProgress <= -capTime && zone.Owner != "GER")
            {
                zone.Owner = "GER";
                UnturnedChat.Say($"Точка {zone.Name} захвачена силами GER!", Color.red);
                Configuration.Save();
            }
            else if (currentProgress == 0 && zone.Owner != "Neutral")
            {
                zone.Owner = "Neutral";
                UnturnedChat.Say($"Точка {zone.Name} стала нейтральной!", Color.yellow);
                Configuration.Save();
            }
        }

        // --- Логика матча и очков (Тик генерации VP) ---
        private IEnumerator MatchRoutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(Configuration.Instance.VP_GenerationIntervalSeconds);

                if (!IsMatchRunning) continue;

                foreach (var zone in Configuration.Instance.Zones)
                {
                    if (zone.Owner == "USA") ScoreUSA += Configuration.Instance.VP_PerZone;
                    else if (zone.Owner == "GER") ScoreGER += Configuration.Instance.VP_PerZone;
                }

                CheckWinCondition();
            }
        }

        private void CheckWinCondition()
        {
            int winScore = Configuration.Instance.ScoreToWin;
            if (ScoreUSA >= winScore)
            {
                UnturnedChat.Say($"ПОБЕДА USA! Со счетом {ScoreUSA} против {ScoreGER}", Color.blue);
                IsMatchRunning = false;
            }
            else if (ScoreGER >= winScore)
            {
                UnturnedChat.Say($"ПОБЕДА GER! Со счетом {ScoreGER} против {ScoreUSA}", Color.red);
                IsMatchRunning = false;
            }
        }

        // --- Утилиты ---
        public string GetPlayerFaction(Player player)
        {
            if (HasItem(player, Configuration.Instance.USA_KeyItemID)) return "USA";
            if (HasItem(player, Configuration.Instance.GER_KeyItemID)) return "GER";
            return "Neutral";
        }

        private bool HasItem(Player player, ushort itemId)
        {
            var search = player.inventory.search(itemId, true, true);
            return search != null && search.Count > 0;
        }
    }
}
