using Rocket.API;
using Rocket.Unturned.Chat;
using System.Collections.Generic;

namespace WarHams.Commands
{
    public class CommandScore : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Both;
        public string Name => "score";
        public string Help => "Показывает текущий счет матча";
        public string Syntax => "";
        public List<string> Aliases => new List<string> { "vp" };
        public List<string> Permissions => new List<string> { "warhams.player" };

        public void Execute(IRocketPlayer caller, string[] command)
        {
            var plugin = WarHamsPlugin.Instance;
            UnturnedChat.Say(caller, $"Текущий счет - USA: {plugin.ScoreUSA} | GER: {plugin.ScoreGER}");
        }
    }
}
