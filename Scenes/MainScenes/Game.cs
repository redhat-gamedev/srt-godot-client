using Godot;
using System;
using System.Collections.Generic;
using redhatgamedev.srt;

// This class is autoloaded
public class Game : Node
{
  private ServerConnection serverConnection;
  private LoginScreen loginScreen;
  private CanvasLayer canvasLayer;
  private Control mapOverlay;
  CSLogger cslogger;

  // dictionary mapping for quicker access (might not need if GetNode<> is fast enough)
  Dictionary<String, PlayerShip> playerObjects = new Dictionary<string, PlayerShip>();
  Dictionary<String, SpaceMissile> missileObjects = new Dictionary<string, SpaceMissile>();
  PlayerShip myShip;

  public String myUuid = null;

  // Called when the node enters the scene tree for the first time.
  public override void _Ready()
  {
    cslogger = GetNode<CSLogger>("/root/CSLogger");
    cslogger.Info("Space Ring Things (SRT) Game Client v???");

    canvasLayer = GetNode<CanvasLayer>("MapCanvasLayer");
    if (canvasLayer != null) mapOverlay = canvasLayer.GetNode<Control>("MapOverlay");
    else cslogger.Error("WTF - map canvas layer");

    serverConnection = new ServerConnection();
    this.AddChild(serverConnection);

    PackedScene packedLoginScene = (PackedScene)ResourceLoader.Load("res://Scenes/LoginScreen.tscn");
    loginScreen = (LoginScreen)packedLoginScene.Instance();
    loginScreen.Visible = false;
    this.AddChild(loginScreen);
    loginScreen.Visible = true;

    // TODO: check for server connection and do some retries if something is wrong
    // if lots of fails, pop up an error screen (and let player do server config?)
  }

  public override void _Process(float delta)
  {
    // make sure our player's camera is current
    if (myUuid != null)
    {
      if (playerObjects.ContainsKey(myUuid))
      {
        Node2D playerForCamera = playerObjects[myUuid];
        Camera2D playerCamera = playerForCamera.GetNode<Camera2D>("Camera2D");

        // it's possible the playerobject entry exists but the player hasn't
        // been added to the scene yet
        if (!(playerCamera == null))
        {
          // if it's not null, then we can make it current
          if (!playerCamera.Current) { playerCamera.MakeCurrent(); }

          // set its position to the player's position
          //playerCamera.Position = playerForCamera.Position;
        }
      }
    }

    var velocity = Vector2.Zero; // The player's movement direction.
    var shoot = Vector2.Zero; // the player's shoot status
    if (Input.IsActionPressed("rotate_right")) velocity.x += 1;
    if (Input.IsActionPressed("rotate_left")) velocity.x -= 1;
    if (Input.IsActionPressed("thrust_forward")) velocity.y += 1;
    if (Input.IsActionPressed("thrust_reverse")) velocity.y -= 1;
    if (Input.IsActionPressed("fire")) shoot.y = 1;
    if ((velocity.Length() > 0) || (shoot.Length() > 0)) ProcessInputEvent(velocity, shoot);

    // TODO ? maybe handle server telling us of new players
  }

  public bool JoinGameAsPlayer(string playerName)
  {
    // TODO: if not connected, try again to connect to server
    cslogger.Debug($"Game.cs: Sending join with UUID: {myUuid}, named: {playerName}");

    // construct a join message
    SecurityCommandBuffer scb = new SecurityCommandBuffer();

    //scb.Uuid = ServerConnection.UUID;
    scb.Uuid = playerName;

    scb.Type = SecurityCommandBuffer.SecurityCommandBufferType.Join;
    CommandBuffer cb = new CommandBuffer();
    cb.Type = CommandBuffer.CommandBufferType.Security;
    cb.securityCommandBuffer = scb;
    serverConnection.SendCommand(cb);

    Label nameLabel = mapOverlay.GetNode<Label>("PlayerNameLabel");
    nameLabel.Text = playerName;
    return true; // TODO: this can't always be true

    // TODO: send my name and player preferences over to the severside
  }

  /// <summary>
  /// Called when the network processes a game event with a create ship event
  ///
  /// </summary>
  /// <param name="uuid"></param>
  /// <returns>the created ship instance</returns>
  public PlayerShip CreateShipForUUID(string uuid)
  {
    cslogger.Debug("CreateShipForUUID: " + uuid);
    // TODO: check to ensure it doesn't already exist
    // TODO: check if it's my ship

    PackedScene playerShipThing = (PackedScene)ResourceLoader.Load("res://Scenes/SupportScenes/Player.tscn");
    Node2D playerShipThingInstance = (Node2D)playerShipThing.Instance();
    PlayerShip shipInstance = playerShipThingInstance.GetNode<PlayerShip>("PlayerShip"); // get the PlayerShip (a KinematicBody2D) child node
    shipInstance.uuid = uuid;

    cslogger.Debug("Adding ship to scene tree");
    AddChild(playerShipThingInstance);

    // TODO: this is inconsistent with the way the server uses the playerObjects array
    // where the server is using the ShipThing, this is using the PlayerShip. It may 
    // or may not be significant down the line
    playerObjects.Add(uuid, shipInstance);

    return shipInstance;
  }

