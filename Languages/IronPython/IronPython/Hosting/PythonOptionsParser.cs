/* ****************************************************************************
 *
 * Copyright (c) Microsoft Corporation. 
 *
 * This source code is subject to terms and conditions of the Microsoft Public License. A 
 * copy of the license can be found in the License.html file at the root of this distribution. If 
 * you cannot locate the  Microsoft Public License, please send an email to 
 * dlr@microsoft.com. By using this source code in any fashion, you are agreeing to be bound 
 * by the terms of the Microsoft Public License.
 *
 * You must not remove this notice, or any other, from this software.
 *
 *
 * ***************************************************************************/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Scripting;
using Microsoft.Scripting.Hosting;
using Microsoft.Scripting.Hosting.Shell;
using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Utils;

namespace IronPython.Hosting {

    public sealed class PythonOptionsParser : OptionsParser<PythonConsoleOptions> {
        private List<string> _warningFilters;

        public PythonOptionsParser() {
        }

        /// <exception cref="Exception">On error.</exception>
        protected override void ParseArgument(string/*!*/ arg) {
            ContractUtils.RequiresNotNull(arg, "arg");

            switch (arg) {
                case "-B": break; // dont_write_bytecode always true in IronPython
                case "-U": break; // unicode always true in IronPython
                case "-d": break; // debug output from parser, always False in IronPython

                case "-b": // Not shown in help on CPython
                    LanguageSetup.Options["BytesWarning"] = ScriptingRuntimeHelpers.True;
                    break;

                case "-c":
                    ConsoleOptions.Command = PeekNextArg();
                    string[] arguments = PopRemainingArgs();
                    arguments[0] = arg;
                    LanguageSetup.Options["Arguments"] = arguments;
                    break;

                case "-?":
                    ConsoleOptions.PrintUsage = true;
                    ConsoleOptions.Exit = true;
                    break;

                case "-i":
                    ConsoleOptions.Introspection = true;
                    LanguageSetup.Options["Inspect"] = ScriptingRuntimeHelpers.True;
                    break;

                case "-m":
                    ConsoleOptions.ModuleToRun = PeekNextArg();
                    LanguageSetup.Options["Arguments"] = PopRemainingArgs();
                    break;

                case "-x":
                    ConsoleOptions.SkipFirstSourceLine = true;
                    break;

                // TODO: unbuffered stdout?
                case "-u": break;

                // TODO: create a trace listener?
                case "-v":
                    LanguageSetup.Options["Verbose"] = ScriptingRuntimeHelpers.True;
                    break;

                case "-S":
                    ConsoleOptions.SkipImportSite = true;
                    LanguageSetup.Options["NoSite"] = ScriptingRuntimeHelpers.True;
                    break;

                case "-s":
                    LanguageSetup.Options["NoUserSite"] = ScriptingRuntimeHelpers.True;
                    break;

                case "-E":
                    ConsoleOptions.IgnoreEnvironmentVariables = true;
                    LanguageSetup.Options["IgnoreEnvironment"] = ScriptingRuntimeHelpers.True;
                    break;

                case "-t": LanguageSetup.Options["IndentationInconsistencySeverity"] = Severity.Warning; break;
                case "-tt": LanguageSetup.Options["IndentationInconsistencySeverity"] = Severity.Error; break;

                case "-O":
                    LanguageSetup.Options["Optimize"] = ScriptingRuntimeHelpers.True;
                    break;

                case "-OO":
                    LanguageSetup.Options["Optimize"] = ScriptingRuntimeHelpers.True;
                    LanguageSetup.Options["StripDocStrings"] = ScriptingRuntimeHelpers.True;
                    break;

                case "-Q":
                    LanguageSetup.Options["DivisionOptions"] = ToDivisionOptions(PopNextArg());
                    break;

                case "-Qold":
                case "-Qnew":
                case "-Qwarn":
                case "-Qwarnall":
                    LanguageSetup.Options["DivisionOptions"] = ToDivisionOptions(arg.Substring(2));
                    break;

                case "-V":
                    ConsoleOptions.PrintVersion = true;
                    ConsoleOptions.Exit = true;
                    IgnoreRemainingArgs();
                    break;

                case "-W":
                    if (_warningFilters == null) {
                        _warningFilters = new List<string>();
                    }

                    _warningFilters.Add(PopNextArg());
                    break;

                case "-3":
                    LanguageSetup.Options["WarnPy3k"] = ScriptingRuntimeHelpers.True;
                    break;

                case "-":
                    PushArgBack();
                    LanguageSetup.Options["Arguments"] = PopRemainingArgs();
                    break;

                case "-X:Frames":
                    LanguageSetup.Options["Frames"] = ScriptingRuntimeHelpers.True;
                    break;
                case "-X:FullFrames":
                    LanguageSetup.Options["Frames"] = LanguageSetup.Options["FullFrames"] = ScriptingRuntimeHelpers.True;
                    break;
                case "-X:Tracing":
                    LanguageSetup.Options["Tracing"] = ScriptingRuntimeHelpers.True;
                    break;
                case "-X:GCStress":
                    int gcStress;
                    if (!int.TryParse(PopNextArg(), out gcStress) || (gcStress < 0 || gcStress > GC.MaxGeneration)) {
                        throw new InvalidOptionException(String.Format("The argument for the {0} option must be between 0 and {1}.", arg, GC.MaxGeneration));
                    }

                    LanguageSetup.Options["GCStress"] = gcStress;
                    break;

                case "-X:MaxRecursion":
                    // we need about 6 frames for starting up, so 10 is a nice round number.
                    int limit;
                    if (!int.TryParse(PopNextArg(), out limit) || limit < 10) {
                        throw new InvalidOptionException(String.Format("The argument for the {0} option must be an integer >= 10.", arg));
                    }

                    LanguageSetup.Options["RecursionLimit"] = limit;
                    break;

                case "-X:EnableProfiler":
                    LanguageSetup.Options["EnableProfiler"] = ScriptingRuntimeHelpers.True;
                    break;

                case "-X:LightweightScopes":
                    LanguageSetup.Options["LightweightScopes"] = ScriptingRuntimeHelpers.True;
                    break;

                case "-X:MTA":
                    ConsoleOptions.IsMta = true;
                    break;
                case "-X:Python30":
                    LanguageSetup.Options["PythonVersion"] = new Version(3, 0);
                    break;

                case "-X:Debug":
                    RuntimeSetup.DebugMode = true;
                    LanguageSetup.Options["Debug"] = ScriptingRuntimeHelpers.True;
                    break;

                default:
                    base.ParseArgument(arg);

                    if (ConsoleOptions.FileName != null) {
                        PushArgBack();
                        LanguageSetup.Options["Arguments"] = PopRemainingArgs();
                    }
                    break;
            }
        }

