using Godot;
using System;
using redhatgamedev.srt.v1;

public partial class SpaceMissile : Area2D
{
  Game MyGame;

  public Serilog.Core.Logger _serilogger;

  // TODO: need to get this information from the server
  public float MissileLife = 2;

  public int MissileSpeed = 300;

  public int MissileDamage;

  public PlayerShip MyPlayer;

  public String uuid;

  private bool markedForDestruction = false;
  private UInt32 destroyedSequenceNumber; // the sequence when this missile was marked for destruction

  AnimatedSprite2D missileAnimation;
  AnimatedSprite2D missileExplosion;

// I don't think this signal is used any more in the client
  [Signal]
  public delegate void HitEventHandler(PlayerShip HitPlayer);

  // when the missile is first created, it should be recently initialized, which tells
  // us to ignore lerping and wait for updates, although we can be initialized with a
  // rotation and velocity so
  public bool recentlyInitialized = true;
  Vector2 targetPosition;

  /// <summary>
  ///
  /// </summary>
  /// <param name="egeb"></param>
  public void UpdateFromGameEventBuffer(GameEvent.GameObject gameObject)
  {
    // if the missile is marked for destruction, don't bother processing the update here
    if (markedForDestruction)
    {
      return;
    }

    // on the first update, we are no longer recently initialized
    recentlyInitialized = false;

    _serilogger.Verbose($"SpaceMissile.cs: updating missile {uuid}");
    //Position = new Vector2(gameObject.PositionX, gameObject.PositionY);

    targetPosition = new Vector2(gameObject.PositionX, gameObject.PositionY);

    // don't bother lerping angle or speed
    RotationDegrees = gameObject.Angle;
    MissileSpeed = (int)gameObject.AbsoluteVelocity;

    //_serilogger.Verbose($"SpaceMissile.cs: updating missile {uuid}");
    //targetPosition = new Vector2(gameObject.PositionX, gameObject.PositionY);
    //targetRotation = gameObject.Angle;
    //targetVelocity = gameObject.AbsoluteVelocity;
  }

  public void Expire(UInt32 sequenceNumber)
  {
    // mark for destruction
    markedForDestruction = true;
    destroyedSequenceNumber = sequenceNumber;

    // stop the regular animation and play the explosion animation
    _serilogger.Debug($"SpaceMissile.cs: missile {uuid} expiring");
    GetNode<Sprite2D>("Sprite2D").Hide();
    GetNode<AnimatedSprite2D>("Animations").Hide();
    missileAnimation.Stop();
    missileAnimation.Frame = 0;
    missileExplosion.Play();
    GetNode<AudioStreamPlayer2D>("ExplodeSound").Play();

    if (MyPlayer != null) MyPlayer.MyMissile = null;
  }

  // Called when the node enters the scene tree for the first time.
  public override void _Ready()
  {
    MyGame = GetNode<Game>("/root/Game");
    _serilogger = MyGame._serilogger;

    // connect the hit signal to handling the hit
    //Connect(nameof(Hit), this, "_HandleHit");

    missileAnimation = GetNode<AnimatedSprite2D>("Animations");
    missileExplosion = GetNode<AnimatedSprite2D>("Explosion");
    //GetNode<AudioStreamPlayer>("FireSound").Play();
  }

  public override void _Process(double delta)
  {
    // lerp the interp
    // TODO: if we were recently initialized, move us via basic math instead

    if (recentlyInitialized)
    {
      _serilogger.Debug($"SpaceMissile.cs: missile {uuid} recently initialized, skipping");
    }

    if (!recentlyInitialized)
    {
      Position = Position.Lerp(targetPosition, 0.2f);
    }

    // TODO: should probably switch this to some kind of signal
    if (missileAnimation.Animation == "launch" && missileAnimation.Frame > 30)
    {
      _serilogger.Debug($"SpaceMissile.cs: missile {uuid} launch animation finished, playing travel animation");
      missileAnimation.Frame = 0;
      missileAnimation.Play("travel");
    }

    // check if we should be removed
    UInt32 sequenceDiff = MyGame.bufferMessagesCount + destroyedSequenceNumber;
    if (markedForDestruction && MyGame.sequenceNumber > sequenceDiff)
    {
      _serilogger.Verbose($"SpaceMissile.cs: missile {uuid} marked for destruction {destroyedSequenceNumber}");
      _serilogger.Verbose($"SpaceMissile.cs: missile {uuid} current sequence {MyGame.sequenceNumber} + {MyGame.bufferMessagesCount} = {sequenceDiff}");

      // check if the animation is still playing
      if (missileExplosion.IsPlaying())
      {
        _serilogger.Verbose($"SpaceMissile.cs: missile being destroyed but animation still playing");
        // if it is, then we're still waiting for the animation to finish
        return;
      }

      // once the animation is finished, remove the missile from the server's
      // list because it's truly gone now
      MyGame.missileObjects.Remove(uuid);

      // fully destroy the missile
      QueueFree();
    }
  }

  void _on_Explosion_animation_finished()
  {
    _serilogger.Debug($"SpaceMissile.cs: Explosion animation finished - hiding missile {uuid}");
    // when the explosion animation finishes, hide the missile from the scene for later destruction
    Hide();
  }

  // TODO: Investigate whether this conflicts with receiving the updates about
  // our own missile
  public override void _PhysicsProcess(double delta)
  {
    // TODO disable the collision shape until the missile is "away" from the ship

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
