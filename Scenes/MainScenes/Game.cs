using Godot;
using System;
using System.Diagnostics;
using System.Collections.Generic;
using redhatgamedev.srt.v1;
using Serilog;

// This class is autoloaded
public class Game : Node
{
  Serilog.Core.LoggingLevelSwitch levelSwitch = new Serilog.Core.LoggingLevelSwitch();
  public Serilog.Core.Logger _serilogger;

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
  TextureRect gameRadar;
  Texture shipBlip;

  // radar update timer
  int radarRefreshTime = 1; // 1000ms = 1sec
  float radarRefreshTimer = 0;

  // dictionary mapping for quicker access (might not need if GetNode<> is fast enough)
  [Export]
  Dictionary<String, PlayerShip> playerObjects = new Dictionary<string, PlayerShip>();
  Dictionary<String, SpaceMissile> missileObjects = new Dictionary<string, SpaceMissile>();
  PlayerShip myShip = null;

  PackedScene PackedMissile = (PackedScene)ResourceLoader.Load("res://Scenes/SupportScenes/SpaceMissile.tscn");
  PackedScene PlayerShipThing = (PackedScene)ResourceLoader.Load("res://Scenes/SupportScenes/Player.tscn");

  public String myUuid = null;

  // Queues for processing incoming messages
  public Queue<GameEvent> PlayerCreateQueue = new Queue<GameEvent>();
  public Queue<GameEvent> PlayerUpdateQueue = new Queue<GameEvent>();
  public Queue<GameEvent> PlayerDestroyQueue = new Queue<GameEvent>();
  public Queue<GameEvent> MissileCreateQueue = new Queue<GameEvent>();
  public Queue<GameEvent> MissileUpdateQueue = new Queue<GameEvent>();
  public Queue<GameEvent> MissileDestroyQueue = new Queue<GameEvent>();

  /* PLAYER DEFAULTS AND CONFIG */

  float PlayerDefaultThrust = 1f;
  float PlayerDefaultMaxSpeed = 5;
  float PlayerDefaultRotationThrust = 1.5f;
  public int PlayerDefaultHitPoints = 100;
  int PlayerDefaultMissileSpeed = 300;
  float PlayerDefaultMissileLife = 2;
  int PlayerDefaultMissileDamage = 25;
  int PlayerDefaultMissileReloadTime = 2;

  /* END PLAYER DEFAULTS AND CONFIG */

  public void LoadConfig()
  {
    _serilogger.Information("Game.cs: Configuring");

    var clientConfig = new ConfigFile();

    // try to load user preference config first
    Error err = clientConfig.Load("user://client.cfg");
    if (err != Error.Ok)
    {
      _serilogger.Information("Game.cs: Local user config not found, defaulting to built-in");
      err = clientConfig.Load("Resources/client.cfg");
    }

    int DesiredLogLevel = 3;

    // if the file was loaded successfully, read the vars
    if (err == Error.Ok)
    {
      DesiredLogLevel = (int)clientConfig.GetValue("game", "log_level");
    }

    // pull values from env -- will get nulls if any vars are not set
    String envLogLevel = System.Environment.GetEnvironmentVariable("SRT_LOG_LEVEL");

    // override any loaded config with env
    if (envLogLevel != null) DesiredLogLevel = int.Parse(envLogLevel);

    switch (DesiredLogLevel)
    {
      case 0:
        _serilogger.Information("Game.cs: Setting minimum log level to: Fatal");
        levelSwitch.MinimumLevel = Serilog.Events.LogEventLevel.Fatal;
        break;
      case 1:
        _serilogger.Information("Game.cs: Setting minimum log level to: Error");
        levelSwitch.MinimumLevel = Serilog.Events.LogEventLevel.Error;
        break;
      case 2:
        _serilogger.Information("Game.cs: Setting minimum log level to: Warning");
        levelSwitch.MinimumLevel = Serilog.Events.LogEventLevel.Warning;
        break;
      case 3:
        _serilogger.Information("Game.cs: Setting minimum log level to: Information");
        levelSwitch.MinimumLevel = Serilog.Events.LogEventLevel.Information;
        break;
      case 4:
        _serilogger.Information("Game.cs: Setting minimum log level to: Debug");
        levelSwitch.MinimumLevel = Serilog.Events.LogEventLevel.Debug;
        break;
      case 5:
        _serilogger.Information("Game.cs: Setting minimum log level to: Verbose");
        levelSwitch.MinimumLevel = Serilog.Events.LogEventLevel.Verbose;
        break;
      default:
        _serilogger.Information("Game.cs: Unknown log level specified, defaulting to: Information");
        levelSwitch.MinimumLevel = Serilog.Events.LogEventLevel.Debug;
        break;
    }

  }

