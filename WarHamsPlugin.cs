using Rocket.Core.Plugins;
using Rocket.Unturned.Chat;
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

        // Кэш для отслеживания состояния игроков (предотвращает спам в чат)
        private Dictionary<ulong, string> playerZones = new Dictionary<ulong, string>();
        private Dictionary<ulong, bool> playerWarned = new Dictionary<ulong, bool>();

        protected override void Load()
        {
            Instance = this;
            
            // Подписываемся на события для очистки кэша, чтобы не было утечек памяти
            Provider.onEnemyDisconnected += OnPlayerDisconnected;
            PlayerLife.onPlayerDied += OnPlayerDied;

            StartCoroutine(ZoneUpdateLoop());
            Rocket.Core.Logging.Logger.Log("WarHams: Система каптов запущена. Код оптимизирован.");
        }

        protected override void Unload()
        {
            Provider.onEnemyDisconnected -= OnPlayerDisconnected;
            PlayerLife.onPlayerDied -= OnPlayerDied;
            StopAllCoroutines();
            Instance = null;
        }

        private void OnPlayerDisconnected(SteamPlayer player)
        {
            ClearPlayerCache(player.playerID.steamID.m_SteamID);
        }

        private void OnPlayerDied(PlayerLife sender, EDeathCause cause, EKill ELimb, CSteamID instigator)
        {
            ClearPlayerCache(sender.channel.owner.playerID.steamID.m_SteamID);
        }

        private void ClearPlayerCache(ulong steamId)
        {
            playerZones.Remove(steamId);
            playerWarned.Remove(steamId);
        }

        // --- Основной цикл расчета зон ---
        private IEnumerator ZoneUpdateLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(1f);
                if (Configuration.Instance.Zones == null || Configuration.Instance.Zones.Count == 0) continue;

                // Подготовка счетчиков для текущего тика
                var usaCounts = new Dictionary<string, int>();
                var gerCounts = new Dictionary<string, int>();
                foreach (var z in Configuration.Instance.Zones)
                {
                    usaCounts[z.Id] = 0;
                    gerCounts[z.Id] = 0;
                }

                // Единый проход по всем игрокам (максимальная производительность)
                foreach (var client in Provider.clients)
                {
                    var player = client.player;
                    if (player == null || player.life.isDead) continue;
                    
                    ulong steamId = client.playerID.steamID.m_SteamID;
                    ZoneData currentZone = null;

                    // Поиск зоны, в которой находится игрок
                    foreach (var zone in Configuration.Instance.Zones)
                    {
                        if ((player.transform.position - zone.Position).sqrMagnitude <= (zone.Radius * zone.Radius))
                        {
                            currentZone = zone;
                            break; // Игрок может быть только в одной зоне одновременно
                        }
                    }

                    string currentZoneId = currentZone?.Id;
                    string previousZoneId = playerZones.ContainsKey(steamId) ? playerZones[steamId] : null;

                    // Логика личных уведомлений о входе на точку
                    if (currentZoneId != previousZoneId)
                    {
                        if (currentZone != null)
                        {
                            UnturnedChat.Say(client, $"Вы вошли на точку: {currentZone.Name}", Color.yellow);
                        }
                        playerZones[steamId] = currentZoneId;
                        playerWarned[steamId] = false; // Сбрасываем флаг предупреждения при смене зоны
                    }

                    // Обработка фракции и влияния на захват
                    if (currentZone != null)
                    {
                        string faction = GetFaction(player);

                        if (faction == "Conflict")
                        {
                            if (!playerWarned.ContainsKey(steamId) || !playerWarned[steamId])
                            {
                                UnturnedChat.Say(client, "ВНИМАНИЕ: У вас в инвентаре предметы обеих фракций! Выбросьте один, чтобы захватывать точку.", Color.red);
                                playerWarned[steamId] = true;
                            }
                        }
                        else if (faction == "USA") usaCounts[currentZoneId]++;
                        else if (faction == "GER") gerCounts[currentZoneId]++;
                    }
                }

                // Применение подсчитанных сил к зонам
                foreach (var zone in Configuration.Instance.Zones)
                {
                    UpdateZoneProgress(zone, usaCounts[zone.Id], gerCounts[zone.Id]);
                }
            }
        }

        private void UpdateZoneProgress(ZoneData zone, int usa, int ger)
        {
            if (usa > 0 && ger > 0)
            {
                if (!zone.IsContested)
                {
                    zone.IsContested = true;
                    // Опционально: можно раскомментировать, если нужно сообщение в глобал о конфликте на точке
                    // UnturnedChat.Say($"Точка {zone.Name} оспаривается (Contested)!");
                }
                return;
            }

            zone.IsContested = false;
            int capGoal = Configuration.Instance.CaptureTimeSeconds;

            if (usa > 0)
            {
                if (zone.Owner == "USA") return;
                if (zone.Progress == 0) UnturnedChat.Say($"Силы USA начали захват: {zone.Name}!");
                
                zone.Progress += 1;
                if (zone.Progress >= capGoal)
                {
                    zone.Owner = "USA";
                    zone.Progress = capGoal;
                    UnturnedChat.Say($"Точка {zone.Name} перешла под контроль USA!", Color.cyan);
                }
            }
            else if (ger > 0)
            {
                if (zone.Owner == "GER") return;
                if (zone.Progress == 0) UnturnedChat.Say($"Силы GER начали захват: {zone.Name}!");
                
                zone.Progress -= 1;
                if (zone.Progress <= -capGoal)
                {
                    zone.Owner = "GER";
                    zone.Progress = -capGoal;
                    UnturnedChat.Say($"Точка {zone.Name} перешла под контроль GER!", Color.red);
                }
            }
            // Если на точке никого нет, прогресс остается на том же уровне (не откатывается сам по себе).
            // Если нужен автооткат, сюда нужно добавить блок else.
        }

        // --- Управление матчем ---
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

                // Начисление очков происходит ТОЛЬКО ЗДЕСЬ, раз в VP_IntervalSeconds
                foreach (var zone in Configuration.Instance.Zones)
                {
                    if (zone.Owner == "USA") ScoreUSA += Configuration.Instance.VP_PerZone;
                    else if (zone.Owner == "GER") ScoreGER += Configuration.Instance.VP_PerZone;
                }

                if (ScoreUSA >= Configuration.Instance.WinScore || ScoreGER >= Configuration.Instance.WinScore)
                {
                    StopMatch();
                    break;
                }

                if (System.DateTime.Now >= MatchEndTime)
                {
                    StopMatch();
                    break;
                }
            }
        }

        // --- Утилиты ---
        public string GetFaction(Player player)
        {
            // Используем нативный инвентарь игрока для оптимизации (без обертки UnturnedPlayer)
            bool hasUsa = player.inventory.search(Configuration.Instance.USA_KeyItemID, true, true).Count > 0;
            bool hasGer = player.inventory.search(Configuration.Instance.GER_KeyItemID, true, true).Count > 0;

            if (hasUsa && hasGer) return "Conflict";
            if (hasUsa) return "USA";
            if (hasGer) return "GER";
            return "Neutral";
        }
    }
}
