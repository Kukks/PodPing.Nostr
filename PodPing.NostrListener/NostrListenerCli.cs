using System.Text;
using CommandLine;
using NNostr.Client;
using PodPing.Common;

namespace PodPing.NostrListener;

public class NostrListenerCLI : BaseCLI
{
    public async Task<int> Handle(ListenOptions opts)
    {
        var relayListener = new NostrRelayListener(new Uri(opts.NostrRelay),
            new ConsoleLogger());
        var updateListener =
            new NostrPodcastUpdateListener(relayListener, opts.Whitelist.ToArray(), opts.ContactWhitelist.ToArray());
        updateListener.PodcastsUpdated += PodcastsUpdated;
        await updateListener.StartAsync(CancellationToken.None);
        while (true)
        {
            await Task.Delay(1000);
        }
    }

    private void PodcastsUpdated(object? sender, NostrEvent[] events)
    {
        foreach (var e in events)
        {
            var isPodcast = e.Tags.FirstOrDefault(tag => tag.TagIdentifier == "podcast");
            if (isPodcast is null)
            {
                continue;
            }
            var podcastId = isPodcast.Data.FirstOrDefault();
            var podcasturi = e.GetTaggedData("feed").First();
            var medium = e.GetTaggedData("medium").FirstOrDefault();
            var reason = e.GetTaggedData("reason").FirstOrDefault();
            var feedHash = e.Content;
            StringBuilder sb = new("podcast ");
            sb.Append(podcastId);
            if (podcasturi != podcastId)
            {
                sb.Append($" with feed {podcastId}");
            }

            if (!string.IsNullOrEmpty(feedHash))
            {
                sb.Append($" H:{feedHash}");
            }

            if (!string.IsNullOrEmpty(medium))
            {
                sb.Append($" M:{medium}");
            }

            if (!string.IsNullOrEmpty(reason))
            {
                sb.Append($" R:{reason}");
            }

            Console.WriteLine(sb);
        }
    }


    public override async Task<int> ExecuteCliCore(string[] args)
    {
        var result = 0;
        await Parser.Default.ParseArguments<ListenOptions>(args)
            .WithParsedAsync(async o => await Handle(o));
        return result;
    }
}