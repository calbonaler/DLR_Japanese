/* ****************************************************************************
 *
 * Copyright (c) Microsoft Corporation. 
 *
 * This source code is subject to terms and conditions of the Microsoft Public License. A 
 * copy of the license can be found in the License.html file at the root of this distribution. If 
 * you cannot locate the  Microsoft Public License, please send an email to 
 * ironruby@microsoft.com. By using this source code in any fashion, you are agreeing to be bound 
 * by the terms of the Microsoft Public License.
 *
 * You must not remove this notice, or any other, from this software.
 *
 *
 * ***************************************************************************/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security;
using IronRuby.Builtins;
using IronRuby.Runtime;
using Microsoft.Scripting;
using Microsoft.Scripting.Hosting;
using Microsoft.Scripting.Hosting.Shell;
using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Utils;

namespace IronRuby.Hosting {
    public sealed class RubyConsoleOptions : ConsoleOptions {
        public string ChangeDirectory;
        public bool DisplayVersion;
    }

    public sealed class RubyOptionsParser : OptionsParser<RubyConsoleOptions> {
        private readonly List<string>/*!*/ _loadPaths = new List<string>();
        private readonly List<string>/*!*/ _requiredPaths = new List<string>();

#if DEBUG && !SILVERLIGHT
        private ConsoleTraceListener _debugListener;

        private sealed class CustomTraceFilter : TraceFilter {
            public readonly Dictionary<string, bool>/*!*/ Categories = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

            public bool EnableAll { get; set; }

            public override bool ShouldTrace(TraceEventCache cache, string source, TraceEventType eventType, int id, string category, object[] args, object data1, object[] data) {
                string message = data1 as string;
                if (message == null) return true;

                bool enabled;
                if (Categories.TryGetValue(category, out enabled)) {
                    return enabled;
                } else {
                    return EnableAll;
                }
            }
        }

        private void SetTraceFilter(string/*!*/ arg, bool enable) {
            string[] categories = arg.Split(new[] { ';', ','}, StringSplitOptions.RemoveEmptyEntries);

            if (categories.Length == 0 && !enable) {
                Debug.Listeners.Clear();
                return;
            }

            if (_debugListener == null) {
                _debugListener = new ConsoleTraceListener { IndentSize = 4, Filter = new CustomTraceFilter { EnableAll = categories.Length == 0 } };
                Debug.Listeners.Add(_debugListener);
            } 
         
            foreach (var category in categories) {
                ((CustomTraceFilter)_debugListener.Filter).Categories[category] = enable;
            }
        }
#endif

        private static string[] GetPaths(string input) {
            string[] paths = input.Split(new char[] { Path.PathSeparator }, Int32.MaxValue, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < paths.Length; i++) {
                // Trim any occurrances of "
                string[] parts = paths[i].Split(new char[] { '"' }, Int32.MaxValue, StringSplitOptions.RemoveEmptyEntries);
                paths[i] = String.Concat(parts);
            }
            return paths;
        }

