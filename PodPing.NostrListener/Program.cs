

using PodPing.NostrListener;
Environment.SetEnvironmentVariable("PODPING_NOSTR_RELAY", "wss://nostr-pub.wellorder.net");
args = new[] {"listen"};
return await new NostrListenerCLI().ExecuteCli(args);