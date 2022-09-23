using Godot;
using System;
using System.Diagnostics;
using System.Collections.Generic;
using redhatgamedev.srt;

// This class is autoloaded
public class Game : Node
{
  private ServerConnection serverConnection;
  private LoginScreen loginScreen;

  private Stopwatch GameStopwatch = new Stopwatch();

  Boolean inGame = false;

  // UI elements
  CanvasLayer gameUI;
  TextureRect speedometer;
  TextureRect missileDisplay;
  TextureRect missileReadyIndicator;
  Texture missileReadyIndicatorReady;
  Texture missileReadyIndicatorNotReady;
  Texture missileReadyIndicatorDefault;

  CSLogger cslogger;

  // dictionary mapping for quicker access (might not need if GetNode<> is fast enough)
  Dictionary<String, PlayerShip> playerObjects = new Dictionary<string, PlayerShip>();
  Dictionary<String, SpaceMissile> missileObjects = new Dictionary<string, SpaceMissile>();
  PlayerShip myShip = null;

  PackedScene PackedMissile = (PackedScene)ResourceLoader.Load("res://Scenes/SupportScenes/SpaceMissile.tscn");
  PackedScene PlayerShipThing = (PackedScene)ResourceLoader.Load("res://Scenes/SupportScenes/Player.tscn");

  public String myUuid = null;

  // Called when the node enters the scene tree for the first time.
  public override void _Ready()
  {
    GameStopwatch.Start();
    cslogger = GetNode<CSLogger>("/root/CSLogger");
    cslogger.Info("Space Ring Things (SRT) Game Client v???");

    //canvasLayer = GetNode<CanvasLayer>("MapCanvasLayer");
    //if (canvasLayer != null) mapOverlay = canvasLayer.GetNode<Control>("MapOverlay");
    //else cslogger.Error("WTF - map canvas layer");

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

  public void initializeGameUI() 
  {
    // we are now in the game
    inGame = true;

    // load the textures for the missile statuses
    missileReadyIndicatorDefault = ResourceLoader.Load<Texture>("res://Assets/UIElements/HUD/HUD_missile_status_circle_indicator.png");
    missileReadyIndicatorNotReady = ResourceLoader.Load<Texture>("res://Assets/UIElements/HUD/HUD_missile_status_circle_indicator_red.png");
    missileReadyIndicatorReady = ResourceLoader.Load<Texture>("res://Assets/UIElements/HUD/HUD_missile_status_circle_indicator_green.png");

    // TODO: should we be adding the GUI to the scene instead of displaying its elements?
    // find the HUD to show its elements
    gameUI = GetNode<CanvasLayer>("GUI");
    speedometer = gameUI.GetNode<TextureRect>("Speedometer");
    speedometer.Show();

    missileDisplay = gameUI.GetNode<TextureRect>("Missile");
    missileDisplay.Show();

    missileReadyIndicator = gameUI.GetNode<TextureRect>("Missile/MissileReadyIndicator");
  }

  void updateGameUI()
  {
    // update the speedometer for our player
    speedometer.GetNode<Label>("SpeedLabel").Text = myShip.CurrentVelocity.ToString("n2");

    // if we have a missile, set the indicator to red, otherwise set the indicator to green
    if (myShip.MyMissile != null) missileReadyIndicator.Texture = missileReadyIndicatorNotReady;
    else missileReadyIndicator.Texture = missileReadyIndicatorReady;
  }

  public override void _Process(float delta)
  {

    var velocity = Vector2.Zero; // The player's movement direction.
    var shoot = Vector2.Zero; // the player's shoot status

    // check for inputs once we are ingame
    // TODO: this seems like a horrible and inefficient way to do this. We should probably
    // start the game in the login screen scene and then completely switch to the game scene
    // instead of doing this, no?
    if (inGame)
    {
      if (Input.IsActionPressed("rotate_right")) velocity.x += 1;
      if (Input.IsActionPressed("rotate_left")) velocity.x -= 1;
      if (Input.IsActionPressed("thrust_forward")) velocity.y += 1;
      if (Input.IsActionPressed("thrust_reverse")) velocity.y -= 1;
      if (Input.IsActionPressed("fire")) shoot.y = 1;
      if ((velocity.Length() > 0) || (shoot.Length() > 0)) ProcessInputEvent(velocity, shoot);

      if (myShip != null) updateGameUI();
    }
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

    return true; // TODO: this can't always be true

    // TODO: send my name and player preferences over to the severside
  }

  void QuitGame()
  {
    // our UUID was set, so we should send a leave message to be polite
    SecurityCommandBuffer scb = new SecurityCommandBuffer();
    scb.Uuid = myUuid;
    scb.Type = SecurityCommandBuffer.SecurityCommandBufferType.Leave;
    CommandBuffer cb = new CommandBuffer();
    cb.Type = CommandBuffer.CommandBufferType.Security;
    cb.securityCommandBuffer = scb;
    serverConnection.SendCommand(cb);
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
    Node2D playerShipThingInstance = (Node2D)PlayerShipThing.Instance();
    PlayerShip shipInstance = playerShipThingInstance.GetNode<PlayerShip>("PlayerShip"); // get the PlayerShip (a KinematicBody2D) child node
    shipInstance.uuid = uuid;

    cslogger.Debug("Adding ship to scene tree");
    AddChild(playerShipThingInstance);

    // TODO: this is inconsistent with the way the server uses the playerObjects array
    // where the server is using the ShipThing, this is using the PlayerShip. It may 
    // or may not be significant down the line
    playerObjects.Add(uuid, shipInstance);

    // check if our UUID is set -- would be set after join, but it's possible
    // we are receiving messages and creates, etc. before we have joined
    if (myUuid != null)
    {
      // the ship we just added is myship
      myShip = shipInstance;
      Node2D playerForCamera = playerObjects[myUuid];
      Camera2D playerCamera = playerForCamera.GetNode<Camera2D>("Camera2D");

      if (!playerCamera.Current) { playerCamera.MakeCurrent(); }
    }

    return shipInstance;
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
      cslogger.Debug("Game.cs: UpdateShipWithUUID: ship doesn't exist, creating " + uuid);
      shipInstance = this.CreateShipForUUID(uuid);
    }
    return shipInstance;
  }

