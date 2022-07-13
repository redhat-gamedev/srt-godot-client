using Godot;
using System;
using redhatgamedev.srt;

public class SpaceMissile : Area2D
{
  CSLogger cslogger;

  public float MissileLife;

  public int MissileSpeed;

  public int MissileDamage;

  public PlayerShip MyPlayer;

  public String uuid;

  AnimatedSprite missileAnimation;
  AnimatedSprite missileExplosion;

  [Signal]
  public delegate void Hit(PlayerShip HitPlayer);

  /// <summary>
  /// 
  /// </summary>
  /// <param name="egeb"></param>
  public void UpdateFromGameEventBuffer(EntityGameEventBuffer egeb)
  {
    cslogger.Verbose($"SpaceMissile.cs: updating missile {uuid}");
    this.GlobalPosition = new Vector2(egeb.Body.Position.X, egeb.Body.Position.Y);
    this.RotationDegrees = egeb.Body.Angle;
  }

  public void Expire()
  {
    // stop the regular animation and play the explosion animation
    cslogger.Debug($"SpaceMissile.cs: missile {uuid} expiring");
    GetNode<Sprite>("Sprite").Hide();
    GetNode<AnimatedSprite>("Animations").Hide();
    missileAnimation.Stop();
    missileAnimation.Frame = 0;
    missileExplosion.Play();
  }

  // Called when the node enters the scene tree for the first time.
  public override void _Ready()
  {
    cslogger = GetNode<CSLogger>("/root/CSLogger");

    // connect the hit signal to handling the hit
    //Connect(nameof(Hit), this, "_HandleHit");

    missileAnimation = GetNode<AnimatedSprite>("Animations");
    missileExplosion = GetNode<AnimatedSprite>("Explosion");
  }

  public override void _Process(float delta)
  {
    if (missileAnimation.Animation == "launch" && missileAnimation.Frame > 30)
    {
      missileAnimation.Frame = 0;
      missileAnimation.Play("travel");
    }
  }

  void _on_Explosion_animation_finished()
  {
    cslogger.Debug($"SpaceMissile.cs: Explosion animation finished - freeing queue");
    // when the explosion animation finishes, remove the missile from the scene
    QueueFree();
  }

  //public override void _PhysicsProcess(float delta)
  //{
  //  // TODO disable the collision shape until the missile is "away" from the ship

  //  // create a new vector and rotate it by the current heading of the missile
  //  // then move the missile in the direction of that vector
  //  Vector2 velocity = new Vector2(0, -1);
  //  velocity = velocity.Rotated(Rotation);
  //  velocity = velocity * MissileSpeed * delta;
  //  Position += velocity;

  //  // once the life reaches zero, remove the missile and don't forget
  //  // to expire it from the parent's perspective
  //  MissileLife -= delta;
  //  if (MissileLife <= 0) { 
  //    QueueFree(); 

  //    // there's got to be a better way
  //    MyPlayer.ExpireMissile();
  //  }
  //}

  //void _onSpaceMissileBodyEntered(Node body)
  //{
  //  cslogger.Debug("SpaceMissile.cs: Body entered!");

  //  if (body.GetType().Name != "PlayerShip")
  //  {
  //    // We didn't hit another player, so remove ourselves, expire the missile, and return
  //    // TODO: may want to decide to do something fancy here
  //    QueueFree();
  //    MyPlayer.ExpireMissile();
  //    return;
  //  }

  //  // We hit another Player, so proceed
  //  EmitSignal("Hit", (PlayerShip)body);

  //  // Must be deferred as we can't change physics properties on a physics callback.
  //  GetNode<CollisionShape2D>("CollisionShape2D").SetDeferred("disabled", true);
  //}

  //void _HandleHit(PlayerShip HitPlayer)
  //{
  //  cslogger.Debug("SpaceMissile.cs: Evaluating hit!");
  //  QueueFree();
  //  MyPlayer.ExpireMissile();
  //  HitPlayer.TakeDamage(MissileDamage);
  //}
}