        /// <exception cref="Exception">On error.</exception>
        protected override void ParseArgument(string arg) {
            ContractUtils.RequiresNotNull(arg, "arg");

            string mainFileFromPath = null;

            if (arg.StartsWith("-e", StringComparison.Ordinal)) {
                string command;
                if (arg == "-e") {
                    command = PopNextArg();
                } else {
                    command = arg.Substring(2);
                }

                LanguageSetup.Options["MainFile"] = "-e";
                if (CommonConsoleOptions.Command == null) {
                    CommonConsoleOptions.Command = String.Empty;
                } else {
                    CommonConsoleOptions.Command += "\n";
                }
                CommonConsoleOptions.Command += command;
                return;
            }

            if (arg.StartsWith("-S", StringComparison.Ordinal)) {
                mainFileFromPath = arg == "-S" ? PopNextArg() : arg.Substring(2);
            }

            if (arg.StartsWith("-I", StringComparison.Ordinal)) {
                string includePaths;
                if (arg == "-I") {
                    includePaths = PopNextArg();
                } else {
                    includePaths = arg.Substring(2);
                }

                _loadPaths.AddRange(GetPaths(includePaths));
                return;
            }

            if (arg.StartsWith("-K", StringComparison.Ordinal)) {
                LanguageSetup.Options["KCode"] = arg.Length >= 3 ? RubyEncoding.GetKCodingByNameInitial(arg[2]) : null;
                return;
            }

            if (arg.StartsWith("-r", StringComparison.Ordinal)) {
                _requiredPaths.Add((arg == "-r") ? PopNextArg() : arg.Substring(2));
                return;
            }

            if (arg.StartsWith("-C", StringComparison.Ordinal)) {
                ConsoleOptions.ChangeDirectory = arg.Substring(2);
                return;
            }

            if (arg.StartsWith("-0", StringComparison.Ordinal) ||
                arg.StartsWith("-C", StringComparison.Ordinal) ||
                arg.StartsWith("-F", StringComparison.Ordinal) ||
                arg.StartsWith("-i", StringComparison.Ordinal) ||
                arg.StartsWith("-T", StringComparison.Ordinal) ||
                arg.StartsWith("-x", StringComparison.Ordinal)) {
                throw new InvalidOptionException(String.Format("Option `{0}' not supported", arg));
            }

            int colon = arg.IndexOf(':');
            string optionName, optionValue;
            if (colon >= 0) {
                optionName = arg.Substring(0, colon);
                optionValue = arg.Substring(colon + 1);
            } else {
                optionName = arg;
                optionValue = null;
            }

            switch (optionName) {
                #region Ruby options

                case "-a":
                case "-c":
                case "--copyright":
                case "-l":
                case "-n":
                case "-p":
                case "-s":
                    throw new InvalidOptionException(String.Format("Option `{0}' not supported", optionName));

                case "-d":
                    LanguageSetup.Options["DebugVariable"] = true; // $DEBUG = true
                    break;

                case "--version":
                    ConsoleOptions.PrintVersion = true;
                    ConsoleOptions.Exit = true;
                    break;

                case "-v":
                    ConsoleOptions.DisplayVersion = true;
                    goto case "-W2";

                case "-W0":
                    LanguageSetup.Options["Verbosity"] = 0; // $VERBOSE = nil
                    break;

                case "-W1":
                    LanguageSetup.Options["Verbosity"] = 1; // $VERBOSE = false
                    break;

                case "-w":
                case "-W2":
                    LanguageSetup.Options["Verbosity"] = 2; // $VERBOSE = true
                    break;

                #endregion

#if DEBUG && !SILVERLIGHT
                case "-DT*":
                    SetTraceFilter(String.Empty, false);
                    break;

                case "-DT":
                    SetTraceFilter(PopNextArg(), false);
                    break;

                case "-ET*":
                    SetTraceFilter(String.Empty, true);
                    break;

                case "-ET":
                    SetTraceFilter(PopNextArg(), true);
                    break;

                case "-ER":
                    RubyOptions.ShowRules = true;
                    break;

                case "-save":
                    LanguageSetup.Options["SavePath"] = optionValue ?? AppDomain.CurrentDomain.BaseDirectory;
                    break;

                case "-load":
                    LanguageSetup.Options["LoadFromDisk"] = ScriptingRuntimeHelpers.True;
                    break;

                case "-useThreadAbortForSyncRaise":
                    RubyOptions.UseThreadAbortForSyncRaise = true;
                    break;

                case "-compileRegexps":
                    RubyOptions.CompileRegexps = true;
                    break;
#endif
                case "-trace":
                    LanguageSetup.Options["EnableTracing"] = ScriptingRuntimeHelpers.True;
                    break;

                case "-profile":
                    LanguageSetup.Options["Profile"] = ScriptingRuntimeHelpers.True;
                    break;

                case "-1.8.6":
                    LanguageSetup.Options["Compatibility"] = RubyCompatibility.Ruby186;
                    break;

                case "-1.8.7":
                    LanguageSetup.Options["Compatibility"] = RubyCompatibility.Ruby187;
                    break;

                case "-1.9":
                    LanguageSetup.Options["Compatibility"] = RubyCompatibility.Ruby19;
                    break;

                case "-2.0":
                    LanguageSetup.Options["Compatibility"] = RubyCompatibility.Ruby20;
                    break;

                case "-X":
                    switch (optionValue) {
                        case "AutoIndent":
                        case "TabCompletion":
                        case "ColorfulConsole":
                            throw new InvalidOptionException(String.Format("Option `{0}' not supported", optionName));
                    }
                    goto default;
                    
               default:
                    base.ParseArgument(arg);

                    if (ConsoleOptions.FileName != null) {
                        if (mainFileFromPath != null) {
                            ConsoleOptions.FileName = FindMainFileFromPath(mainFileFromPath);
                        }

                        if (ConsoleOptions.Command == null) {
                            SetupOptionsForMainFile();
                        } else {
                            SetupOptionsForCommand();
                        }
                        
                        LanguageSetup.Options["ArgumentEncoding"] = 
#if SILVERLIGHT
                            RubyEncoding.UTF8;
#else
                            RubyEncoding.GetRubyEncoding(Console.InputEncoding);
#endif
                    } 
                    break;
            }
        }

