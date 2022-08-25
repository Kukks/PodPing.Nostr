# PodPing.Nostr

This repository is a reimplementation of [podping-hivewriter](https://github.com/Podcastindex-org/podping-hivewriter), except that it uses the [Nostr protocol](https://github.com/nostr-protocol/) instead of the hive blockchain.


This implementation is in C# but nostr is incredibly simple and can be ported trivially to other languages and stacks. 

There are 2 projects in this repository: a writer and listener.

## Writer

The writer allows you to send podcast updates and offers a standalone one-time use command or running a zeromq that can receive urls to broadcast on demand.

## Listener

The listener simply allows you to see a constant list of podcast updates since the application was started.

## Installation
* Clone git repo
* Install dotnet 6 from https://dotnet.microsoft.com/en-us/download
* To run the writer or listener, go into its directory, open a terminal and run `dotnet run`, which will give you a set of commands and its options.

## Specifics

We are utilizing a custom `kind` for nostr events: `30500`, and this fits under the NIP16 additional proposal: https://github.com/nostr-protocol/nips/pull/40

For `content`, an optional `hash` of the current podcast rss content can be provided. This helps clients easily pre-emptively realise if they're already on the latest version without needing to pull to check after noticing a push.
There are 4 `tags`:
* `podcast` tag, required, with the first value being the podcast guid, or the feed url if the podcast does not support podcast:guid yet
* `feed` tag, required, with the first value being the feed url (note that `podcast` field should always link to the original feed and this field would link to the current in case of a change)
* `reason` tag, optional, with the first value describing why this update occurred.
* `medium` tag, optional, with the first value describing what medium this podcast is.

