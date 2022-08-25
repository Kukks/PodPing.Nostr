using System.Collections.Concurrent;
using System.Text.Json;
using NNostr.Client;
using PodPing.Common;

namespace PodPing.NostrListener;

public class NostrPodcastUpdateListener
{
    private readonly NostrRelayListener _nostrRelayListener;

    public readonly string[] ExplicitWhitelist;

    private readonly ConcurrentDictionary<string, string[]> whitelistContactResults = new();
    public readonly string[] WhitelistContacts;
    private readonly int _contactListKind;

    public NostrPodcastUpdateListener(NostrRelayListener nostrRelayListener, string[] explicitWhitelist,
        string[] whitelistContacts, int contactListKind = 3)
    {
        _nostrRelayListener = nostrRelayListener;
        ExplicitWhitelist = explicitWhitelist;
        WhitelistContacts = whitelistContacts;
        _contactListKind = contactListKind;
    }

    public EventHandler<NostrEvent[]> PodcastsUpdated;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _nostrRelayListener.EventsReceived += EventsReceived;
        _nostrRelayListener.NoticeReceived += (sender, s) => Console.WriteLine($"NOTICE: {s}");
        await _nostrRelayListener.StartAsync(cancellationToken);
        if (WhitelistContacts?.Any() is true)
        {
            var filter = new NostrSubscriptionFilter
            {
                Authors = WhitelistContacts,
                Kinds = new[] {_contactListKind}
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
            Kinds = new[] {BaseCLI.PodPingEvent}
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

        if (e.subscriptionId == "podcast-updates")
        {
            PodcastsUpdated.Invoke(this, e.events);
        }
    }
}