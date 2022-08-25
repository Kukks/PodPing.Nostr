using System.Reflection;
using CommandLine;

namespace PodPing.Common;


public abstract  class BaseCLI
{
    public static string PodPingPrefix = "PODPING";
    public static int PodPingEvent = 30500;

    public async Task<int> ExecuteCli(string[] args)
    {
        args = AppendEnvironmentVariables(args);
        return await ExecuteCliCore(args);
    }

    public abstract Task<int> ExecuteCliCore(string[] args);

   
    private static string[] AppendEnvironmentVariables(string[] args)
    {
        if (args.Length == 0) return args;

        var verb = args[0];
        if (!TryGetOptions(verb, out var options)) return args;

        var newArgs = new List<string>(args);
        foreach (var unusedOption in FilterOptions(args, options))
        {
            var value = Environment.GetEnvironmentVariable(
                $"{PodPingPrefix}_{unusedOption.LongName.Replace("-", "_").ToUpperInvariant()}");
            if (value != null) newArgs.Add($"--{unusedOption.LongName}={value}");
        }

        return newArgs.ToArray();
    }

    private static bool TryGetOptions(string verb, out OptionAttribute[] options)
    {
        foreach (var type in Assembly.GetEntryAssembly().GetTypes().Where(type => !type.IsAbstract))
        {
            var verbAttribute = type.GetCustomAttributes<VerbAttribute>().FirstOrDefault();
            if (verbAttribute != null)
                if (verbAttribute.Name == verb)
                {
                    options = type.GetProperties()
                        .Select(property => property.GetCustomAttributes<OptionAttribute>().FirstOrDefault())
                        .Where(option => option != null).ToArray();
                    return true;
                }
        }

        options = null;
        return false;
    }

    private static OptionAttribute[] FilterOptions(string[] args, OptionAttribute[] options)
    {
        var usedLongNames = new HashSet<string>();
        var usedShortNames = new HashSet<string>();

        foreach (var arg in args)
            if (arg.StartsWith("--"))
            {
                var longName = arg.Substring(2);
                if (longName.Contains('=')) longName = longName.Substring(0, longName.IndexOf('='));

                usedLongNames.Add(longName);
            }
            else if (arg.StartsWith("-"))
            {
                var shortName = arg.Substring(1);
                if (shortName.Contains('=')) shortName = shortName.Substring(0, shortName.IndexOf('='));

                usedShortNames.Add(shortName);
            }

        return options.Where(option =>
            !usedLongNames.Contains(option.LongName) && !usedShortNames.Contains(option.ShortName)).ToArray();
    }
}