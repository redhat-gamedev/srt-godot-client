using Godot;
using System;
using redhatgamedev.srt.v1;

public class SpaceMissile : Area2D
{
  Game MyGame;

  public Serilog.Core.Logger _serilogger;

  // TODO: need to get this information from the server
  public float MissileLife = 2;

  public int MissileSpeed = 300;

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
  public void UpdateFromGameEventBuffer(GameEvent egeb)
  {
	_serilogger.Verbose($"SpaceMissile.cs: updating missile {uuid}");
	this.GlobalPosition = new Vector2(egeb.PositionX, egeb.PositionY);
	this.RotationDegrees = egeb.Angle;
  }

  public void Expire()
  {
	// stop the regular animation and play the explosion animation
	_serilogger.Debug($"SpaceMissile.cs: missile {uuid} expiring");
	GetNode<Sprite>("Sprite").Hide();
	GetNode<AnimatedSprite>("Animations").Hide();
	missileAnimation.Stop();
	missileAnimation.Frame = 0;
	missileExplosion.Play();

	if (MyPlayer != null) MyPlayer.MyMissile = null;
  }

  // Called when the node enters the scene tree for the first time.
  public override void _Ready()
  {
	MyGame = GetNode<Game>("/root/Game");
	_serilogger = MyGame._serilogger;

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
	_serilogger.Debug($"SpaceMissile.cs: Explosion animation finished - freeing queue");
	// when the explosion animation finishes, remove the missile from the scene
	QueueFree();
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
  }

  //void _onSpaceMissileBodyEntered(Node body)
  //{
  //  _serilogger.Debug("SpaceMissile.cs: Body entered!");

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
  //  _serilogger.Debug("SpaceMissile.cs: Evaluating hit!");
  //  QueueFree();
  //  MyPlayer.ExpireMissile();
  //  HitPlayer.TakeDamage(MissileDamage);
  //}
}