  /// <summary>Called when a ship destroy message is received</summary>
  /// <param name="uuid"></param>
  /// <returns>nothing</returns>
  public void DestroyShipWithUUID(string uuid)
  {
    PlayerShip shipInstance;

    // if we don't find anything, do nothing, since there's nothing displayed yet to remove
    if (playerObjects.TryGetValue(uuid, out shipInstance))
    {
      playerObjects.Remove(uuid);
      // need to free the parent of the ship, which is the "shipthing"
      shipInstance.GetParent().QueueFree();
    }
  }

  /// <summary>
  /// Called when the network processes a game event with a create missile event
  ///
  /// </summary>
  /// <param name="uuid"></param>
  /// <returns>the created missile instance</returns>
  public SpaceMissile CreateMissileForUUID(EntityGameEventBuffer egeb)
  {
    // check if a key already exists for the uuid and return that missile if it does
    SpaceMissile existingMissile;
    if (missileObjects.TryGetValue(egeb.Uuid, out existingMissile)) 
    { 
      cslogger.Debug($"Game.cs: Existing missile found for UUID: {egeb.Uuid}");

      // check if it's our own missile that we're finally getting the create for
      if (existingMissile.uuid == myShip.MyMissile.uuid)
      { 
        cslogger.Debug($"Game.cs: Missile create was for our own existing missile");
      }
      return existingMissile; 
    }

    // TODO: refactor missile setup into a dedicated function to prevent all this duplicated code
    // set up the new missile
    SpaceMissile missileInstance = (SpaceMissile)PackedMissile.Instance();

    // set the missile's UUID to the message's UUID
    missileInstance.uuid = egeb.Uuid;

    // missiles have owners, so find the right player (hopefully)
    // TODO: this could conceivably blow up if we got missile information before we got player information
    missileInstance.MyPlayer = playerObjects[egeb.ownerUUID];
    
    // players own missiles, inversely
    missileInstance.MyPlayer.MyMissile = missileInstance;

    missileObjects.Add(egeb.Uuid, missileInstance);
    missileInstance.GlobalPosition = new Vector2(egeb.Body.Position.X, egeb.Body.Position.Y);
    missileInstance.RotationDegrees = egeb.Body.Angle;
    cslogger.Debug("Game.cs: Adding missile to scene tree");
    AddChild(missileInstance);

    // just in case we need to use it later
    missileInstance.AddToGroup("missiles");

    // Run the missile animation
    cslogger.Debug($"Game.cs: Starting missile animation for {missileInstance.uuid}");
    AnimatedSprite missileFiringAnimation = (AnimatedSprite)missileInstance.GetNode("Animations");
    missileFiringAnimation.Frame = 0;
    missileFiringAnimation.Play("launch");
    return missileInstance;
  }

