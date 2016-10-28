﻿using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;

namespace Bitcraft.Tools.GitCommitHash
{
    public class Program
    {
        private static readonly string[] ClassComment = new string[]
        {
            "/// <summary>",
            "/// Stores the git commit hash of the current HEAD of your local repository.",
            "/// </summary>"
        };

        private static readonly string[] PropertyComment = new string[]
        {
            "/// <summary>",
            "/// Gets the git commit hash.",
            "/// </summary>"
        };

        private static readonly Regex namespaceNameRegex = new Regex(@"^[A-Za-z_][A-Za-z0-9_\.]*$");
        private static readonly Regex classNameRegex = new Regex(@"^[A-Za-z_][A-Za-z0-9_]*$");

        public enum AccessModifier
        {
            Public,
            Internal
        }

        public enum Indenting
        {
            Spaces,
            Tabs
        }

        public enum LineEnding
        {
            CrLf,
            Lf
        }

        public enum HashType
        {
            Short,
            Long
        }

        public class Options
        {
            public string OutputFilename = "GitCommitHash.cs";
            public string NamespaceName = null;
            public string ClassName = "GitCommitHash";
            public AccessModifier AccessModifier = AccessModifier.Public;
            public Indenting Indenting = Indenting.Spaces;
            public int IndentationSize = 4;
            public LineEnding LineEnding = LineEnding.Lf;
            public HashType HashType = HashType.Short;
        }

        public static int Main(string[] args)
        {
            IConfigurationRoot config = new ConfigurationBuilder()
                .AddCommandLine(args)
                .Build();

            Options options = ProcessConfig(config);

            if (options == null)
                return -1;

            return new Program().Run(options);
        }

        private Options options;

