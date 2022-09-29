# Space Rings Thing Game Client (for Red Hat Gaming)
This is the player side source code for the SRT game. A multiplayer space game written in Godot.

### How Can I Play This Game
Everything in currently in development. To play you'll need to roll up your sleves and do a little bit of build/operations work. See the development notes below for instructions on how to do that.

## Architecture
TBD

## Development Notes
This repo now uses submodules to point to the Networking/protobufs. Make sure to `--recurse-submodules` when you clone. If you've already cloned prior to this change, then run the following `git submodule update --init`.

### Networking Code Gen (Protobuf)
The protocol buffer definitions are generated automatically via CI/CD in thier own git repo. That code is linked via git submodule by this repo. However, if you do need to make local client-side only changes, you can manually gen the C# code with the following instructions:

You will need the `dotnet` command line tool (or equivalent) in order to do this.

Intstall the Protogen tooling:
```
dotnet tool install --global protobuf-net.Protogen --version 3.0.101
```

Then, in the `proto` folder:
```
protogen --csharp_out=. *.proto
```

### Building the Client
TBD

### Running Your Own Everything / Testing Locally
You will need to run the game server, the AMQ messaging server, and this game (client-side).
We will eventually clean all this up to make it easier. For now there is a fair amount of manual work to be done. See below...

#### Broker ( [Apache Artemis](https://activemq.apache.org/components/artemis/download/) / [Red Hat AMQ](https://developers.redhat.com/products/amq/download) )
* Option 1. To run the messaging broker locally (in a container via podman/docker):
`podman run --name artemis -it -p 8161:8161 -p 5672:5672 --ip 10.88.0.2 -e AMQ_USER=admin -e AMQ_PASSWORD=admin -e AMQ_ALLOW_ANONYMOUS=true quay.io/artemiscloud/activemq-artemis-broker:latest`

In order to get a specific IP for a local container running Artemis, you will need to do this as the root system user.
Above command puts AMQ serving on IP address 10.88.0.2 with ports 8161 (the management console) and 5672 (the AMQP port).

* Option 2. You can run artemis natively with:
run `./artemis create gamebroker --user XXXXX --password XXXXX --role admin --name broker --allow-anonymous --force`

It will provide the command to run your newly configured broker which will look something like:
`apache-artemis-2.23.1/bin/gamebroker/bin/artemis-service start`

* Option 3. Running in a Kubernetes cluster
TBD

#### Server
Find/follow instructions for the [game server here](https://github.com/redhat-gamedev/srt-godot-server).

#### Client
This repo is the player game client.

If you are debugging the Godot server and Godot client on the same machine you will want to do one of the following:
* Option 1. Goto `Editor->Editor Settings...` and search for the `Remote Port` setting (it's under `Network->Debug'). Change the port so that the client and server use different ports.
* Option 2. In the Server project, goto `Project->Export...` and export to your platform, then run the exported server.
