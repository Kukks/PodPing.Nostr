using System.Reflection;
using CommandLine;
using NBitcoin.Secp256k1;
using NetMQ;
using NetMQ.Sockets;
using NNostr.Client;

namespace PodPing.NostrWriter;

public class NostrWriterCli
{
    public static string PodPingPrefix = "PODPING";
    public static int podpingEvent = 30500;

    public async Task<int> ExecuteCli(string[] args)
    {
        args = AppendEnvironmentVariables(args);
        var result = 0;
        await Parser.Default.ParseArguments<WriteOptions, ServerOptions>(args)
            .WithParsedAsync(async o =>
            {
                switch (o)
                {
                    case WriteOptions writeOptions:
                        result = await Handle(writeOptions);
                        break;
                    case ServerOptions serverOptions:
                        result = await Handle(serverOptions);
                        break;
                    default:
                        result = 1;
                        break;
                }
            });
        return result;
    }

    public async Task<int> Handle(WriteOptions opts)
    {
        var evt = await CreateEvent(opts.FeedUri, opts.PodcastGuid, opts.Reason, opts.Medium, opts.PrivateKey,
            opts.FeedHash);

        using var client = new NostrClient(new Uri(opts.NostrRelay));
        client.MessageReceived += (sender, s) => Console.WriteLine(s);
        await client.ConnectAndWaitUntilConnected();
        await client.PublishEvent(evt);
        return 0;
    }

    public async Task<int> Handle(ServerOptions opts)
    {
        using var client = new NostrClient(new Uri(opts.NostrRelay));
        await client.ConnectAndWaitUntilConnected();

        using var server = new ResponseSocket();
        server.Bind(opts.ListenUri.ToString());
        while (true)
        {
            var message = server.ReceiveFrameString().SplitArgs();

            var evt = await CreateEvent(message[0], message.Length > 1 ? message[1] : null, opts.Reason, opts.Medium,
                opts.PrivateKey, message.Length > 2 ? message[2] : null);
            await client.PublishEvent(evt);
        }

        return 0;
    }


    private async Task<NostrEvent> CreateEvent(string iri, string? podcastGuid, string reason, string medium,
        ECPrivKey key,
        string? feedHash)
    {
        var evt = new NostrEvent
        {
            Kind = podpingEvent,
            Content = feedHash??"",
            PublicKey = key.CreateXOnlyPubKey().ToBytes().ToHex(),
            CreatedAt = DateTimeOffset.UtcNow,
            Tags = new List<NostrEventTag>
            {
                new()
                {
                    TagIdentifier = "podcast",
                    Data = new List<string>
                    {
                        podcastGuid ?? iri
                    }
                },
                new()
                {
                    TagIdentifier = "feed",
                    Data = new List<string>
                    {
                        iri
                    }
                },
                new()
                {
                    TagIdentifier = "medium",
                    Data = new List<string>
                    {
                        medium
                    }
                },
                new()
                {
                    TagIdentifier = "reason",
                    Data = new List<string>
                    {
                        reason
                    }
                }
            }
        };
        await evt.ComputeIdAndSign(key);
        return evt;
    }

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
        foreach (var type in Assembly.GetExecutingAssembly().GetTypes().Where(type => !type.IsAbstract))
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