﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Repl;
using Microsoft.Repl.Commanding;
using Microsoft.Repl.Parsing;
using Microsoft.Repl.Scripting;
using Microsoft.Repl.Suggestions;

namespace Microsoft.HttpRepl.Commands
{
    public class RunCommand : ICommand<HttpState, ICoreParseResult>
    {
        private static readonly string Name = "run";

        public bool? CanHandle(IShellState shellState, HttpState programState, ICoreParseResult parseResult)
        {
            return parseResult.Sections.Count > 1 && parseResult.Sections.Count < 4 && string.Equals(Name, parseResult.Sections[0], StringComparison.OrdinalIgnoreCase)
                ? (bool?)true
                : null;
        }

        public async Task ExecuteAsync(IShellState shellState, HttpState programState, ICoreParseResult parseResult, CancellationToken cancellationToken)
        {
            if (!File.Exists(parseResult.Sections[1]))
            {
                shellState.ConsoleManager.Error.WriteLine($"Could not file script file {parseResult.Sections[1]}");
                return;
            }

            bool suppressScriptLinesInHistory = true;
            if (parseResult.Sections.Count == 3)
            {
                suppressScriptLinesInHistory = !string.Equals(parseResult.Sections[2], "+history");
            }

            string[] lines = File.ReadAllLines(parseResult.Sections[1]);
            IScriptExecutor scriptExecutor = new ScriptExecutor<HttpState, ICoreParseResult>(suppressScriptLinesInHistory);
            await scriptExecutor.ExecuteScriptAsync(shellState, lines, cancellationToken).ConfigureAwait(false);
        }

        public string GetHelpDetails(IShellState shellState, HttpState programState, ICoreParseResult parseResult)
        {
            if (parseResult.Sections.Count > 0 && string.Equals(parseResult.Sections[0], Name, StringComparison.OrdinalIgnoreCase))
            {
                if (parseResult.Sections.Count == 1)
                {
                    return "Runs the specified script";
                }

                return "Runs the script " + parseResult.Sections[1];
            }

            return null;
        }

        public string GetHelpSummary(IShellState shellState, HttpState programState)
        {
            return "run {path to script} - Runs a script";
        }

        public IEnumerable<string> Suggest(IShellState shellState, HttpState programState, ICoreParseResult parseResult)
        {
            if (parseResult.SelectedSection == 0 &&
                (string.IsNullOrEmpty(parseResult.Sections[parseResult.SelectedSection]) || Name.StartsWith(parseResult.Sections[0].Substring(0, parseResult.CaretPositionWithinSelectedSection), StringComparison.OrdinalIgnoreCase)))
            {
                return new[] { Name };
            }

            if (parseResult.SelectedSection == 1 && string.Equals(parseResult.Sections[0], Name, StringComparison.OrdinalIgnoreCase))
            {
                return FileSystemCompletion.GetCompletions(parseResult.Sections[1].Substring(0, parseResult.CaretPositionWithinSelectedSection));
            }

            return null;
        }
    }
}
