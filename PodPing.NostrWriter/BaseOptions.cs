using CommandLine;
using NBitcoin.DataEncoders;
using NBitcoin.Secp256k1;

namespace PodPing.NostrWriter;

public abstract class BaseOptions
{
    [Option("nostr-key", Required = true, HelpText = "Key to sign nostr events with (hex format)")]
    public string NostrKey { get; set; }

    [Option("nostr-relay", Required = true, HelpText = "Relay to publish nostr events to")]
    public string NostrRelay { get; set; }

    [Option('r', "reason", Required = false, HelpText = "Reason for iri publish")]
    public string Reason { get; set; }

    [Option('m', "medium", Required = false, HelpText = "What kind of medium is the iri")]
    public string Medium { get; set; }

    public ECPrivKey? PrivateKey
    {
        get
        {
            if (string.IsNullOrEmpty(NostrKey)) return null;

            var key = Encoders.Hex.DecodeData(NostrKey);
            return ECPrivKey.TryCreate(key, out var privKey) ? privKey : null;
        }
    }
}