  // Called when the node enters the scene tree for the first time.
  public override void _Ready()
  {
    GameStopwatch.Start();
    levelSwitch.MinimumLevel = Serilog.Events.LogEventLevel.Information;

    _serilogger = new LoggerConfiguration().MinimumLevel.ControlledBy(levelSwitch).WriteTo.Console().CreateLogger();
    _serilogger.Information("Space Ring Things (SRT) Game Client v???");

    serverConnection = new ServerConnection();
    this.AddChild(serverConnection);

    LoadConfig();

    PackedScene packedLoginScene = (PackedScene)ResourceLoader.Load("res://Scenes/LoginScreen.tscn");
    loginScreen = (LoginScreen)packedLoginScene.Instance();
    loginScreen.Hide();
    this.AddChild(loginScreen);
    loginScreen.Show();

    // TODO: check for server connection and do some retries if something is wrong
    // if lots of fails, pop up an error screen (and let player do server config?)
  }

  public void displayGameOverScreen()
  {
    _serilogger.Verbose($"Game.cs: hide the GUI");
    CanvasLayer ourGui = GetNode<CanvasLayer>("GUI");
    ourGui.Hide();

    PackedScene packedGameOverScene = (PackedScene)ResourceLoader.Load("res://Scenes/GameOverScreen.tscn");
    GameOverScreen gameOver = (GameOverScreen)packedGameOverScene.Instance();
    AddChild(gameOver);
  }

  public void initializeGameUI()
  {
    // we are now in the game
    inGame = true;

    // load the textures for the missile statuses
    missileReadyIndicatorDefault = ResourceLoader.Load<Texture>("res://Assets/UIElements/HUD/HUD_missile_status_circle_indicator.png");
    missileReadyIndicatorNotReady = ResourceLoader.Load<Texture>("res://Assets/UIElements/HUD/HUD_missile_status_circle_indicator_red.png");
    missileReadyIndicatorReady = ResourceLoader.Load<Texture>("res://Assets/UIElements/HUD/HUD_missile_status_circle_indicator_green.png");

    // load the ship blip texture
    shipBlip = ResourceLoader.Load<Texture>("res://Assets/UIElements/HUD/ship_blip.png");

    // TODO: should we be adding the GUI to the scene instead of displaying its elements?
    // find the HUD to show its elements
    gameUI = GetNode<CanvasLayer>("GUI");
    gameUI.Show();

    speedometer = gameUI.GetNode<TextureRect>("Speedometer");
    speedometer.Show();

    missileDisplay = gameUI.GetNode<TextureRect>("Missile");
    missileDisplay.Show();

    missileReadyIndicator = gameUI.GetNode<TextureRect>("Missile/MissileReadyIndicator");

    gameRadar = gameUI.GetNode<TextureRect>("Radar");
    gameRadar.Show();
    gameUI.GetNode<TextureRect>("RadarReticle").Show();
  }

  // called to update camera and 2d listener for audio
  void updateWhoAmI()
  {
    // check if our UUID is set -- would be set after join, but it's possible
    // we are receiving messages and creates, etc. before we have joined
    if (myUuid != null && inGame == true)
    {
      // check if our uuid is in the playership dict
      if (playerObjects.TryGetValue(myUuid, out myShip))
      {
        Node2D playerForCamera = myShip;
        Camera2D playerCamera = playerForCamera.GetNode<Camera2D>("Camera2D");
        Listener2D theListener = playerForCamera.GetNode<Listener2D>("Listener2D");

        if (!playerCamera.Current) { playerCamera.MakeCurrent(); }
        if (!theListener.IsCurrent()) { theListener.MakeCurrent(); }
      }
    }
  }
  void updateGameUI()
  {
    // update the speedometer for our player
    speedometer.GetNode<Label>("SpeedLabel").Text = myShip.CurrentVelocity.ToString("n2");

    // if we have a missile, or we're not ready to reload, set the indicator to
    // red, otherwise set the indicator to green
    if ((myShip.MyMissile != null) || (!myShip.MissileReady))
    { missileReadyIndicator.Texture = missileReadyIndicatorNotReady; }
    else missileReadyIndicator.Texture = missileReadyIndicatorReady;
  }

