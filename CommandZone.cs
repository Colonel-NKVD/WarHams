using Rocket.API;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WarHams.Commands
{
    public class CommandZone : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Player;
        public string Name => "zone";
        public string Help => "Управление зонами захвата";
        public string Syntax => "<create/remove/list/setowner>";
        public List<string> Aliases => new List<string>();
        
        // Меняем базовое право, чтобы команда открывалась для игроков
        public List<string> Permissions => new List<string> { "warhams.zone" };

        public void Execute(IRocketPlayer caller, string[] command)
        {
            var player = (UnturnedPlayer)caller;

            if (WarHamsPlugin.Instance.Configuration.Instance.Zones == null)
            {
                WarHamsPlugin.Instance.Configuration.Instance.Zones = new List<ZoneData>();
            }

            if (command.Length < 1)
            {
                UnturnedChat.Say(caller, "Используй: /zone <create/remove/list/setowner>", Color.red);
                return;
            }

            string subCommand = command[0].ToLower();

            // ВНУТРЕННЯЯ ПРОВЕРКА ПРАВ: Если это не list, требуем админку
            if (subCommand == "create" || subCommand == "remove" || subCommand == "setowner")
            {
                if (!caller.HasPermission("warhams.admin"))
                {
                    UnturnedChat.Say(caller, "Ошибка! У вас нет доступа к управлению зонами.", Color.red);
                    return;
                }
            }

            switch (subCommand)
            {
                case "create":
                    if (command.Length < 3) 
                    { 
                        UnturnedChat.Say(caller, "Ошибка! Используй: /zone create <id> <радиус>", Color.red); 
                        return; 
                    }
                    if (!float.TryParse(command[2], out float radius))
                    {
                        UnturnedChat.Say(caller, "Ошибка! Радиус должен быть числом.", Color.red);
                        return;
                    }

                    var newZone = new ZoneData
                    {
                        Id = command[1].ToLower(),
                        Name = command[1],
                        Position = player.Position,
                        Radius = radius,
                        Owner = "Neutral"
                    };
                    WarHamsPlugin.Instance.Configuration.Instance.Zones.Add(newZone);
                    WarHamsPlugin.Instance.Configuration.Save();
                    UnturnedChat.Say(caller, $"Зона {command[1]} успешно создана (Радиус: {radius})!", Color.green);
                    break;

                case "remove":
                    if (command.Length < 2) 
                    {
                        UnturnedChat.Say(caller, "Ошибка! Используй: /zone remove <id>", Color.red);
                        return;
                    }
                    int removed = WarHamsPlugin.Instance.Configuration.Instance.Zones.RemoveAll(z => z.Id == command[1].ToLower());
                    WarHamsPlugin.Instance.Configuration.Save();
                    if (removed > 0) UnturnedChat.Say(caller, $"Зона {command[1]} удалена.", Color.green);
                    else UnturnedChat.Say(caller, $"Зона {command[1]} не найдена.", Color.red);
                    break;

                case "list":
                    var zones = WarHamsPlugin.Instance.Configuration.Instance.Zones;
                    if (zones.Count == 0)
                    {
                        UnturnedChat.Say(caller, "Нет созданных зон.", Color.red);
                        return;
                    }

                    UnturnedChat.Say(caller, "--- Список зон ---", Color.yellow);
                    foreach (var zone in zones)
                    {
                        string status = zone.IsContested ? "[CONTESTED]" : $"Владелец: {zone.Owner} (Прогресс: {Mathf.Abs(zone.Progress)})";
                        UnturnedChat.Say(caller, $"- {zone.Name} (ID: {zone.Id}) | {status}", Color.white);
                    }
                    break;

                case "setowner":
                    if (command.Length < 3) 
                    {
                        UnturnedChat.Say(caller, "Ошибка! Используй: /zone setowner <id> <USA/GER/Neutral>", Color.red);
                        return;
                    }
                    
                    var targetZone = WarHamsPlugin.Instance.Configuration.Instance.Zones.FirstOrDefault(z => z.Id == command[1].ToLower());
                    if (targetZone != null)
                    {
                        string newOwner = command[2];
                        if (newOwner != "USA" && newOwner != "GER" && newOwner != "Neutral")
                        {
                            UnturnedChat.Say(caller, "Фракция должна быть USA, GER или Neutral", Color.red);
                            return;
                        }

                        targetZone.Owner = newOwner;
                        targetZone.Progress = (newOwner == "USA") ? WarHamsPlugin.Instance.Configuration.Instance.CaptureTimeSeconds : (newOwner == "GER" ? -WarHamsPlugin.Instance.Configuration.Instance.CaptureTimeSeconds : 0);
                        targetZone.CurrentCapper = "None";
                        WarHamsPlugin.Instance.Configuration.Save();
                        UnturnedChat.Say(caller, $"Владелец зоны {targetZone.Name} принудительно изменен на {newOwner}", Color.green);
                    }
                    else
                    {
                        UnturnedChat.Say(caller, $"Зона с ID '{command[1]}' не найдена.", Color.red);
                    }
                    break;
                    
                default:
                    UnturnedChat.Say(caller, "Неизвестная подкоманда.", Color.red);
                    break;
            }
        }
    }
}