        private void SetupOptionsForMainFile() {
            LanguageSetup.Options["MainFile"] = RubyUtils.CanonicalizePath(ConsoleOptions.FileName);
            LanguageSetup.Options["Arguments"] = PopRemainingArgs();;
        }

        private void SetupOptionsForCommand() {
            string firstArg = ConsoleOptions.FileName;
            ConsoleOptions.FileName = null;

            List<string> args = new List<string>(new string[] { firstArg });
            args.AddRange(PopRemainingArgs());

            LanguageSetup.Options["MainFile"] = "-e";
            LanguageSetup.Options["Arguments"] = args.ToArray();
        }

        private string FindMainFileFromPath(string mainFileFromPath) {
            string path = Platform.GetEnvironmentVariable("PATH");
            foreach (string p in path.Split(';')) {
                string fullPath = RubyUtils.CombinePaths(p, mainFileFromPath);
                if (Platform.FileExists(fullPath)) {
                    return fullPath;
                }
            }
            return mainFileFromPath;
        }

        protected override void AfterParse() {
            var existingSearchPaths =
                LanguageOptions.GetSearchPathsOption(LanguageSetup.Options) ??
                LanguageOptions.GetSearchPathsOption(RuntimeSetup.Options);

            if (existingSearchPaths != null) {
                _loadPaths.InsertRange(0, existingSearchPaths);
            }

#if !SILVERLIGHT
            try {
                string rubylib = Environment.GetEnvironmentVariable("RUBYLIB");
                if (rubylib != null) {
                    _loadPaths.AddRange(GetPaths(rubylib));
                }
            } catch (SecurityException) {
                // nop
            }
#endif
            LanguageSetup.Options["SearchPaths"] = _loadPaths;
            LanguageSetup.Options["RequiredPaths"] = _requiredPaths;

#if DEBUG && !SILVERLIGHT
            // Can be set to nl-BE, ja-JP, etc
            string culture = Environment.GetEnvironmentVariable("IR_CULTURE");
            if (culture != null) {
                System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo(culture, false);
            }
#endif
            if (ConsoleOptions.DisplayVersion && ConsoleOptions.Command == null && ConsoleOptions.FileName == null) {
                ConsoleOptions.PrintVersion = true;
                ConsoleOptions.Exit = true;
            }
        }

