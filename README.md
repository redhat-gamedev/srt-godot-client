# Space Rings Thing Game Client (for Red Hat Gaming)
This is the player side source code for the SRT game. A multiplayer space game written in Godot.

### How Can I Play This Game
Everything in currently in development. To play you'll need to roll up your sleves and do a little bit of build/operations work.

## Architecture
TBD

## Development Notes
### Networking Code Gen (Protobuf)
After making changes to the protocol buffer definitions, they need to be compiled to C# code.
You will need the `dotnet` command line tool (or equivalent) in order to do this.

Intstall the Protogen tooling:
```
dotnet tool install --global protobuf-net.Protogen --version 3.0.101
```

Then, in the `proto` folder:
```
protogen --csharp_out=. *.proto
```

### Building
TBD

### Running Your Own Everything / Testing Locally
You will need to run the game server, the AMQ messaging server, and this game (client-side).
We will eventually clean all this up to make it easier. For now there is a fair amount of manual work to be done. See below...

#### Broker
* Option 1. To run AMQ locally (in a container via podman/docker):
`podman run --name artemis -it -p 8161:8161 -p 5672:5672 --ip 10.88.0.2 -e AMQ_USER=admin -e AMQ_PASSWORD=admin -e AMQ_ALLOW_ANONYMOUS=true quay.io/artemiscloud/activemq-artemis-broker:latest`

In order to get a specific IP for a local container running Artemis, you will need to do this as the root system user.
Above command puts AMQ serving on IP address 10.88.0.2 with ports 8161 (the management console) and 5672 (the AMQP port).

* Option 2. You can run artemis natively with:
run `./artemis create gamebroker --user XXXXX --password XXXXX --role admin --name broker --allow-anonymous --force`

It will provide the command to run your newly configured broker

* Option 3. Running in a Kubernetes cluster
TBD - helm to launch

#### Server
Find/follow instructions for the [game server here](https://github.com/redhat-gamedev/srt-godot-server).

#### Client
This repo is the player game client.

If you are debugging the Godot server and Godot client on the same machine you will want to do one of the following:
* Option 1. Goto `Editor->Editor Settings...` and search for the `Remote Port` setting (it's under `Network->Debug'). Change the port so that the client and server use different ports.
* Option 2. In the Server project, goto `Project->Export...` and export to your platform, then run the exported server.