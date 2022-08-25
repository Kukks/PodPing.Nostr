using System.Text.Json;
using CommandLine;
using NBitcoin.Secp256k1;
using NetMQ;
using NetMQ.Sockets;
using NNostr.Client;
using PodPing.Common;

namespace PodPing.NostrWriter;

public class NostrWriterCli : BaseCLI
{
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
    }

    private async Task<NostrEvent> CreateEvent(string iri, string? podcastGuid, string reason, string medium,
        ECPrivKey key,
        string? feedHash)
    {
        var evt = new NostrEvent
        {
            Kind = PodPingEvent,
            Content = feedHash ?? "",
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
                }
            }
        };
        if (!string.IsNullOrEmpty(medium))
        {
            evt.Tags.Add(new()
            {
                TagIdentifier = "medium",
                Data = new List<string>
                {
                    medium
                }
            });
        }
        if (!string.IsNullOrEmpty(reason))
        {
            evt.Tags.Add(new()
            {
                TagIdentifier = "reason",
                Data = new List<string>
                {
                    reason
                }
            });
        }
        
        await evt.ComputeIdAndSign(key);
        return evt;
    }

    public override async Task<int> ExecuteCliCore(string[] args)
    {
        var result = 0;
        await Parser.Default.ParseArguments<WriteOptions, ServerOptions>(args)
            .WithParsedAsync(async o =>
            {
                result = o switch
                {
                    WriteOptions writeOptions => await Handle(writeOptions),
                    ServerOptions serverOptions => await Handle(serverOptions),
                    _ => 1
                };
            });
        return result;
    }
}