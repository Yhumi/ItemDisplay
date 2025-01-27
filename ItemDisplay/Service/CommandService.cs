using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItemDisplay.Service
{
    internal static class CommandService
    {
        public struct ParsedTextCommand
        {
            public ParsedTextCommand() { }
            public string Main = string.Empty;
            public string Args = string.Empty;

            public override readonly string ToString()
            {
                return (Main + " " + Args).Trim();
            }
        }

        public static ParsedTextCommand FormatCommand(string command)
        {
            ParsedTextCommand textCommand = new();
            if (command != string.Empty)
            {
                command = command.Trim();
                if (command.StartsWith('/'))
                {
                    command = command.Replace('[', '<').Replace(']', '>');
                    var space = command.IndexOf(' ');
                    textCommand.Main = (space == -1 ? command : command[..space]).ToLower();
                    textCommand.Args = (space == -1 ? string.Empty : command[(space + 1)..]);
                }
                else
                    textCommand.Main = command;
            }
            return textCommand;
        }
    }
}
