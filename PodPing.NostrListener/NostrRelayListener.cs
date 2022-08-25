using System.Text.Json;
using Microsoft.Extensions.Logging;
using NNostr.Client;

namespace PodPing.NostrListener;

public class NostrRelayListener
{
    private readonly ILogger _logger;

    private readonly Uri _uri;
    private CancellationToken _ct;
    private NostrClient? _nostrClient;
    private RelayStatus _status = RelayStatus.Disconnected;
    public EventHandler<(string subscriptionId, NostrEvent[] events)> EventsReceived;
    public EventHandler<string> NoticeReceived;
    public EventHandler StatusChanged;

    public NostrRelayListener(Uri uri, ILogger logger)
    {
        _uri = uri;
        _logger = logger;
    }

    public RelayStatus Status
    {
        get => _status;
        set
        {
            _logger.LogInformation($" relay {_uri} status: {value} ");
            if (_status == value) return;
            _status = value;
            StatusChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public Dictionary<string, NostrSubscriptionFilter[]> Subscriptions { get; set; } = new();

    public void Dispose()
    {
        _nostrClient?.Dispose();
    }

    public async Task StartAsync(CancellationToken token)
    {
        _logger.LogInformation($"Starting to listen on relay {_uri}");
        _ct = token;

        _nostrClient = new NostrClient(_uri);
        _nostrClient.NoticeReceived += NoticeReceived;
        _nostrClient.EventsReceived += EventsReceived;
        // _nostrClient.MessageReceived += (sender, s) => { _logger.LogInformation($"Relay {_uri} sent message: {s}"); };
        _ = Task.Factory.StartNew(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                Status = RelayStatus.Connecting;
                await _nostrClient.ConnectAndWaitUntilConnected(token);
                Status = RelayStatus.Connected;
                foreach (var subscription in Subscriptions)
                {
                    _logger.LogInformation($" relay {_uri} subscribing {subscription.Key}\n{JsonSerializer.Serialize(subscription.Value)}");
                    
                    await _nostrClient.CreateSubscription(subscription.Key, subscription.Value, token);
                }

                await _nostrClient.ListenForMessages();
                Status = RelayStatus.Disconnected;
                await Task.Delay(2000, token);
            }
        }, token);
    }


    public async Task StopAsync(CancellationToken token)
    {
        if (_nostrClient is not null) await _nostrClient.Disconnect();
    }

    public async Task Unsubscribe(string directDm)
    {
        if (Subscriptions.Remove(directDm))
            try
            {
                await _nostrClient.CloseSubscription(directDm, _ct);
            }
            catch (Exception e)
            {
            }
    }

    public async Task Subscribe(string directDm, NostrSubscriptionFilter[] getMessageThreadFilters)
    {
        await Unsubscribe(directDm);
        if (Subscriptions.TryAdd(directDm, getMessageThreadFilters) && Status == RelayStatus.Connected)
            try
            {
                await _nostrClient.CreateSubscription(directDm, getMessageThreadFilters, _ct);
            }
            catch (Exception e)
            {
            }
    }

    public async Task SendEvent(NostrEvent evt)
    {
        _logger.LogInformation($"Sending evt to relay {_uri} : {JsonSerializer.Serialize(evt)} ");
        await _nostrClient.PublishEvent(evt, _ct);
    }
}