        protected override void AfterParse() {
            if (_warningFilters != null) {
                LanguageSetup.Options["WarningFilters"] = _warningFilters.ToArray();
            }
        }

        private static PythonDivisionOptions ToDivisionOptions(string/*!*/ value) {
            switch (value) {
                case "old": return PythonDivisionOptions.Old;
                case "new": return PythonDivisionOptions.New;
                case "warn": return PythonDivisionOptions.Warn;
                case "warnall": return PythonDivisionOptions.WarnAll;
                default:
                    throw InvalidOptionValue("-Q", value);
            }
        }

        public override OptionsHelp GetHelp() {
            var standardResult = base.GetHelp();
#if !IRONPYTHON_WINDOW
            var commandLine = "Usage: ipy [options] [file.py|- [arguments]]";
#else
            var commandLine = "Usage: ipyw [options] [file.py|- [arguments]]";
#endif

            var pythonOptions = new[] {
#if !IRONPYTHON_WINDOW
                new KeyValuePair<string, string>("-v",                     "Verbose (trace import statements) (also PYTHONVERBOSE=x)"),
#endif
                new KeyValuePair<string, string>("-m module",              "run library module as a script"),
                new KeyValuePair<string, string>("-x",                     "Skip first line of the source"),
                new KeyValuePair<string, string>("-u",                     "Unbuffered stdout & stderr"),
                new KeyValuePair<string, string>("-O",                     "generate optimized code"),
                new KeyValuePair<string, string>("-OO",                    "remove doc strings and apply -O optimizations"),
                new KeyValuePair<string, string>("-E",                     "Ignore environment variables"),
                new KeyValuePair<string, string>("-Q arg",                 "Division options: -Qold (default), -Qwarn, -Qwarnall, -Qnew"),
                new KeyValuePair<string, string>("-S",                     "Don't imply 'import site' on initialization"),
                new KeyValuePair<string, string>("-s",                     "Don't add user site directory to sys.path"),
                new KeyValuePair<string, string>("-t",                     "Issue warnings about inconsistent tab usage"),
                new KeyValuePair<string, string>("-tt",                    "Issue errors for inconsistent tab usage"),
                new KeyValuePair<string, string>("-W arg",                 "Warning control (arg is action:message:category:module:lineno)"),
                new KeyValuePair<string, string>("-3",                     "Warn about Python 3.x incompatibilities"),

                new KeyValuePair<string, string>("-X:Frames",              "Enable basic sys._getframe support"),
                new KeyValuePair<string, string>("-X:FullFrames",          "Enable sys._getframe with access to locals"),
                new KeyValuePair<string, string>("-X:Tracing",             "Enable support for tracing all methods even before sys.settrace is called"),
                new KeyValuePair<string, string>("-X:GCStress",            "Specifies the GC stress level (the generation to collect each statement)"),
                new KeyValuePair<string, string>("-X:MaxRecursion",        "Set the maximum recursion level"),
                new KeyValuePair<string, string>("-X:Debug",               "Enable application debugging (preferred over -D)"),
                new KeyValuePair<string, string>("-X:MTA",                 "Run in multithreaded apartment"),
                new KeyValuePair<string, string>("-X:Python30",            "Enable available Python 3.0 features"),
                new KeyValuePair<string, string>("-X:EnableProfiler",      "Enables profiling support in the compiler"),
                new KeyValuePair<string, string>("-X:LightweightScopes",   "Generate optimized scopes that can be garbage collected"),
            };

            // Ensure the combined options come out sorted
			var options = new List<KeyValuePair<string, string>>();
			options.AddRange(pythonOptions);
			options.AddRange(standardResult.Options);
			options.Sort((x, y) => StringComparer.InvariantCulture.Compare(x.Key, y.Key));

            Debug.Assert(standardResult.EnvironmentVariables.Count == 0); // No need to append if the default is empty
            var environmentVariables = new[] {
                new KeyValuePair<string, string>("IRONPYTHONPATH",        "Path to search for module"),
                new KeyValuePair<string, string>("IRONPYTHONSTARTUP",     "Startup module")
            };

			return new OptionsHelp(commandLine, options, environmentVariables, standardResult.Comments);
        }
    }
}