        private int Run(Options options)
        {
            this.options = options;

            char hashFormat = options.HashType == HashType.Short ? 'h' : 'H';

            var p = new Process();
            p.StartInfo = new ProcessStartInfo("git", $"log -n 1 --pretty=format:\"%{hashFormat}\"");
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.UseShellExecute = false;
            p.Start();

            string commitHash = p.StandardOutput.ReadToEnd();

            if (p.ExitCode != 0)
            {
                Console.WriteLine($"git failed and returned exit code {p.ExitCode}");
                return p.ExitCode;
            }

            var sb = new StringBuilder();

            AddLine(sb, "using System;");
            sb.Append(GetLineEnding());

            if (options.NamespaceName != null)
            {
                AddLine(sb, $"namespace {options.NamespaceName}");
                AddLine(sb, "{");
                indentationLevel++;
            }

            foreach (string comment in ClassComment)
                AddLine(sb, comment);

            string accessModifier = options.AccessModifier == AccessModifier.Public ? "public" : "internal";

            AddLine(sb, $"{accessModifier} static class {options.ClassName}");
            AddLine(sb, "{");
            indentationLevel++;

            foreach (string comment in PropertyComment)
                AddLine(sb, comment);

            AddLine(sb, "public static string Value");
            AddLine(sb, "{");
            indentationLevel++;

            AddLine(sb, string.Concat("get { return \"", commitHash, "\"; }"));

            indentationLevel--;
            AddLine(sb, "}");

            indentationLevel--;
            AddLine(sb, "}");

            if (options.NamespaceName != null)
            {
                indentationLevel--;
                AddLine(sb, "}");
            }

            string outputFilename = options.OutputFilename;

            if (Path.IsPathRooted(outputFilename) == false)
            {
                string temp = Path.Combine(Directory.GetCurrentDirectory(), outputFilename);
                outputFilename = Path.GetFullPath(temp);
            }

            string outputDirectory = Path.GetDirectoryName(outputFilename);
            if (Directory.Exists(outputDirectory) == false)
                Directory.CreateDirectory(outputDirectory);

            try
            {
                File.WriteAllText(outputFilename, sb.ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return -3;
            }

            return 0;
        }

        private int indentationLevel = 0;

        private void AddLine(StringBuilder sb, string line)
        {
            sb.Append($"{GetIndentation()}{line}{GetLineEnding()}");
        }

        private string GetIndentation()
        {
            if (options.IndentationSize == 0)
                return string.Empty;

            return new string(
                options.Indenting == Indenting.Spaces ? ' ' : '\t',
                options.IndentationSize * indentationLevel);
        }

        private string GetLineEnding()
        {
            return options.LineEnding == LineEnding.CrLf ? "\r\n" : "\n";
        }

        private static Options ProcessConfig(IConfiguration config)
        {
            var options = new Options();

            foreach (IConfigurationSection option in config.GetChildren())
            {
                switch (option.Key)
                {
                    case "ns":
                    case "namespace":
                        if (namespaceNameRegex.IsMatch(option.Value))
                            options.NamespaceName = option.Value;
                        else
                        {
                            Console.WriteLine($"Invalid 'namespace/ns' argument. '{option.Value}' [valid values: valid C# namespace name]");
                            return null;
                        }
                        break;
                    case "class":
                        if (classNameRegex.IsMatch(option.Value))
                            options.ClassName = option.Value;
                        else
                        {
                            Console.WriteLine($"Invalid 'class' argument. '{option.Value}' [valid values: valid C# class name]");
                            return null;
                        }
                        break;
                    case "output":
                        options.OutputFilename = option.Value;
                        break;
                    case "hash":
                        if (string.Equals(option.Value, "short", StringComparison.OrdinalIgnoreCase))
                            options.HashType = HashType.Short;
                        else if (string.Equals(option.Value, "long", StringComparison.OrdinalIgnoreCase))
                            options.HashType = HashType.Long;
                        else
                        {
                            Console.WriteLine($"Invalid 'hash' argument. '{option.Value}' [valid values: 'short' or 'long']");
                            return null;
                        }
                        break;
                    case "access-modifier":
                        if (string.Equals(option.Value, "public", StringComparison.OrdinalIgnoreCase))
                            options.AccessModifier = AccessModifier.Public;
                        else if (string.Equals(option.Value, "internal", StringComparison.OrdinalIgnoreCase))
                            options.AccessModifier = AccessModifier.Internal;
                        else
                        {
                            Console.WriteLine($"Invalid 'access-modifier' argument. '{option.Value}' [valid values: 'public' or 'internal']");
                            return null;
                        }
                        break;
                    case "indent":
                    case "indenting":
                        if (string.Equals(option.Value, "space", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(option.Value, "spaces", StringComparison.OrdinalIgnoreCase))
                            options.Indenting = Indenting.Spaces;
                        else if (string.Equals(option.Value, "tab", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(option.Value, "tabs", StringComparison.OrdinalIgnoreCase))
                            options.Indenting = Indenting.Tabs;
                        else
                        {
                            Console.WriteLine($"Invalid 'indent/indenting' argument. '{option.Value}' [valid values: 'space', 'spaces', 'tab' or 'tabs']");
                            return null;
                        }
                        break;
                    case "indent-size":
                    case "indenting-size":
                        int indentationSize;
                        if (int.TryParse(option.Value, out indentationSize) && indentationSize >= 0)
                            options.IndentationSize = indentationSize;
                        else
                        {
                            Console.WriteLine($"Invalid 'indent-size/indenting-size' argument. '{option.Value}' [valid values: integer greater than or equal to zero]");
                            return null;
                        }
                        break;
                    case "line-ending":
                        if (string.Equals(option.Value, "crlf", StringComparison.OrdinalIgnoreCase))
                            options.LineEnding = LineEnding.CrLf;
                        else if (string.Equals(option.Value, "lf", StringComparison.OrdinalIgnoreCase))
                            options.LineEnding = LineEnding.Lf;
                        else
                        {
                            Console.WriteLine($"Invalid 'line-ending' argument. '{option.Value}' [valid values: 'crlf' or 'lf']");
                            return null;
                        }
                        break;
                    default:
                        Console.WriteLine($"Unknown argument '{option.Key}'.");
                        return null;
                }
            }

            return options;
        }
    }
}
