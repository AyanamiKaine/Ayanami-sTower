# Stella File Watcher

## API

## Example

1. You define the address and protocol in `config.json` it follows the same semantics as found in NNG
2. You can define an inital folder to watch files, you may change this later on

You can expect the FilerWatcher to publish its events to all subscribers that are listening for topics of `_pathToWatch` the current path watched is the topic used
