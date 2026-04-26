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
        public string Syntax => "<start/stop> [минуты]";
        public List<string> Aliases => new List<string>();
        public List<string> Permissions => new List<string> { "warhams.admin" };

        public void Execute(IRocketPlayer caller, string[] command)
        {
            if (command.Length < 1)
            {
                UnturnedChat.Say(caller, "Использование: /match <start/stop> [минуты]");
                return;
            }

            string action = command[0].ToLower();
            if (action == "start")
            {
                int minutes = 60; // По умолчанию матч на час
                if (command.Length > 1) int.TryParse(command[1], out minutes);
                
                WarHamsPlugin.Instance.StartMatch(minutes);
            }
            else if (action == "stop")
            {
                WarHamsPlugin.Instance.StopMatch();
            }
        }
    }
}
