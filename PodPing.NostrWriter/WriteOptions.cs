using CommandLine;

namespace PodPing.NostrWriter;

[Verb("write", HelpText = "One-off command to push iris")]
public class WriteOptions : BaseOptions
{
    [Option("uri", Required = true, HelpText = "url of the feed")]
    public string FeedUri { get; set; }

    [Option("hash", Required = false, HelpText = "current hash of the feed content")]
    public string FeedHash { get; set; }

    [Option("podcast", Required = false, HelpText = "podcast guid")]
    public string? PodcastGuid { get; set; }
}