  void updateGameRadar()
  {
    // the radar circle is approximately 280x280 and its center is 
    // approximately 169,215 on the image

    // delete all the radar blips
    // TODO: the performance on this is probably terrible
    deleteChildren(gameRadar);

    // iterate over the player objects
    foreach (KeyValuePair<String, PlayerShip> entry in playerObjects)
    {
      String player = entry.Key;
      PlayerShip playerShip = entry.Value;

      // don't draw ourselves
      if (player == myUuid) continue;
      _serilogger.Verbose($"Game.cs: Drawing radar dot for {player}");

      _serilogger.Verbose($"Game.cs: Player {player} is at position {playerShip.Position.x}:{playerShip.Position.y}");

      float deltaX = myShip.Position.x - playerShip.Position.x;
      float deltaY = myShip.Position.y - playerShip.Position.y;

      _serilogger.Verbose($"Game.cs: Relative position to player is {deltaX}:{deltaY}");

      // scale the relative position where 10,000 is the edge of the radar circle
      float scaledX = (deltaX / 10000) * (280 / 2);
      float scaledY = (deltaY / 10000) * (280 / 2);

      // x and y are "upside down" for some reason
      float finalX = (scaledX * -1) + 169;
      float finalY = (scaledY * -1) + 215;

      _serilogger.Verbose($"Game.cs: Scaled position to player is {scaledX}:{scaledY}");

      // add a blip at the scaled location offset from the center
      Sprite newBlip = new Sprite();
      newBlip.Texture = shipBlip;
      newBlip.Offset = new Vector2(finalX, finalY);

      gameRadar.AddChild(newBlip);
    }
  }

  void ProcessPlayerCreate()
  {
    while (PlayerCreateQueue.Count > 0)
    {
      GameEvent ge = PlayerCreateQueue.Dequeue();
      _serilogger.Debug($"Game.cs: Dequeuing player create message for {ge.Uuid}");
      PlayerShip newShip = CreateShipForUUID(ge.Uuid);
      newShip.UpdateFromGameEventBuffer(ge);
    }
  }

  void ProcessPlayerUpdate()
  {
    while (PlayerUpdateQueue.Count > 0)
    {
      GameEvent ge = PlayerUpdateQueue.Dequeue();
      _serilogger.Verbose($"Game.cs: Dequeuing player update message for {ge.Uuid}");
      PlayerShip ship = UpdateShipWithUUID(ge.Uuid);
      ship.UpdateFromGameEventBuffer(ge);
    }
  }

  void ProcessPlayerDestroy()
  {
    while (PlayerDestroyQueue.Count > 0)
    {
      GameEvent ge = PlayerDestroyQueue.Dequeue();
      _serilogger.Debug($"Game.cs: Dequeuing player destroy message for {ge.Uuid}");
      DestroyShipWithUUID(ge.Uuid, ge.HitPoints);
    }
  }

  void ProcessMissileCreate()
  {
    while (MissileCreateQueue.Count > 0)
    {
      GameEvent ge = MissileCreateQueue.Dequeue();
      _serilogger.Debug($"Game.cs: Dequeuing missile create message for {ge.Uuid} owner {ge.OwnerUuid}");
      SpaceMissile newMissile = CreateMissileForUUID(ge);
      newMissile.UpdateFromGameEventBuffer(ge);
    }
  }

  void ProcessMissileUpdate()
  {
    while (MissileUpdateQueue.Count > 0)
    {
      GameEvent ge = MissileUpdateQueue.Dequeue();
      _serilogger.Verbose($"Game.cs: Dequeuing missile update message for {ge.Uuid} owner {ge.OwnerUuid}");
      SpaceMissile missile = UpdateMissileWithUUID(ge);
      missile.UpdateFromGameEventBuffer(ge);
    }
  }

  void ProcessMissileDestroy()
  {
    while (MissileDestroyQueue.Count > 0)
    {
      GameEvent ge = MissileDestroyQueue.Dequeue();
      _serilogger.Debug($"Game.cs: Dequeuing missile destroy message for {ge.Uuid} owner {ge.OwnerUuid}");
      DestroyMissileWithUUID(ge.Uuid);
    }
  }

