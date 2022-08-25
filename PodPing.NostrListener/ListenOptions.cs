using CommandLine;

namespace PodPing.NostrListener;

[Verb("listen", HelpText = "Listen on a nostr relay for podcast updates")]
public class ListenOptions
{
    [Option("nostr-relay", Required = true, HelpText = "Relay to publish nostr events to")]
    public string NostrRelay { get; set; }

    [Option("whitelist", Required = false, HelpText = "set of keys to explicitly only listen to")]
    public IEnumerable<string> Whitelist { get; set; }

    [Option("contact-whitelist", Required = false,
        HelpText = "set of keys, whose contact list will be whitelisted to listen to")]
    public IEnumerable<string> ContactWhitelist { get; set; }
}