        public override OptionsHelp GetHelp() {
            var options = new[] {
             // new KeyValuePair<string, string>("-0[octal]",                   "specify record separator (\0, if no argument)"),
             // new KeyValuePair<string, string>("-a",                          "autosplit mode with -n or -p (splits $_ into $F)"),
             // new KeyValuePair<string, string>("-c",                          "check syntax only"),
                new KeyValuePair<string, string>("-Cdirectory",                 "cd to directory, before executing your script"),
                new KeyValuePair<string, string>("-d",                          "set debugging flags (set $DEBUG to true)"),
                new KeyValuePair<string, string>("-D",                          "emit debugging information (PDBs) for Visual Studio debugger"),
                new KeyValuePair<string, string>("-e 'command'",                "one line of script. Several -e's allowed. Omit [file]"),
             // new KeyValuePair<string, string>("-Fpattern",                   "split() pattern for autosplit (-a)"),
                new KeyValuePair<string, string>("-h[elp]",                     "Display usage"),
             // new KeyValuePair<string, string>("-i[extension]",               "edit ARGV files in place (make backup if extension supplied)"),
                new KeyValuePair<string, string>("-Idirectory",                 "specify $LOAD_PATH directory (may be used more than once)"),
#if !SILVERLIGHT
                new KeyValuePair<string, string>("-Kkcode",                     "specifies KANJI (Japanese) code-set"),
#endif
             // new KeyValuePair<string, string>("-l",                          "enable line ending processing"),
             // new KeyValuePair<string, string>("-n",                          "assume 'while gets(); ... end' loop around your script"),
             // new KeyValuePair<string, string>("-p",                          "assume loop like -n but print line also like sed"),
                new KeyValuePair<string, string>("-rlibrary",                   "require the library, before executing your script"),
             // new KeyValuePair<string, string>("-s",                          "enable some switch parsing for switches after script name"),
                new KeyValuePair<string, string>("-S",                          "look for the script using PATH environment variable"),
             // new KeyValuePair<string, string>("-T[level]",                   "turn on tainting checks"),
                new KeyValuePair<string, string>("-v",                          "print version number, then turn on verbose mode"),
                new KeyValuePair<string, string>("-w",                          "turn warnings on for your script"),
                new KeyValuePair<string, string>("-W[level]",                   "set warning level; 0=silence, 1=medium (default), 2=verbose"),
             // new KeyValuePair<string, string>("-x[directory]",               "strip off text before #!ruby line and perhaps cd to directory"),
             // new KeyValuePair<string, string>("--copyright",                 "print the copyright"),
                new KeyValuePair<string, string>("--version",                   "print the version"),

                new KeyValuePair<string, string>("-trace",                      "enable support for set_trace_func"),
                new KeyValuePair<string, string>("-profile",                    "enable support for 'pi = IronRuby::Clr.profile { block_to_profile }'"),
                new KeyValuePair<string, string>("-1.8.6",                      "Ruby 1.8.6 mode (default)"),
                new KeyValuePair<string, string>("-1.8.7",                      "Ruby 1.8.7 mode"),
                new KeyValuePair<string, string>("-1.9",                        "Ruby 1.9 mode"),
                new KeyValuePair<string, string>("-2.0",                        "Ruby 2.0 mode"),

                new KeyValuePair<string, string>("-X:ExceptionDetail",          "enable ExceptionDetail mode"),
                new KeyValuePair<string, string>("-X:NoAdaptiveCompilation",    "disable adaptive compilation - all code will be compiled"),
                new KeyValuePair<string, string>("-X:CompilationThreshold",     "the number of iterations before the interpreter starts compiling"),
                new KeyValuePair<string, string>("-X:PassExceptions",           "do not catch exceptions that are unhandled by script code"),
                new KeyValuePair<string, string>("-X:PrivateBinding",           "enable binding to private members"),
                new KeyValuePair<string, string>("-X:ShowClrExceptions",        "display CLS Exception information"),
                new KeyValuePair<string, string>("-X:RemoteRuntimeChannel",     "remote console channel"), 
             // new KeyValuePair<string, string>("-X:AutoIndent",               "Enable auto-indenting in the REPL loop"),
#if !SILVERLIGHT
             // new KeyValuePair<string, string>("-X:TabCompletion",            "Enable TabCompletion mode"),
             // new KeyValuePair<string, string>("-X:ColorfulConsole",          "Enable ColorfulConsole"),
#endif

#if DEBUG
                new KeyValuePair<string, string>("-DT",                         "disables tracing of specified events [debug only]"),
                new KeyValuePair<string, string>("-DT*",                        "disables tracing of all events [debug only]"),
                new KeyValuePair<string, string>("-ET",                         "enables tracing of specified events [debug only]"),
                new KeyValuePair<string, string>("-ET*",                        "enables tracing of all events [debug only]"),
                new KeyValuePair<string, string>("-save [path]",                "save generated code to given path [debug only]"),
                new KeyValuePair<string, string>("-load",                       "load pre-compiled code [debug only]"),
                new KeyValuePair<string, string>("-useThreadAbortForSyncRaise", "for testing purposes [debug only]"),
                new KeyValuePair<string, string>("-compileRegexps",             "faster throughput, slower startup [debug only]"),
                new KeyValuePair<string, string>("-X:AssembliesDir <dir>",      "set the directory for saving generated assemblies [debug only]"),
                new KeyValuePair<string, string>("-X:SaveAssemblies",           "save generated assemblies [debug only]"),
                new KeyValuePair<string, string>("-X:TrackPerformance",         "track performance sensitive areas [debug only]"),
                new KeyValuePair<string, string>("-X:PerfStats",                "print performance stats when the process exists [debug only]"),
#endif
            };
			return new OptionsHelp("[options] [file] [arguments]", options, null, null);
        }
    }
}