  public void ProcessAnnounce(Security security)
  {
    _serilogger.Debug($"Game.cs: Processing received announce message for {security.Uuid}");

    PlayerDefaultThrust = (float)security.ShipThrust;
    PlayerDefaultMaxSpeed = (float)security.MaxSpeed;
    PlayerDefaultRotationThrust = (float)security.RotationThrust;
    PlayerDefaultHitPoints = (int)security.HitPoints;
    PlayerDefaultMissileSpeed = (int)security.MissileSpeed;
    PlayerDefaultMissileLife = (float)security.MissileLife;
    PlayerDefaultMissileDamage = (int)security.MissileDamage;
    PlayerDefaultMissileReloadTime = (int)security.MissileReload;

    _serilogger.Debug($"Game.cs: Player Thrust:       {PlayerDefaultThrust}");
    _serilogger.Debug($"Game.cs: Player Speed:        {PlayerDefaultMaxSpeed}");
    _serilogger.Debug($"Game.cs: Player Rotation:     {PlayerDefaultRotationThrust}");
    _serilogger.Debug($"Game.cs: Player HP:           {PlayerDefaultHitPoints}");
    _serilogger.Debug($"Game.cs: Missile Speed:       {PlayerDefaultMissileSpeed}");
    _serilogger.Debug($"Game.cs: Missile Life:        {PlayerDefaultMissileLife}");
    _serilogger.Debug($"Game.cs: Missile Damage:      {PlayerDefaultMissileDamage}");
    _serilogger.Debug($"Game.cs: Missile Reload Time: {PlayerDefaultMissileReloadTime}");
  }

  public override void _Process(float delta)
  {
    updateWhoAmI();

    // we may eventually need to throw away some of these if the FPS starts slowing
    ProcessPlayerCreate();
    ProcessPlayerUpdate();
    ProcessMissileCreate();
    ProcessMissileUpdate();
    ProcessMissileDestroy();

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

      if (myShip != null)
      {
        updateGameUI();
      }

      // https://gdscript.com/solutions/godot-timing-tutorial/
      // check if we should update the debug UI, which itself should only be done if 
      // we are in a graphical mode
      // TODO: only if in graphical debug mode
      // TODO: should also probably use timer node
      radarRefreshTimer += delta;
      if (radarRefreshTimer >= radarRefreshTime)
      {
        radarRefreshTimer = 0;
        _serilogger.Verbose($"Game.cs: Updating radar");
        updateGameRadar();
      }
    }

