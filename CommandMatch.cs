using Rocket.API;
using Rocket.Unturned.Chat;
using System.Collections.Generic;

namespace WarHams.Commands
{
    public class CommandMatch : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Both;
        public string Name => "match";
        public string Help => "Управление матчем";
        public string Syntax => "<start/stop>";
        public List<string> Aliases => new List<string>();
        public List<string> Permissions => new List<string> { "warhams.admin" };

        public void Execute(IRocketPlayer caller, string[] command)
        {
            if (command.Length < 1) return;

            string action = command[0].ToLower();
            if (action == "start")
            {
                WarHamsPlugin.Instance.IsMatchRunning = true;
                WarHamsPlugin.Instance.ScoreUSA = 0;
                WarHamsPlugin.Instance.ScoreGER = 0;
                UnturnedChat.Say("Матч начался! Очки победы генерируются.");
            }
            else if (action == "stop")
            {
                WarHamsPlugin.Instance.IsMatchRunning = false;
                UnturnedChat.Say("Матч принудительно остановлен администратором.");
            }
        }
    }
}