  /// <summary>
  /// Called when the network processes a game event with a create missile event
  ///
  /// </summary>
  /// <param name="uuid"></param>
  /// <returns>the created missile instance</returns>
  public SpaceMissile CreateMissileForUUID(EntityGameEventBuffer egeb)
  {
    PackedScene packedMissile = (PackedScene)ResourceLoader.Load("res://Scenes/SupportScenes/SpaceMissile.tscn");
    SpaceMissile missileInstance = (SpaceMissile)packedMissile.Instance();

    // set the missile's UUID to the message's UUID
    missileInstance.uuid = egeb.Uuid;

    // missiles have owners, so find the right player (hopefully)
    missileInstance.MyPlayer = playerObjects[egeb.ownerUUID];

    missileObjects.Add(egeb.Uuid, missileInstance);
    missileInstance.GlobalPosition = new Vector2(egeb.Body.Position.X, egeb.Body.Position.Y);
    missileInstance.RotationDegrees = egeb.Body.Angle;
    cslogger.Debug("Adding missile to scene tree");
    AddChild(missileInstance);

    // Run the missile animation
    AnimatedSprite missileFiringAnimation = (AnimatedSprite)missileInstance.GetNode("Animations");
    missileFiringAnimation.Frame = 0;
    missileFiringAnimation.Play("launch");
    return missileInstance;
  }

  /// <summary>
  /// Called when the network processes a game event with a update ship event
  /// </summary>
  /// <param name="uuid"></param>
  /// <returns>the ship instance</returns>
  // TODO: wouldn't it be more appropriate to call this GetShipFromUUID ? since we're fetching the ship.
  public PlayerShip UpdateShipWithUUID(string uuid)
  {
    PlayerShip shipInstance;
    if (!playerObjects.TryGetValue(uuid, out shipInstance))
    {
      // must've joined before us - so we didn't get the create event, create it
      cslogger.Debug("UpdateShipWithUUID: ship doesn't exist, creating " + uuid);
      shipInstance = this.CreateShipForUUID(uuid);
    }
    return shipInstance;
  }

  /// <summary>
  /// Called when the network processes a game event with an update missile event
  /// </summary>
  /// <param name="uuid"></param>
  /// <returns>the missile instance</returns>
  public SpaceMissile UpdateMissileWithUUID(EntityGameEventBuffer egeb)
  {
    SpaceMissile missileInstance;
    if (!missileObjects.TryGetValue(egeb.Uuid, out missileInstance))
    {
      // the missile existed before we started, so we didn't get the create event
      cslogger.Debug($"UpdateMissileWithUUID: missile doesn't exist, creating {egeb.Uuid} with owner {egeb.ownerUUID}");
      missileInstance = this.CreateMissileForUUID(egeb);
    }

    return missileInstance;
  }

  /// <summary>Called when a missile destroy message is received</summary>
  /// <param name="uuid"></param>
  /// <returns>nothing</returns>
  public void DestroyMissileWithUUID(string uuid)
  {
    SpaceMissile missileInstance;

    // see if we know about the missile by checking the missileObjects array
    // if we don't, do nothing, since there's nothing displayed yet to remove
    if (missileObjects.TryGetValue(uuid, out missileInstance))
    {
      missileInstance.Expire();
      missileObjects.Remove(uuid);
    }
  }

  void ProcessInputEvent(Vector2 velocity, Vector2 shoot)
  {
    // there was some kind of input, so construct a message to send to the server
    CommandBuffer cb = new CommandBuffer();
    cb.Type = CommandBuffer.CommandBufferType.Rawinput;

    RawInputCommandBuffer ricb = new RawInputCommandBuffer();
    ricb.Type = RawInputCommandBuffer.RawInputCommandBufferType.Dualstick;
    ricb.Uuid = myUuid;

    DualStickRawInputCommandBuffer dsricb = new DualStickRawInputCommandBuffer();
    if ((velocity.Length() > 0) || (shoot.Length() > 0))  // what's up with this code? no brackets.

      if (velocity.Length() > 0)
      {
        Box2d.PbVec2 b2dMove = new Box2d.PbVec2();
        b2dMove.X = velocity.x;
        b2dMove.Y = velocity.y;
        dsricb.pbv2Move = b2dMove;
      }

    if (shoot.Length() > 0)
    {
      // TODO: make this actually depend on ship direction
      Box2d.PbVec2 b2dShoot = new Box2d.PbVec2();
      b2dShoot.Y = 1;
      dsricb.pbv2Shoot = b2dShoot;
    }

    ricb.dualStickRawInputCommandBuffer = dsricb;
    cb.rawInputCommandBuffer = ricb;
    serverConnection.SendCommand(cb);
  }

  //  // Called every frame. 'delta' is the elapsed time since the previous frame.
  //  public override void _Process(float delta)
  //  {
  //      
  //  }
}
