
using PodPing.NostrWriter;

Environment.SetEnvironmentVariable("PODPING_NOSTR_KEY", "8a703bed5450c305999bef7b10b74e592b9658a95201279969ef12f02f2477c4");
Environment.SetEnvironmentVariable("PODPING_NOSTR_RELAY", "wss://nostr-pub.wellorder.net");

args = new[] {"write", "--uri=https://gozo.com"};
return await new NostrWriterCli().ExecuteCli(args);