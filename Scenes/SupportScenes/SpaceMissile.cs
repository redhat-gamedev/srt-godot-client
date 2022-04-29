using Godot;
using System;

public class SpaceMissile : Area2D
{
  CSLogger cslogger;

  public float MissileLife;

  public int MissileSpeed;

  public int MissileDamage;

  public PlayerShip MyPlayer;

  public String uuid;

  [Signal]
  public delegate void Hit(PlayerShip HitPlayer);

  // Called when the node enters the scene tree for the first time.
  public override void _Ready() 
  {  
    cslogger = GetNode<CSLogger>("/root/CSLogger");

    // connect the hit signal to handling the hit
    Connect(nameof(Hit), this, "_HandleHit");
  }

  public override void _PhysicsProcess(float delta)
  {
    // TODO disable the collision shape until the missile is "away" from the ship

    // create a new vector and rotate it by the current heading of the missile
    // then move the missile in the direction of that vector
    Vector2 velocity = new Vector2(0, -1);
    velocity = velocity.Rotated(Rotation);
    velocity = velocity * MissileSpeed * delta;
    Position += velocity;

    // once the life reaches zero, remove the missile and don't forget
    // to expire it from the parent's perspective
    MissileLife -= delta;
    if (MissileLife <= 0) { 
      QueueFree(); 

      // there's got to be a better way
      MyPlayer.ExpireMissile();
    }
  }

  void _onSpaceMissileBodyEntered(Node body)
  {
    cslogger.Debug("SpaceMissile.cs: Body entered!");

    if (body.GetType().Name != "PlayerShip")
    {
      // We didn't hit another player, so remove ourselves, expire the missile, and return
      // TODO: may want to decide to do something fancy here
      QueueFree();
      MyPlayer.ExpireMissile();
      return;
    }

    // We hit another Player, so proceed
    EmitSignal("Hit", (PlayerShip)body);

    // Must be deferred as we can't change physics properties on a physics callback.
    GetNode<CollisionShape2D>("CollisionShape2D").SetDeferred("disabled", true);
  }

  void _HandleHit(PlayerShip HitPlayer)
  {
    cslogger.Debug("SpaceMissile.cs: Evaluating hit!");
    QueueFree();
    MyPlayer.ExpireMissile();
    HitPlayer.TakeDamage(MissileDamage);
  }
}
