# Space Rings Thing Game Client (for Red Hat Gaming)
This repository is the Godot-based multiplayer client for the "Space Ring
Things" game. You will need to already have the [server
repository](https://github.com/redhat-gamedev/srt-godot-server) and the server's
prerequisites handled. The client also uses Protobufs for messaging, like the
server, and makes use of Git submodules in order to pull in the [Protobuf
descriptions](https://github.com/redhat-gamedev/srt-protobufs).

## How Can I Play This Game
Everything in currently in development. To play you'll need to roll up your
sleves and do a little bit of build/operations work. See the development notes
below for instructions on how to do that.

## Development Notes and Prerequisites
The game is currently being built with Linux and using Godot 3.5.1 with Mono.

This repo now uses submodules to point to the `Networking/protobufs`. Make sure to
`--recurse-submodules` when you clone. If you've already cloned prior to this
change, then run the following `git submodule update --init`.

Make sure that you are using the same commit/tag for both the client and server
in the `Networking/protobufs` folder.

### Running Your Own Everything / Testing Locally
You will need to run the game server, the AMQ messaging server, and this game
(client-side).

#### Server
Find/follow instructions for the [game server
here](https://github.com/redhat-gamedev/srt-godot-server). Once you have the
server working first, then you can run the game client with the Godot editor.

#### Client
This repo is the player game client.

If you are debugging the Godot server and Godot client on the same machine you
will want to do one of the following:

* Option 1. Goto `Editor->Editor Settings...` and search for the `Remote Port`
  setting (it's under `Network->Debug'). Change the port so that the client and
  server use different ports.
* Option 2. In the Server project, goto `Project->Export...` and export to your
  platform, then run the exported server.
