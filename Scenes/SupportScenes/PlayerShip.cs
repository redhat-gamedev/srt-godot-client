using Godot;
using System;
using System.Collections;
using redhatgamedev.srt.v1;

public partial class PlayerShip : CharacterBody2D
{
  public float Thrust = 1f; // effective acceleration

  public float MaxSpeed = 5;

  public float StopThreshold = 10f;

  public float GoThreshold = 90f;

  public float CurrentVelocity = 0;

  public float RotationThrust = 1.5f;

  public float CurrentRotation = 0;

  public int HitPoints = 100;

  public int MissileSpeed = 300;

  public float MissileLife = 4;

  public int MissileDamage = 25;

  // the reload time is the minimum time between missile firings
  // relevant when two players are very close to one another and
  // prevents missile spamming
  public float MissileReloadTime = 2;
  public float MissileReloadCountdown;
  public bool MissileReady = true;

  public String uuid;

  Game MyGame;

  public Serilog.Core.Logger _serilogger;

  //public Queue MovementQueue = new Queue();
  public SpaceMissile MyMissile = null; // for now only one missile at a time

  Node2D shipThing;

  Sprite2D hitPointRing;

  Vector2 targetPosition;
  float targetRotation;
  float targetVelocity;

  public enum PlayerRemoveType
  {
    Destroy,
    WarpOut
  }

  private bool markedForDestruction = false;
  private UInt32 destroyedSequenceNumber; // the sequence when this missile was marked for destruction

  AudioStreamPlayer2D explodePlayer;
  AudioStreamPlayer2D warpOutPlayer;

  /// <summary>
  /// Called when the node enters the scene tree for the first time.
  /// </summary>
  public override void _Ready()
  {
    MyGame = GetNode<Game>("/root/Game");
    _serilogger = MyGame._serilogger;

    shipThing = (Node2D)GetParent();
    Label playerIDLabel = (Label)shipThing.GetNode("Stat/IDLabel");

    hitPointRing = (Sprite2D)GetNode("HitPointShader");

    // TODO: deal with really long UUIDs
    playerIDLabel.Text = uuid;

    explodePlayer = GetNode<AudioStreamPlayer2D>("ExplodeSound");
    warpOutPlayer = GetNode<AudioStreamPlayer2D>("WarpOutSound");
  }

  /// <summary>
  ///
  /// </summary>
  /// <param name="egeb"></param>
  public void UpdateFromGameEventBuffer(GameEvent.GameObject gameObject)
  {
    // if we are marked for destruction, don't process any updates
    if (markedForDestruction) { return; }

    _serilogger.Verbose("PlayerShip.cs: UpdateFromGameEventBuffer");

    HitPoints = gameObject.HitPoints;

    _serilogger.Verbose($"PlayerShip.cs: {uuid}: Current position:  {GlobalPosition.X},{GlobalPosition.Y}");
    _serilogger.Verbose($"PlayerShip.cs: {uuid}: Incoming position: {gameObject.PositionX},{gameObject.PositionY}");

    targetPosition = new Vector2(gameObject.PositionX, gameObject.PositionY);
    targetRotation = gameObject.Angle;
    targetVelocity = gameObject.AbsoluteVelocity;
  }

  /// <summary>
  ///
  /// </summary>
  public void ExpireMissile() { MyMissile = null; }

  public void ExpirePlayer(Enum removeType, UInt32 sequenceNumber)
  {
    markedForDestruction = true;
    destroyedSequenceNumber = sequenceNumber;

    switch (removeType)
    {
      case PlayerRemoveType.Destroy:
        explodePlayer.Play();
        break;
      case PlayerRemoveType.WarpOut:
        warpOutPlayer.Play();
        break;
      default:
        _serilogger.Debug($"PlayerShip.cs: Got unknown remove enum type");
        break;
    }
  }

  // TODO: this is unused -- should we relocate fire methods from Game.cs?
  public void FireMissile()
  {
    // only one missile allowed for now
    if (MyMissile != null) { return; }

    PackedScene missileScene = (PackedScene)ResourceLoader.Load("res://SpaceMissile.tscn");
    MyMissile = (SpaceMissile)missileScene.Instantiate();

    MyMissile.uuid = Guid.NewGuid().ToString();

    // missile should point in the same direction as the ship
    MyMissile.Rotation = Rotation;

    // TODO: need to offset this to the front of the ship
    // start at our position
    MyMissile.Position = GlobalPosition;

    // negative direction is "up"
    Vector2 offset = new Vector2(0, -100);

    // rotate the offset to match the current ship heading
    offset = offset.Rotated(Rotation);
    MyMissile.Position = MyMissile.Position + offset;

    // set missile's parameters based on current modifiers
    MyMissile.MissileSpeed = MissileSpeed;
    MyMissile.MissileLife = MissileLife;
    MyMissile.MissileDamage = MissileDamage;

    // this is a poop way to do this
    MyMissile.MyPlayer = this;

    // put the missile into the missiles group so we can send updates about it later
    MyMissile.AddToGroup("missiles");

    Node rootNode = GetNode<Node>("/root/");
    rootNode.CallDeferred("add_child", MyMissile);
    _serilogger.Debug("Added missile instance!");
  }

