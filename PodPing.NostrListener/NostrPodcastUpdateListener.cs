using System.Collections.Concurrent;
using System.Text.Json;
using NNostr.Client;

namespace PodPing.NostrListener;

public class NostrPodcastUpdateListener
{
    private readonly NostrRelayListener _nostrRelayListener;

    public readonly string[] ExplicitWhitelist;

    private readonly ConcurrentDictionary<string, string[]> whitelistContactResults = new();
    public readonly string[] WhitelistContacts;

    public NostrPodcastUpdateListener(NostrRelayListener nostrRelayListener, string[] explicitWhitelist,
        string[] whitelistContacts)
    {
        _nostrRelayListener = nostrRelayListener;
        ExplicitWhitelist = explicitWhitelist;
        WhitelistContacts = whitelistContacts;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _nostrRelayListener.StartAsync(cancellationToken);
        _nostrRelayListener.EventsReceived += EventsReceived;
        if (WhitelistContacts?.Any() is true)
        {
            var filter = new NostrSubscriptionFilter
            {
                Authors = WhitelistContacts,
                Kinds = new[] {3}
            };

            await _nostrRelayListener.Subscribe("whitelist-contacts", new[] {filter});
        }

        await RestartUpdateFilter();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _nostrRelayListener.StopAsync(cancellationToken);
    }

    private async Task RestartUpdateFilter()
    {
        var list = whitelistContactResults.SelectMany(pair => pair.Value).Concat(ExplicitWhitelist).Distinct()
            .ToArray();
        var filter = new NostrSubscriptionFilter
        {
            Authors = list.Any() || WhitelistContacts.Any() ? list : null,
            ExtensionData = new Dictionary<string, JsonElement>
            {
                {"#podcast", JsonSerializer.SerializeToElement(Array.Empty<string>())}
            }
        };

        await _nostrRelayListener.Subscribe("podcast-updates", new[] {filter});
    }

    private void EventsReceived(object? sender, (string subscriptionId, NostrEvent[] events) e)
    {
        if (e.subscriptionId == "whitelist-contacts")
        {
            var newList = e.events.OrderByDescending(e => e.CreatedAt).DistinctBy(e => e.PublicKey)
                .ToDictionary(e => e.PublicKey, e => e.GetTaggedData("p"));
            foreach (var i in newList) whitelistContactResults.AddOrUpdate(i.Key, s => i.Value, (_, _) => i.Value);
        }
    }
}