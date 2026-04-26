using Rocket.API;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using System.Collections.Generic;
using System.Linq;

namespace WarHams.Commands
{
    public class CommandZone : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Player;
        public string Name => "zone";
        public string Help => "Управление зонами захвата";
        public string Syntax => "<create/remove/setowner> <id> [radius/faction]";
        public List<string> Aliases => new List<string>();
        public List<string> Permissions => new List<string> { "warhams.admin" };

        public void Execute(IRocketPlayer caller, string[] command)
        {
            UnturnedPlayer player = (UnturnedPlayer)caller;
            if (command.Length < 2)
            {
                UnturnedChat.Say(caller, "Использование: /zone <create/remove/setowner> <id> [radius/faction]");
                return;
            }

            string action = command[0].ToLower();
            string zoneId = command[1].ToLower();
            var config = WarHamsPlugin.Instance.Configuration.Instance;

            if (action == "create" && command.Length == 3)
            {
                if (float.TryParse(command[2], out float radius))
                {
                    config.Zones.Add(new ZoneData
                    {
                        Id = zoneId,
                        Name = zoneId,
                        Position = player.Position,
                        Radius = radius,
                        Owner = "Neutral"
                    });
                    WarHamsPlugin.Instance.Configuration.Save();
                    UnturnedChat.Say(caller, $"Зона {zoneId} создана с радиусом {radius}.");
                }
            }
            else if (action == "remove")
            {
                var zone = config.Zones.FirstOrDefault(z => z.Id == zoneId);
                if (zone != null)
                {
                    config.Zones.Remove(zone);
                    WarHamsPlugin.Instance.Configuration.Save();
                    UnturnedChat.Say(caller, $"Зона {zoneId} удалена.");
                }
            }
            // Аналогично можно добавить логику для setowner
        }
    }
}
