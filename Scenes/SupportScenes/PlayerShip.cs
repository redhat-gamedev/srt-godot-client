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

  public Queue MovementQueue = new Queue();

  public String uuid;

  CSLogger cslogger;

  // for now only one missile at a time
  SpaceMissile MyMissile = null;

  [Export] 
  int MissileSpeed = 300;
  
  [Export]
  float MissileLife = 4;

  [Export]
  int MissileDamage = 25;

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

  public void ExpireMissile() { MyMissile = null; }

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

    Node rootNode = GetNode<Node>("/root");
    rootNode.AddChild(MyMissile);
    cslogger.Debug("Added missile instance!");
  }

  // Called when the node enters the scene tree for the first time.
  public override void _Ready()
  {
    // initialize the logging configuration
    cslogger = GetNode<CSLogger>("/root/CSLogger");

    Node2D shipThing = (Node2D)GetParent();
    Label playerIDLabel = (Label)shipThing.GetNode("Stat/IDLabel");

    // TODO: deal with really long UUIDs
    playerIDLabel.Text = uuid;
  }

  public void TakeDamage(int Damage)
  {
    cslogger.Debug($"Player.cs: {uuid}: Taking damage: {Damage}");
    HitPoints -= Damage;
    cslogger.Debug($"Player.cs: {uuid}: Hitpoints: {HitPoints}");
  }

  //void RemovePlayer()
  //{
  //  cslogger.Verbose($"Player.cs: removing {uuid}");
  //  Server theServer = (Server)GetNode("/root/Server");
  //  theServer.RemovePlayer(uuid);
  //s}

  public override void _Process(float delta)
  {
    if (HitPoints <= 0)
    {
      cslogger.Debug("Hitpoints zeroed! Remove the player!");
      //RemovePlayer();
      // TODO: you're dead - should this come from server of processed here?
    }
  }

  public override void _PhysicsProcess(float delta)
  {
    // somewhat based on: https://kidscancode.org/godot_recipes/2d/topdown_movement/
    // "rotate and move" / asteroids-style-ish

    Node2D shipThing = (Node2D)GetParent();

    // TODO: we are doing instant rotation so probably should rename this
    Label angularVelocityLabel = (Label)shipThing.GetNode("Stat/AngularVelocity");
    Label linearVelocityLabel = (Label)shipThing.GetNode("Stat/LinearVelocity");
    Label hitPointsLabel = (Label)shipThing.GetNode("Stat/HitPoints");
    Label positionLabel = (Label)shipThing.GetNode("Stat/Position");
    Label hexLabel = (Label)shipThing.GetNode("Stat/Hex");

    angularVelocityLabel.Text = $"Rot: {RotationDegrees}";
    linearVelocityLabel.Text = $"Vel: {CurrentVelocity}";
    hitPointsLabel.Text = $"HP: {HitPoints}";
    positionLabel.Text = $"X: {GlobalPosition.x} Y: {GlobalPosition.y}";

    // Server theServer = GetNode<Server>("/root/Server");

    // TODO: get this from server instead of calculating it
    //// figure out the hex from the pixel position
    //Layout theLayout = theServer.HexLayout;
    //FractionalHex theHex = theLayout.PixelToHex(new Point(GlobalPosition.x, GlobalPosition.y));

    //hexLabel.Text = $"q: {theHex.HexRound().q}, r: {theHex.HexRound().r}, s: {theHex.HexRound().s}";

    float rotation_dir = 0; // in case we need it

    cslogger.Verbose($"{uuid}: handling physics");
    if (MovementQueue.Count > 0)
    {
      Vector2 thisMovement = (Vector2)MovementQueue.Dequeue();
      cslogger.Verbose($"UUID: {uuid} X: {thisMovement.x} Y: {thisMovement.y}");

      if (thisMovement.y > 0)
      {
        CurrentVelocity = Mathf.Lerp(CurrentVelocity, MaxSpeed, Thrust * delta);

        // max out speed when velocity gets above threshold for same reason
        if (CurrentVelocity > MaxSpeed * (GoThreshold/100)) { CurrentVelocity = MaxSpeed; }
      }

      if (thisMovement.y < 0)
      {
        CurrentVelocity = Mathf.Lerp(CurrentVelocity, 0, Thrust * delta);

        // cut speed when velocity gets below threshold, otherwise LERPing
        // results in never actually stopping. 
        if (CurrentVelocity < MaxSpeed * (StopThreshold/100)) { CurrentVelocity = 0; }
      }

      if (thisMovement.x != 0)
      {
        rotation_dir = thisMovement.x;
      }

      cslogger.Verbose($"UUID: {uuid} Velocity: {CurrentVelocity}");

    }
    Vector2 velocity =  -(Transform.y * CurrentVelocity);
    cslogger.Verbose($"UUID: {uuid} Vector X: {velocity.x} Y: {velocity.y} ");
    Rotation += rotation_dir * RotationThrust * delta;

    // TODO: implement collision mechanics
    MoveAndCollide(velocity);

    // TODO: have the server do this - not the client
    //// clamp the player to the starfield radius
    //Int32 starFieldRadiusPixels = theServer.StarFieldRadiusPixels;
    //Vector2 currentGlobalPosition = GlobalPosition;
    //if (currentGlobalPosition.Length() > starFieldRadiusPixels)
    //{
    //  Vector2 newPosition = starFieldRadiusPixels * currentGlobalPosition.Normalized();
    //  GlobalPosition = newPosition;
    //}
  }

}
