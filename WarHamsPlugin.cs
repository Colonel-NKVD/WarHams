using Rocket.Core.Plugins;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WarHams
{
    public class WarHamsPlugin : RocketPlugin<WarHamsConfiguration>
    {
        public static WarHamsPlugin Instance { get; private set; }
        public bool MatchActive { get; private set; }
        public int ScoreUSA { get; private set; }
        public int ScoreGER { get; private set; }
        public System.DateTime MatchEndTime { get; private set; }

        private Dictionary<ulong, string> playerZones = new Dictionary<ulong, string>();
        private Dictionary<ulong, bool> playerWarned = new Dictionary<ulong, bool>();

        protected override void Load()
        {
            Instance = this;
            if (Configuration.Instance.Zones == null) Configuration.Instance.Zones = new List<ZoneData>();

            Provider.onEnemyDisconnected += OnPlayerDisconnected;
            PlayerLife.onPlayerDied += OnPlayerDied;
            StartCoroutine(ZoneUpdateLoop());
            Rocket.Core.Logging.Logger.Log("WarHams: Логика захвата оптимизирована.");
        }

        protected override void Unload()
        {
            Provider.onEnemyDisconnected -= OnPlayerDisconnected;
            PlayerLife.onPlayerDied -= OnPlayerDied;
            StopAllCoroutines();
            Instance = null;
        }

        private void OnPlayerDisconnected(SteamPlayer player) => ClearPlayerCache(player.playerID.steamID.m_SteamID);
        
        private void OnPlayerDied(PlayerLife sender, EDeathCause cause, ELimb limb, CSteamID instigator)
        {
            if (sender?.channel?.owner != null) ClearPlayerCache(sender.channel.owner.playerID.steamID.m_SteamID);
        }

        private void ClearPlayerCache(ulong steamId)
        {
            playerZones.Remove(steamId);
            playerWarned.Remove(steamId);
        }

        private IEnumerator ZoneUpdateLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(1f);
                if (Configuration.Instance.Zones.Count == 0) continue;

                var usaCounts = new Dictionary<string, int>();
                var gerCounts = new Dictionary<string, int>();
                foreach (var z in Configuration.Instance.Zones)
                {
                    usaCounts[z.Id] = 0;
                    gerCounts[z.Id] = 0;
                }

                foreach (var client in Provider.clients)
                {
                    var player = client.player;
                    if (player == null || player.life.isDead) continue;
                    
                    ulong steamId = client.playerID.steamID.m_SteamID;
                    ZoneData currentZone = null;

                    foreach (var zone in Configuration.Instance.Zones)
                    {
                        if ((player.transform.position - zone.Position).sqrMagnitude <= (zone.Radius * zone.Radius))
                        {
                            currentZone = zone;
                            break;
                        }
                    }

                    string currentZoneId = currentZone?.Id;
                    string previousZoneId = playerZones.ContainsKey(steamId) ? playerZones[steamId] : null;
                    var unturnedPlayer = UnturnedPlayer.FromSteamPlayer(client);

                    if (currentZoneId != previousZoneId)
                    {
                        if (currentZone != null) UnturnedChat.Say(unturnedPlayer, $"Вы вошли на точку: {currentZone.Name}", Color.yellow);
                        playerZones[steamId] = currentZoneId;
                        playerWarned[steamId] = false;
                    }

                    if (currentZone != null)
                    {
                        string faction = GetFaction(player);
                        if (faction == "Conflict")
                        {
                            if (!playerWarned.ContainsKey(steamId) || !playerWarned[steamId])
                            {
                                UnturnedChat.Say(unturnedPlayer, "ВНИМАНИЕ: У вас предметы обеих фракций! Выбросьте один.", Color.red);
                                playerWarned[steamId] = true;
                            }
                        }
                        else if (faction == "USA") usaCounts[currentZoneId]++;
                        else if (faction == "GER") gerCounts[currentZoneId]++;
                    }
                }

                foreach (var zone in Configuration.Instance.Zones)
                {
                    UpdateZoneProgress(zone, usaCounts[zone.Id], gerCounts[zone.Id]);
                }
            }
        }

        private void UpdateZoneProgress(ZoneData zone, int usa, int ger)
        {
            int capGoal = Configuration.Instance.CaptureTimeSeconds;

            // Логика Конфликта (Contested)
            if (usa > 0 && ger > 0)
            {
                zone.IsContested = true;
                zone.CurrentCapper = "Contested";
                return;
            }
            
            zone.IsContested = false;

            // Логика захвата USA
            if (usa > 0 && ger == 0)
            {
                if (zone.Owner == "USA" && zone.Progress == capGoal) return; // Точка наша и полностью укреплена

                if (zone.CurrentCapper != "USA")
                {
                    zone.CurrentCapper = "USA";
                    if (zone.Owner != "USA") UnturnedChat.Say($"Силы USA начали захват точки: {zone.Name}!", Color.cyan);
                    else UnturnedChat.Say($"Силы USA восстанавливают контроль над точкой: {zone.Name}!", Color.cyan);
                }

                if (zone.Progress < capGoal) zone.Progress++;

                if (zone.Progress >= capGoal && zone.Owner != "USA")
                {
                    zone.Owner = "USA";
                    zone.Progress = capGoal;
                    UnturnedChat.Say($"Точка {zone.Name} перешла под контроль USA!", Color.blue);
                }
            }
            // Логика захвата GER
            else if (ger > 0 && usa == 0)
            {
                if (zone.Owner == "GER" && zone.Progress == -capGoal) return;

                if (zone.CurrentCapper != "GER")
                {
                    zone.CurrentCapper = "GER";
                    if (zone.Owner != "GER") UnturnedChat.Say($"Силы GER начали захват точки: {zone.Name}!", Color.red);
                    else UnturnedChat.Say($"Силы GER восстанавливают контроль над точкой: {zone.Name}!", Color.red);
                }

                if (zone.Progress > -capGoal) zone.Progress--;

                if (zone.Progress <= -capGoal && zone.Owner != "GER")
                {
                    zone.Owner = "GER";
                    zone.Progress = -capGoal;
                    UnturnedChat.Say($"Точка {zone.Name} перешла под контроль GER!", Color.yellow);
                }
            }
            // Никого на точке
            else
            {
                zone.CurrentCapper = "None";
            }
        }

        public void StartMatch(int minutes)
        {
            ScoreUSA = 0;
            ScoreGER = 0;
            MatchActive = true;
            MatchEndTime = System.DateTime.Now.AddMinutes(minutes);
            StartCoroutine(VPGenerationLoop());
            UnturnedChat.Say($"Матч запущен ({minutes} мин). Цель: {Configuration.Instance.WinScore} VP.", Color.green);
        }

        public void StopMatch()
        {
            MatchActive = false;
            StopCoroutine(VPGenerationLoop());
            string winner = ScoreUSA > ScoreGER ? "USA" : (ScoreGER > ScoreUSA ? "GER" : "Ничья");
            UnturnedChat.Say($"Матч завершен! Победитель: {winner}. Итог: {ScoreUSA} - {ScoreGER}", Color.yellow);
        }

        private IEnumerator VPGenerationLoop()
        {
            while (MatchActive)
            {
                yield return new WaitForSeconds(Configuration.Instance.VP_IntervalSeconds);
                if (!MatchActive) break;

                foreach (var zone in Configuration.Instance.Zones)
                {
                    if (zone.Owner == "USA") ScoreUSA += Configuration.Instance.VP_PerZone;
                    else if (zone.Owner == "GER") ScoreGER += Configuration.Instance.VP_PerZone;
                }

                if (ScoreUSA >= Configuration.Instance.WinScore || ScoreGER >= Configuration.Instance.WinScore || System.DateTime.Now >= MatchEndTime)
                {
                    StopMatch();
                    break;
                }
            }
        }

        public string GetFaction(Player player)
        {
            List<InventorySearch> searchUSA = new List<InventorySearch>();
            player.inventory.search(searchUSA, Configuration.Instance.USA_KeyItemID, true, true);
            bool hasUsa = searchUSA.Count > 0;

            List<InventorySearch> searchGER = new List<InventorySearch>();
            player.inventory.search(searchGER, Configuration.Instance.GER_KeyItemID, true, true);
            bool hasGer = searchGER.Count > 0;

            if (hasUsa && hasGer) return "Conflict";
            if (hasUsa) return "USA";
            if (hasGer) return "GER";
            return "Neutral";
        }
    }
}
