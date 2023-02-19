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

#### Configuration
Go in the Resources folder, copy and rename the file client.cfg.template in client.cfg. Here you can set parameters like authentication and server url.

## Run authentication in development

### Run keycloack locally using docker

- fast way:
`docker run -p 8080:8080 -e KEYCLOAK_ADMIN=admin -e KEYCLOAK_ADMIN_PASSWORD=admin quay.io/keycloak/keycloak:20.0.3 start-dev`

This will start Keycloak exposed on the local port 8080. It will also create an initial admin user with username admin and password admin.
You can find more images here: https://quay.io/repository/keycloak/keycloak

for more customization you can follow the official guide:
https://www.keycloak.org/server/containers

### Setup Keycloack
You can follow this guide https://medium.com/@robert.broeckelmann/openid-connect-authorization-code-flow-with-red-hat-sso-d141dde4ed3f until "Setup Red Hat SSO Client Configuration". This guide works for keycloack too.

#### create github auth app
- Go in your github account and click settings

- Go in Developer settings -> Oauth app then click on the button "new Oauth app"

- Here you need to set the "Authorization callback URL" with the redirect uri from keycloak (see section below)

-  "Homepage URL" is mandatory but you can use any words
#### Setup github
- Go to the "Identity provider" section and create a github provider, Then set ClientId and SecretId with your Github credentials.

- Copy the Redirect URI and paste it in your github "Oauth app" configuration

### Setup str-godot-client
By default, in debug mode, the authentication is disabled, to activate it you need to configure some params:

- Go in Resources/client.cfg and set these variables:

```
activate_auth_dev=true;
port=<port selected for your callback uri>;
host="<host selected for your callback uti";
client_id="<set the clientId from your keycloack "user" section>";
client_secret="<set the client secret from your keyckloack "user/credential" section>";
auth_api_url= "<set the url api "/auth" url from kyecloack "Realm settings/OpenID Endpoint Configuration">";
token_api_url= "<set the url api "/token" url from kyecloack "Realm settings/OpenID Endpoint Configuration">";
```