  /// <summary>
  ///
  /// </summary>
  /// <param name="Damage"></param>
  public void TakeDamage(int Damage)
  {
    _serilogger.Debug($"Player.cs: {uuid}: Taking damage: {Damage}");
    HitPoints -= Damage;
    _serilogger.Debug($"Player.cs: {uuid}: Hitpoints: {HitPoints}");
  }

  void CheckMissileReload(float delta)
  {
    // nothing to check if we are already reloaded
    if (MissileReady == true) { return; }

    MissileReloadCountdown -= delta;
    if (MissileReloadCountdown <= 0)
    {
      _serilogger.Debug($"PlayerShip.cs: player {uuid} missile reload countdown complete");
      MissileReady = true;
    }
  }

  void UpdateHitPointRing()
  {
    float hitPointRatio = (float)HitPoints / (float)MyGame.PlayerDefaultHitPoints;
    _serilogger.Verbose($"PlayerShip.cs: hitpoints is {HitPoints} for UUID {uuid}");
    _serilogger.Verbose($"PlayerShip.cs: hitpoint ratio {hitPointRatio} for UUID {uuid}");

    ShaderMaterial ringShader = (ShaderMaterial)hitPointRing.Material;
    ringShader.SetShaderParameter("fill_ratio", hitPointRatio);
    _serilogger.Verbose($"PlayerShip.cs: shader fill_ratio is {ringShader.GetShaderParameter("fill_ratio")}");
  }

  void RemovePlayer()
  {
    UInt32 sequenceDiff = MyGame.bufferMessagesCount + destroyedSequenceNumber;
    if (markedForDestruction && MyGame.sequenceNumber > sequenceDiff)
    {
      _serilogger.Verbose($"PlayerShip.cs: player {uuid} marked for destruction {destroyedSequenceNumber}");
      _serilogger.Verbose($"PlayerShip.cs: player {uuid} current sequence {MyGame.sequenceNumber} + {MyGame.bufferMessagesCount} = {sequenceDiff}");

      // TODO: check if any animations are playing and, if so, return

      // check if sounds are playing
      if (explodePlayer.Playing || warpOutPlayer.Playing) { return; }

      // remove from the player object list
      MyGame.playerObjects.Remove(uuid);

      // finally, get rid of ourselves because we're truly gone now
      GetParent().QueueFree();
    }
  }

  public override void _Process(double delta)
  {
    RemovePlayer();
    CheckMissileReload((float)delta);
    UpdateHitPointRing();

    _serilogger.Verbose($"PlayerShip.cs: {uuid}: Current rotation: {RotationDegrees} Target rotation: {targetRotation}");

    // TODO: this results in a little bit of a hitch when the ship crosses the rotation
    // boundary. There's probably a way to improve how this lerps across the boundary

    // check if the target rotation is more than 350 out from the current rotation
    if (Mathf.Abs(RotationDegrees - targetRotation) > 350)
    {
      // if so, just set the rotation to the target rotation
      RotationDegrees = targetRotation;
    }
    else
    {
      // otherwise, lerp
      RotationDegrees = Mathf.Lerp(RotationDegrees, targetRotation, 0.2f);
    }

    CurrentVelocity = Mathf.Lerp(CurrentVelocity, targetVelocity, 0.2f);

    _serilogger.Verbose($"PlayerShip.cs: {uuid}: Current position:  {GlobalPosition.X},{GlobalPosition.Y}");
    _serilogger.Verbose($"PlayerShip.cs: {uuid}: Target position: {targetPosition.X},{targetPosition.Y}");
    GlobalPosition = GlobalPosition.Lerp(targetPosition, 0.2f);
  }


// TODO: probably need to implement ship markedForDestruction like we have for missiles
  void _on_ExplodeSound_finished()
  {
  }

  void _on_WarpOutSound_finished()
  {
  }

}