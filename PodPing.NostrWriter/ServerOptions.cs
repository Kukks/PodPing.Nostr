using CommandLine;

namespace PodPing.NostrWriter;

[Verb("server", HelpText = "Listen through zeromq for iris")]
public class ServerOptions : BaseOptions
{
    [Option("listen", Required = true, HelpText = "Where to listen on for zmq messages")]
    public Uri ListenUri { get; set; }
}