  void CreateLocalMissileForUUID(string uuid)
  {
    cslogger.Debug($"Game.cs: CreateLocalMissileForUUID");
    // check if we already have a missile and return if we do
    if (myShip.MyMissile != null) 
    { 
      cslogger.Debug($"Game.cs: Missile already exists, aborting.");
      return; 
    }

    cslogger.Debug($"Game.cs: Creating local missile with uuid {uuid}");
    // set up the new missile
    PackedScene packedMissile = (PackedScene)ResourceLoader.Load("res://Scenes/SupportScenes/SpaceMissile.tscn");

    SpaceMissile missileInstance = (SpaceMissile)packedMissile.Instance();

    // set the missile's UUID to the message's UUID
    missileInstance.uuid = uuid;

    // missiles have owners, so find the right player (hopefully)
    // TODO: this could conceivably blow up if we got missile information before we got player information
    missileInstance.MyPlayer = myShip;
    
    // players own missiles, inversely
    missileInstance.MyPlayer.MyMissile = missileInstance;

    missileObjects.Add(uuid, missileInstance);

    // missile should point in the same direction as the ship
    missileInstance.Rotation = myShip.Rotation;

    // TODO: need to offset this to the front of the ship
    // start at our position
    missileInstance.Position = myShip.GlobalPosition;

    // negative direction is "up"
    Vector2 offset = new Vector2(0, -60);

    // rotate the offset to match the current ship heading
    offset = offset.Rotated(myShip.Rotation);
    missileInstance.Position = missileInstance.Position + offset;

    cslogger.Debug("Game.cs: Adding missile to scene tree");
    AddChild(missileInstance);

    // just in case we need to use it later
    missileInstance.AddToGroup("missiles");

    // Run the missile animation
    cslogger.Debug($"Game.cs: Starting missile animation for {missileInstance.uuid}");
    AnimatedSprite missileFiringAnimation = (AnimatedSprite)missileInstance.GetNode("Animations");
    missileFiringAnimation.Frame = 0;
    missileFiringAnimation.Play("launch");

    cslogger.Debug($"Game.cs: CreateLocalMissileForUUID");
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
      cslogger.Debug($"Game.cs: UpdateMissileWithUUID: missile doesn't exist, creating {egeb.Uuid} with owner {egeb.ownerUUID}");
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
    if ((velocity.Length() > 0) || (shoot.Length() > 0))
    {
      if (velocity.Length() > 0)
      {
        cslogger.Debug("Game.cs: Got move command");
        Box2d.PbVec2 b2dMove = new Box2d.PbVec2();
        b2dMove.X = velocity.x;
        b2dMove.Y = velocity.y;
        dsricb.pbv2Move = b2dMove;
      }

      if (shoot.Length() > 0)
      {
        cslogger.Debug("Game.cs: Got shoot command");
        // TODO: we should probably just return if our missile already exists
        // instead of going through this rigamarole

        // TODO: make this actually depend on ship direction
        Box2d.PbVec2 b2dShoot = new Box2d.PbVec2();
        b2dShoot.Y = 1;
        dsricb.pbv2Shoot = b2dShoot;

        // suggest a UUID for our new missile
        dsricb.missileUUID = System.Guid.NewGuid().ToString();

        // instantiate the missile locally
        CreateLocalMissileForUUID(dsricb.missileUUID);
      }

      ricb.dualStickRawInputCommandBuffer = dsricb;
      cb.rawInputCommandBuffer = ricb;
      serverConnection.SendCommand(cb);
    }

  }

  public override void _Notification(int what)
  {
    // when the game window is closed, you get this notification
    if (what == MainLoop.NotificationWmQuitRequest)
    {
      cslogger.Info("Game.cs: Got quit notification");
      // check if our UUID is set. If it isn't, we don't have to send a leave
      // message for our player, so we can just return
      if (myUuid == null) return;
      QuitGame();
    }
  }
}
