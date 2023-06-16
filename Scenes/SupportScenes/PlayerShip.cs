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
  }

  /// <summary>
  ///
  /// </summary>
  /// <param name="egeb"></param>
  public void UpdateFromGameEventBuffer(GameEvent.GameObject gameObject)
  {
    _serilogger.Verbose("PlayerShip.cs: UpdateFromGameEventBuffer");

    HitPoints = gameObject.HitPoints;

    float xPos = Mathf.Lerp(GlobalPosition.X, gameObject.PositionX, 0.5f);
    float yPos = Mathf.Lerp(GlobalPosition.Y, gameObject.PositionY, 0.5f);
    GlobalPosition = new Vector2(xPos, yPos);
    RotationDegrees = Mathf.Lerp(RotationDegrees, gameObject.Angle, 0.5f);
    CurrentVelocity = Mathf.Lerp(CurrentVelocity, gameObject.AbsoluteVelocity, 0.5f);
  }

  /// <summary>
  ///
  /// </summary>
  public void ExpireMissile() { MyMissile = null; }

  public void ExpirePlayer()
  {
    // need to free the parent of the ship, which is the "shipthing"
    GetParent().QueueFree();
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

  public override void _Process(double delta)
  {
    CheckMissileReload((float)delta);
    UpdateHitPointRing();
  }

  void _on_ExplodeSound_finished()
  {
    _serilogger.Verbose($"PlayerShip.cs: Explosion sound finished - expiring player");
    ExpirePlayer();
  }

  void _on_WarpOutSound_finished()
  {
    _serilogger.Verbose($"PlayerShip.cs: Warp out sound finished - expiring player");
    ExpirePlayer();
  }

}