    ProcessPlayerDestroy();
  }

  public bool JoinGameAsPlayer(string playerName)
  {
    // TODO: if not connected, try again to connect to server
    _serilogger.Debug($"Game.cs: Sending join with UUID: {myUuid}, named: {playerName}");

    // construct a join message
    Security scb = new Security();

    //scb.Uuid = ServerConnection.UUID;
    scb.Uuid = playerName;

    scb.security_type = Security.SecurityType.SecurityTypeJoin;
    serverConnection.SendSecurity(scb);

    return true; // TODO: this can't always be true

    // TODO: send my name and player preferences over to the severside
  }

  void QuitGame()
  {
    // our UUID was set, so we should send a leave message to be polite
    Security scb = new Security();
    scb.Uuid = myUuid;
    scb.security_type = Security.SecurityType.SecurityTypeLeave;
    serverConnection.SendSecurity(scb);
  }

  /// <summary>
  /// Called when the network processes a game event with a create ship event
  ///
  /// </summary>
  /// <param name="uuid"></param>
  /// <returns>the created ship instance</returns>
  public PlayerShip CreateShipForUUID(string uuid)
  {
    _serilogger.Debug("Game.cs: CreateShipForUUID: " + uuid);
    // TODO: check to ensure it doesn't already exist

    PlayerShip shipInstance;

    // TODO: we might need to do something in the case where we end up creating ships before the announce message
    //       has been processed. Right now the code will create a ship as soon as it receives an update for a ship
    //       it doesn't know about, but that could happen before we've received the announce. It's nice to 
    //       see ships moving around on the login screen. maybe we need to re-initialize all the known ships on 
    //       joining
    if (!playerObjects.TryGetValue(uuid, out shipInstance))
    {
      // we didn't find a matching ship in the playerObjects dictionary, so create a new instance
      Node2D playerShipThingInstance = (Node2D)PlayerShipThing.Instance();
      shipInstance = playerShipThingInstance.GetNode<PlayerShip>("PlayerShip"); // get the PlayerShip (a KinematicBody2D) child node
      shipInstance.uuid = uuid;

      // set the instance defaults to match what we learned from the announce
      _serilogger.Debug("Game.cs: Setting ship instance starting values to defaults");
      shipInstance.Thrust = PlayerDefaultThrust;
      shipInstance.MaxSpeed = PlayerDefaultMaxSpeed;
      shipInstance.RotationThrust = PlayerDefaultRotationThrust;
      shipInstance.HitPoints = PlayerDefaultHitPoints;
      shipInstance.MissileSpeed = PlayerDefaultMissileSpeed;
      shipInstance.MissileLife = PlayerDefaultMissileLife;
      shipInstance.MissileDamage = PlayerDefaultMissileDamage;

      _serilogger.Debug("Game.cs: Adding ship to scene tree");
      AddChild(playerShipThingInstance);

      // if the player is not us and we are ingame, play the warp in sound
      if (inGame == true && uuid != myUuid) shipInstance.GetNode<AudioStreamPlayer2D>("WarpInSound").Play();
    }
    else return shipInstance;

    // TODO: this is inconsistent with the way the server uses the playerObjects array
    // where the server is using the ShipThing, this is using the PlayerShip. It may 
    // or may not be significant down the line
    playerObjects.Add(uuid, shipInstance);

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
    _serilogger.Verbose("Game.cs: UpdateShipWithUUID");
    PlayerShip shipInstance;
    if (!playerObjects.TryGetValue(uuid, out shipInstance))
    {
      // must've joined before us - so we didn't get the create event, create it
      _serilogger.Debug("Game.cs: UpdateShipWithUUID: ship doesn't exist, creating " + uuid);
      shipInstance = this.CreateShipForUUID(uuid);
    }
    return shipInstance;
  }

  /// <summary>Called when a ship destroy message is received</summary>
  /// <param name="uuid"></param>
  /// <returns>nothing</returns>
  public void DestroyShipWithUUID(string uuid, int hitPoints)
  {
    PlayerShip shipInstance;

    // if we don't find anything, do nothing, since there's nothing displayed yet to remove
    if (playerObjects.TryGetValue(uuid, out shipInstance))
    {
      _serilogger.Debug($"Game.cs: checking hitpoints for {uuid}");
      if (hitPoints <= 0)
      {
        _serilogger.Debug($"Game.cs: hitpoints for {uuid} is <= 0, exploding");
        shipInstance.GetNode<AudioStreamPlayer2D>("ExplodeSound").Play();
      }
			else
			{
        _serilogger.Debug($"Game.cs: hitpoints for {uuid} is <= 0, warp out");
        shipInstance.GetNode<AudioStreamPlayer2D>("WarpOutSound").Play();
			}

      _serilogger.Debug($"Game.cs: Expiring player {uuid}");

      // if we are removing ourselves, we should set ingame to false
      if (uuid == myUuid)
      {
        inGame = false;
        _serilogger.Debug($"Game.cs: destroy is for our player, display game over screen");
        displayGameOverScreen();
      }

      // remove our ship from the known ship objects
      playerObjects.Remove(uuid);
    }
  }

  /// <summary>
  /// Called when the network processes a game event with a create missile event
  ///
  /// </summary>
  /// <param name="uuid"></param>
  /// <returns>the created missile instance</returns>
  public SpaceMissile CreateMissileForUUID(GameEvent egeb)
  {
    // check if a key already exists for the uuid and return that missile if it does
    SpaceMissile existingMissile;
    if (missileObjects.TryGetValue(egeb.Uuid, out existingMissile))
    {
      _serilogger.Debug($"Game.cs: Existing missile found for UUID: {egeb.Uuid}");

      // check if it's our own missile that we're finally getting the create for
      if (existingMissile.uuid == myShip.MyMissile.uuid)
      {
        _serilogger.Debug($"Game.cs: Missile create was for our own existing missile");
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
    missileInstance.MyPlayer = playerObjects[egeb.OwnerUuid];

    // players own missiles, inversely
    missileInstance.MyPlayer.MyMissile = missileInstance;

    missileObjects.Add(egeb.Uuid, missileInstance);
    missileInstance.GlobalPosition = new Vector2(egeb.PositionX, egeb.PositionY);
    missileInstance.RotationDegrees = egeb.Angle;
    _serilogger.Debug("Game.cs: Adding missile to scene tree");
    AddChild(missileInstance);
    //missileInstance.GetNode<AudioStreamPlayer>("FireSound").Play();

    // just in case we need to use it later
    missileInstance.AddToGroup("missiles");

    // Run the missile animation
    _serilogger.Debug($"Game.cs: Starting missile animation for {missileInstance.uuid}");
    AnimatedSprite missileFiringAnimation = (AnimatedSprite)missileInstance.GetNode("Animations");
    missileFiringAnimation.Frame = 0;
    missileFiringAnimation.Play("launch");
    return missileInstance;
  }

  void CreateLocalMissileForUUID(string uuid)
  {
    // check if we already have a missile and return if we do
    if (myShip.MyMissile != null)
    {
      _serilogger.Debug($"Game.cs: Missile already exists, aborting.");
      return;
    }

    // check if we are not ready and return if we are not
    if (!myShip.MissileReady)
    {
      _serilogger.Debug($"Game.cs: Missile not ready, aborting.");
      return;
    }

    _serilogger.Debug($"Game.cs: Setting missile to not ready");
    myShip.MissileReady = false;
    myShip.MissileReloadCountdown = myShip.MissileReloadTime;

    _serilogger.Debug($"Game.cs: Creating local missile with uuid {uuid}");
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

    // just in case we need to use it later
    missileInstance.AddToGroup("missiles");

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

    _serilogger.Debug("Game.cs: Adding missile to scene tree");
    AddChild(missileInstance);

    // Run the missile animation
    _serilogger.Debug($"Game.cs: Starting missile animation for {missileInstance.uuid}");
    AnimatedSprite missileFiringAnimation = (AnimatedSprite)missileInstance.GetNode("Animations");
    missileFiringAnimation.Frame = 0;
    missileFiringAnimation.Play("launch");
  }

  /// <summary>
  /// Called when the network processes a game event with an update missile event
  /// </summary>
  /// <param name="uuid"></param>
  /// <returns>the missile instance</returns>
  public SpaceMissile UpdateMissileWithUUID(GameEvent egeb)
  {
    SpaceMissile missileInstance;
    if (!missileObjects.TryGetValue(egeb.Uuid, out missileInstance))
    {
      // the missile existed before we started, so we didn't get the create event
      _serilogger.Debug($"Game.cs: UpdateMissileWithUUID: missile doesn't exist, creating {egeb.Uuid} with owner {egeb.OwnerUuid}");
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
    Command cb = new Command();
    cb.Uuid = myUuid;

    if ((velocity.Length() > 0) || (shoot.Length() > 0))
    {
      if (velocity.Length() > 0)
      {
        _serilogger.Verbose("Game.cs: Got move command");
        cb.command_type = Command.CommandType.CommandTypeMove;
        cb.InputX = (int)velocity.x;
        cb.InputY = (int)velocity.y;
        serverConnection.SendCommand(cb);
      }

      // only process a shoot command if we don't already have our own missile
      if ((shoot.Length() > 0) && (myShip.MyMissile == null) && (myShip.MissileReady))
      {
        _serilogger.Verbose("Game.cs: Got shoot command");
        cb.command_type = Command.CommandType.CommandTypeShoot;

        // suggest a UUID for our new missile
        cb.MissileUuid = System.Guid.NewGuid().ToString();

        // TODO: should we wrap this in some kind of try/catch?
        // send the shoot command
        serverConnection.SendCommand(cb);

        // instantiate the missile locally
        CreateLocalMissileForUUID(cb.MissileUuid);
      }
    }

  }

  public override void _Notification(int what)
  {
    // when the game window is closed, you get this notification
    if (what == MainLoop.NotificationWmQuitRequest)
    {
      _serilogger.Information("Game.cs: Got quit notification");
      // check if our UUID is set. If it isn't, we don't have to send a leave
      // message for our player, so we can just return
      if (myUuid == null) return;
      QuitGame();
    }
  }

  void deleteChildren(Node theNode)
  {
    foreach (Node n in theNode.GetChildren())
    {
      theNode.RemoveChild(n);
      n.QueueFree();
    }
  }

}
