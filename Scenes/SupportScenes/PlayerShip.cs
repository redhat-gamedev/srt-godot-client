using Godot;
using System;
using System.Collections;
using redhatgamedev.srt;

public class PlayerShip : KinematicBody2D
{
  [Export]
  public float Thrust = 1f; // effective acceleration

  [Export]
  public float MaxSpeed = 5;

  [Export]
  public float StopThreshold = 10f;

  [Export]
  public float GoThreshold = 90f;

  [Export]
  public float CurrentVelocity = 0;

  [Export]
  public float RotationThrust = 1.5f;

  [Export]
  public float CurrentRotation = 0;

  [Export]
  public int HitPoints = 100;

  [Export]
  int MissileSpeed = 300;

  [Export]
  float MissileLife = 4;

  [Export]
  int MissileDamage = 25;

  public String uuid;

  Game MyGame;

  public Serilog.Core.Logger _serilogger;

  //public Queue MovementQueue = new Queue();
  public SpaceMissile MyMissile = null; // for now only one missile at a time

  Node2D shipThing;

  /// <summary>
  /// Called when the node enters the scene tree for the first time.
  /// </summary>
  public override void _Ready()
  {
    MyGame = GetNode<Game>("/root/Game");
    _serilogger = MyGame._serilogger;

    shipThing = (Node2D)GetParent();
    //Label playerIDLabel = (Label)shipThing.GetNode("Stat/IDLabel");

    //// TODO: deal with really long UUIDs
    //playerIDLabel.Text = uuid;
  }

  /// <summary>
  /// 
  /// </summary>
  /// <param name="BufferType"></param>
  /// <returns></returns>
  public EntityGameEventBuffer CreatePlayerGameEventBuffer(EntityGameEventBuffer.EntityGameEventBufferType BufferType)
  {
    EntityGameEventBuffer egeb = new EntityGameEventBuffer();
    egeb.Type = BufferType;
    egeb.objectType = EntityGameEventBuffer.EntityGameEventBufferObjectType.Player;
    egeb.Uuid = uuid;

    Box2d.PbBody body = new Box2d.PbBody();
    body.Type = Box2d.PbBodyType.Kinematic; // not sure if this should maybe be static

    // need to use the GlobalPosition because the ship node ends up being offset
    // from the parent Node2D
    body.Position = new Box2d.PbVec2
    {
      X = GlobalPosition.x,
      Y = GlobalPosition.y
    };

    body.Angle = RotationDegrees;
    body.AbsoluteVelocity = CurrentVelocity;

    egeb.Body = body;
    return egeb;
  }

  /// <summary>
  /// 
  /// </summary>
  /// <param name="egeb"></param>
  public void UpdateFromGameEventBuffer(EntityGameEventBuffer egeb)
  {
    this.GlobalPosition = new Vector2(egeb.Body.Position.X, egeb.Body.Position.Y);
    this.RotationDegrees = egeb.Body.Angle;
    this.CurrentVelocity = egeb.Body.AbsoluteVelocity;
  }

  /// <summary>
  /// 
  /// </summary>
  public void ExpireMissile() { MyMissile = null; }

  /// <summary>
  /// 
  /// </summary>
  public void FireMissile()
  {
    // only one missile allowed for now
    if (MyMissile != null) { return; }

    PackedScene missileScene = (PackedScene)ResourceLoader.Load("res://SpaceMissile.tscn");
    MyMissile = (SpaceMissile)missileScene.Instance();

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
    rootNode.AddChild(MyMissile);
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

  //void RemovePlayer()
  //{
  //  cslogger.Verbose($"Player.cs: removing {uuid}");
  //  Server theServer = (Server)GetNode("/root/Server");
  //  theServer.RemovePlayer(uuid);
  //s}

  /// <summary>
  /// 
  /// </summary>
  /// <param name="delta"></param>
  public override void _Process(float delta)
  {
    //if (HitPoints <= 0)
    //{
    //  _serilogger.Debug("Hitpoints zeroed! Remove the player!");
    //  //RemovePlayer();
    //  // TODO: you're dead - should this come from server of processed here?
    //}

    //Node2D shipThing = (Node2D)GetParent();

    //// TODO: we are doing instant rotation so probably should rename this
    //Label angularVelocityLabel = (Label)shipThing.GetNode("Stat/AngularVelocity");
    //Label linearVelocityLabel = (Label)shipThing.GetNode("Stat/LinearVelocity");
    //Label hitPointsLabel = (Label)shipThing.GetNode("Stat/HitPoints");
    //Label positionLabel = (Label)shipThing.GetNode("Stat/Position");
    //Label hexLabel = (Label)shipThing.GetNode("Stat/Hex");

    //angularVelocityLabel.Text = $"Rot: {RotationDegrees}";
    //linearVelocityLabel.Text = $"Vel: {CurrentVelocity}";
    //hitPointsLabel.Text = $"HP: {HitPoints}";
    //positionLabel.Text = $"X: {GlobalPosition.x} Y: {GlobalPosition.y}";
  }

}
