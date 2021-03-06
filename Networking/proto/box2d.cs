// <auto-generated>
//   This file was generated by a tool; you should avoid making direct changes.
//   Consider using 'partial classes' to extend these types
//   Input: box2d.proto
// </auto-generated>

#region Designer generated code
#pragma warning disable CS0612, CS0618, CS1591, CS3021, IDE0079, IDE1006, RCS1036, RCS1057, RCS1085, RCS1192
namespace Box2d
{

    [global::ProtoBuf.ProtoContract()]
    public partial class PbVec2 : global::ProtoBuf.IExtensible
    {
        private global::ProtoBuf.IExtension __pbn__extensionData;
        global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
            => global::ProtoBuf.Extensible.GetExtensionObject(ref __pbn__extensionData, createIfMissing);

        [global::ProtoBuf.ProtoMember(1, Name = @"x", IsRequired = true)]
        public float X { get; set; }

        [global::ProtoBuf.ProtoMember(2, Name = @"y", IsRequired = true)]
        public float Y { get; set; }

    }

    [global::ProtoBuf.ProtoContract()]
    public partial class PbFilter : global::ProtoBuf.IExtensible
    {
        private global::ProtoBuf.IExtension __pbn__extensionData;
        global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
            => global::ProtoBuf.Extensible.GetExtensionObject(ref __pbn__extensionData, createIfMissing);

        [global::ProtoBuf.ProtoMember(1, Name = @"category_bits")]
        public int CategoryBits
        {
            get => __pbn__CategoryBits.GetValueOrDefault();
            set => __pbn__CategoryBits = value;
        }
        public bool ShouldSerializeCategoryBits() => __pbn__CategoryBits != null;
        public void ResetCategoryBits() => __pbn__CategoryBits = null;
        private int? __pbn__CategoryBits;

        [global::ProtoBuf.ProtoMember(2, Name = @"mask_bits")]
        public int MaskBits
        {
            get => __pbn__MaskBits.GetValueOrDefault();
            set => __pbn__MaskBits = value;
        }
        public bool ShouldSerializeMaskBits() => __pbn__MaskBits != null;
        public void ResetMaskBits() => __pbn__MaskBits = null;
        private int? __pbn__MaskBits;

        [global::ProtoBuf.ProtoMember(3, Name = @"group_index")]
        public int GroupIndex
        {
            get => __pbn__GroupIndex.GetValueOrDefault();
            set => __pbn__GroupIndex = value;
        }
        public bool ShouldSerializeGroupIndex() => __pbn__GroupIndex != null;
        public void ResetGroupIndex() => __pbn__GroupIndex = null;
        private int? __pbn__GroupIndex;

    }

    [global::ProtoBuf.ProtoContract()]
    public partial class PbShape : global::ProtoBuf.IExtensible
    {
        private global::ProtoBuf.IExtension __pbn__extensionData;
        global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
            => global::ProtoBuf.Extensible.GetExtensionObject(ref __pbn__extensionData, createIfMissing);

        [global::ProtoBuf.ProtoMember(1, Name = @"tag")]
        public long Tag
        {
            get => __pbn__Tag.GetValueOrDefault();
            set => __pbn__Tag = value;
        }
        public bool ShouldSerializeTag() => __pbn__Tag != null;
        public void ResetTag() => __pbn__Tag = null;
        private long? __pbn__Tag;

        [global::ProtoBuf.ProtoMember(2, Name = @"type", IsRequired = true)]
        public PbShapeType Type { get; set; } = PbShapeType.Circle;

        [global::ProtoBuf.ProtoMember(10, Name = @"center")]
        public PbVec2 Center { get; set; }

        [global::ProtoBuf.ProtoMember(11, Name = @"radius")]
        public float Radius
        {
            get => __pbn__Radius.GetValueOrDefault();
            set => __pbn__Radius = value;
        }
        public bool ShouldSerializeRadius() => __pbn__Radius != null;
        public void ResetRadius() => __pbn__Radius = null;
        private float? __pbn__Radius;

        [global::ProtoBuf.ProtoMember(20, Name = @"points")]
        public global::System.Collections.Generic.List<PbVec2> Points { get; } = new global::System.Collections.Generic.List<PbVec2>();

        [global::ProtoBuf.ProtoMember(21, Name = @"normals")]
        public global::System.Collections.Generic.List<PbVec2> Normals { get; } = new global::System.Collections.Generic.List<PbVec2>();

        [global::ProtoBuf.ProtoMember(22, Name = @"centroid")]
        public PbVec2 Centroid { get; set; }

    }

    [global::ProtoBuf.ProtoContract()]
    public partial class PbFixture : global::ProtoBuf.IExtensible
    {
        private global::ProtoBuf.IExtension __pbn__extensionData;
        global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
            => global::ProtoBuf.Extensible.GetExtensionObject(ref __pbn__extensionData, createIfMissing);

        [global::ProtoBuf.ProtoMember(1, Name = @"tag")]
        public long Tag
        {
            get => __pbn__Tag.GetValueOrDefault();
            set => __pbn__Tag = value;
        }
        public bool ShouldSerializeTag() => __pbn__Tag != null;
        public void ResetTag() => __pbn__Tag = null;
        private long? __pbn__Tag;

        [global::ProtoBuf.ProtoMember(2, Name = @"restitution")]
        public float Restitution
        {
            get => __pbn__Restitution.GetValueOrDefault();
            set => __pbn__Restitution = value;
        }
        public bool ShouldSerializeRestitution() => __pbn__Restitution != null;
        public void ResetRestitution() => __pbn__Restitution = null;
        private float? __pbn__Restitution;

        [global::ProtoBuf.ProtoMember(3, Name = @"friction")]
        public float Friction
        {
            get => __pbn__Friction.GetValueOrDefault();
            set => __pbn__Friction = value;
        }
        public bool ShouldSerializeFriction() => __pbn__Friction != null;
        public void ResetFriction() => __pbn__Friction = null;
        private float? __pbn__Friction;

        [global::ProtoBuf.ProtoMember(4, Name = @"density")]
        public float Density
        {
            get => __pbn__Density.GetValueOrDefault();
            set => __pbn__Density = value;
        }
        public bool ShouldSerializeDensity() => __pbn__Density != null;
        public void ResetDensity() => __pbn__Density = null;
        private float? __pbn__Density;

        [global::ProtoBuf.ProtoMember(5, Name = @"sensor")]
        public bool Sensor
        {
            get => __pbn__Sensor.GetValueOrDefault();
            set => __pbn__Sensor = value;
        }
        public bool ShouldSerializeSensor() => __pbn__Sensor != null;
        public void ResetSensor() => __pbn__Sensor = null;
        private bool? __pbn__Sensor;

        [global::ProtoBuf.ProtoMember(10, Name = @"filter")]
        public PbFilter Filter { get; set; }

        [global::ProtoBuf.ProtoMember(11, Name = @"shape")]
        public PbShape Shape { get; set; }

    }

    [global::ProtoBuf.ProtoContract()]
    public partial class PbJoint : global::ProtoBuf.IExtensible
    {
        private global::ProtoBuf.IExtension __pbn__extensionData;
        global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
            => global::ProtoBuf.Extensible.GetExtensionObject(ref __pbn__extensionData, createIfMissing);

        [global::ProtoBuf.ProtoMember(1, Name = @"tag")]
        public long Tag
        {
            get => __pbn__Tag.GetValueOrDefault();
            set => __pbn__Tag = value;
        }
        public bool ShouldSerializeTag() => __pbn__Tag != null;
        public void ResetTag() => __pbn__Tag = null;
        private long? __pbn__Tag;

        [global::ProtoBuf.ProtoMember(2, Name = @"type", IsRequired = true)]
        public PbJointType Type { get; set; } = PbJointType.Distance;

        [global::ProtoBuf.ProtoMember(3, Name = @"body_a")]
        public int BodyA
        {
            get => __pbn__BodyA.GetValueOrDefault();
            set => __pbn__BodyA = value;
        }
        public bool ShouldSerializeBodyA() => __pbn__BodyA != null;
        public void ResetBodyA() => __pbn__BodyA = null;
        private int? __pbn__BodyA;

        [global::ProtoBuf.ProtoMember(4, Name = @"body_b")]
        public int BodyB
        {
            get => __pbn__BodyB.GetValueOrDefault();
            set => __pbn__BodyB = value;
        }
        public bool ShouldSerializeBodyB() => __pbn__BodyB != null;
        public void ResetBodyB() => __pbn__BodyB = null;
        private int? __pbn__BodyB;

        [global::ProtoBuf.ProtoMember(5)]
        public bool collideConnected
        {
            get => __pbn__collideConnected.GetValueOrDefault();
            set => __pbn__collideConnected = value;
        }
        public bool ShouldSerializecollideConnected() => __pbn__collideConnected != null;
        public void ResetcollideConnected() => __pbn__collideConnected = null;
        private bool? __pbn__collideConnected;

        [global::ProtoBuf.ProtoMember(6, Name = @"local_anchor_a")]
        public PbVec2 LocalAnchorA { get; set; }

        [global::ProtoBuf.ProtoMember(7, Name = @"local_anchor_b")]
        public PbVec2 LocalAnchorB { get; set; }

        [global::ProtoBuf.ProtoMember(10, Name = @"ref_angle")]
        public float RefAngle
        {
            get => __pbn__RefAngle.GetValueOrDefault();
            set => __pbn__RefAngle = value;
        }
        public bool ShouldSerializeRefAngle() => __pbn__RefAngle != null;
        public void ResetRefAngle() => __pbn__RefAngle = null;
        private float? __pbn__RefAngle;

        [global::ProtoBuf.ProtoMember(12, Name = @"enable_limit")]
        public bool EnableLimit
        {
            get => __pbn__EnableLimit.GetValueOrDefault();
            set => __pbn__EnableLimit = value;
        }
        public bool ShouldSerializeEnableLimit() => __pbn__EnableLimit != null;
        public void ResetEnableLimit() => __pbn__EnableLimit = null;
        private bool? __pbn__EnableLimit;

        [global::ProtoBuf.ProtoMember(13, Name = @"lower_limit")]
        public float LowerLimit
        {
            get => __pbn__LowerLimit.GetValueOrDefault();
            set => __pbn__LowerLimit = value;
        }
        public bool ShouldSerializeLowerLimit() => __pbn__LowerLimit != null;
        public void ResetLowerLimit() => __pbn__LowerLimit = null;
        private float? __pbn__LowerLimit;

        [global::ProtoBuf.ProtoMember(14, Name = @"upper_limit")]
        public float UpperLimit
        {
            get => __pbn__UpperLimit.GetValueOrDefault();
            set => __pbn__UpperLimit = value;
        }
        public bool ShouldSerializeUpperLimit() => __pbn__UpperLimit != null;
        public void ResetUpperLimit() => __pbn__UpperLimit = null;
        private float? __pbn__UpperLimit;

        [global::ProtoBuf.ProtoMember(15, Name = @"enable_motor")]
        public bool EnableMotor
        {
            get => __pbn__EnableMotor.GetValueOrDefault();
            set => __pbn__EnableMotor = value;
        }
        public bool ShouldSerializeEnableMotor() => __pbn__EnableMotor != null;
        public void ResetEnableMotor() => __pbn__EnableMotor = null;
        private bool? __pbn__EnableMotor;

        [global::ProtoBuf.ProtoMember(16, Name = @"motor_speed")]
        public float MotorSpeed
        {
            get => __pbn__MotorSpeed.GetValueOrDefault();
            set => __pbn__MotorSpeed = value;
        }
        public bool ShouldSerializeMotorSpeed() => __pbn__MotorSpeed != null;
        public void ResetMotorSpeed() => __pbn__MotorSpeed = null;
        private float? __pbn__MotorSpeed;

        [global::ProtoBuf.ProtoMember(17, Name = @"max_motor_torque")]
        public float MaxMotorTorque
        {
            get => __pbn__MaxMotorTorque.GetValueOrDefault();
            set => __pbn__MaxMotorTorque = value;
        }
        public bool ShouldSerializeMaxMotorTorque() => __pbn__MaxMotorTorque != null;
        public void ResetMaxMotorTorque() => __pbn__MaxMotorTorque = null;
        private float? __pbn__MaxMotorTorque;

        [global::ProtoBuf.ProtoMember(20, Name = @"local_axis_a")]
        public PbVec2 LocalAxisA { get; set; }

        [global::ProtoBuf.ProtoMember(21, Name = @"max_motor_force")]
        public float MaxMotorForce
        {
            get => __pbn__MaxMotorForce.GetValueOrDefault();
            set => __pbn__MaxMotorForce = value;
        }
        public bool ShouldSerializeMaxMotorForce() => __pbn__MaxMotorForce != null;
        public void ResetMaxMotorForce() => __pbn__MaxMotorForce = null;
        private float? __pbn__MaxMotorForce;

        [global::ProtoBuf.ProtoMember(30, Name = @"length")]
        public float Length
        {
            get => __pbn__Length.GetValueOrDefault();
            set => __pbn__Length = value;
        }
        public bool ShouldSerializeLength() => __pbn__Length != null;
        public void ResetLength() => __pbn__Length = null;
        private float? __pbn__Length;

        [global::ProtoBuf.ProtoMember(31, Name = @"frequency")]
        public float Frequency
        {
            get => __pbn__Frequency.GetValueOrDefault();
            set => __pbn__Frequency = value;
        }
        public bool ShouldSerializeFrequency() => __pbn__Frequency != null;
        public void ResetFrequency() => __pbn__Frequency = null;
        private float? __pbn__Frequency;

        [global::ProtoBuf.ProtoMember(32, Name = @"damping_ratio")]
        public float DampingRatio
        {
            get => __pbn__DampingRatio.GetValueOrDefault();
            set => __pbn__DampingRatio = value;
        }
        public bool ShouldSerializeDampingRatio() => __pbn__DampingRatio != null;
        public void ResetDampingRatio() => __pbn__DampingRatio = null;
        private float? __pbn__DampingRatio;

        [global::ProtoBuf.ProtoMember(40, Name = @"ground_anchor_a")]
        public PbVec2 GroundAnchorA { get; set; }

        [global::ProtoBuf.ProtoMember(41, Name = @"ground_anchor_b")]
        public PbVec2 GroundAnchorB { get; set; }

        [global::ProtoBuf.ProtoMember(42, Name = @"length_a")]
        public float LengthA
        {
            get => __pbn__LengthA.GetValueOrDefault();
            set => __pbn__LengthA = value;
        }
        public bool ShouldSerializeLengthA() => __pbn__LengthA != null;
        public void ResetLengthA() => __pbn__LengthA = null;
        private float? __pbn__LengthA;

        [global::ProtoBuf.ProtoMember(43, Name = @"length_b")]
        public float LengthB
        {
            get => __pbn__LengthB.GetValueOrDefault();
            set => __pbn__LengthB = value;
        }
        public bool ShouldSerializeLengthB() => __pbn__LengthB != null;
        public void ResetLengthB() => __pbn__LengthB = null;
        private float? __pbn__LengthB;

        [global::ProtoBuf.ProtoMember(44, Name = @"ratio")]
        public float Ratio
        {
            get => __pbn__Ratio.GetValueOrDefault();
            set => __pbn__Ratio = value;
        }
        public bool ShouldSerializeRatio() => __pbn__Ratio != null;
        public void ResetRatio() => __pbn__Ratio = null;
        private float? __pbn__Ratio;

        [global::ProtoBuf.ProtoMember(45, Name = @"max_length_a")]
        public float MaxLengthA
        {
            get => __pbn__MaxLengthA.GetValueOrDefault();
            set => __pbn__MaxLengthA = value;
        }
        public bool ShouldSerializeMaxLengthA() => __pbn__MaxLengthA != null;
        public void ResetMaxLengthA() => __pbn__MaxLengthA = null;
        private float? __pbn__MaxLengthA;

        [global::ProtoBuf.ProtoMember(46, Name = @"max_length_b")]
        public float MaxLengthB
        {
            get => __pbn__MaxLengthB.GetValueOrDefault();
            set => __pbn__MaxLengthB = value;
        }
        public bool ShouldSerializeMaxLengthB() => __pbn__MaxLengthB != null;
        public void ResetMaxLengthB() => __pbn__MaxLengthB = null;
        private float? __pbn__MaxLengthB;

        [global::ProtoBuf.ProtoMember(50, Name = @"target")]
        public PbVec2 Target { get; set; }

        [global::ProtoBuf.ProtoMember(51, Name = @"max_force")]
        public float MaxForce
        {
            get => __pbn__MaxForce.GetValueOrDefault();
            set => __pbn__MaxForce = value;
        }
        public bool ShouldSerializeMaxForce() => __pbn__MaxForce != null;
        public void ResetMaxForce() => __pbn__MaxForce = null;
        private float? __pbn__MaxForce;

        [global::ProtoBuf.ProtoMember(61, Name = @"joint1")]
        public int Joint1
        {
            get => __pbn__Joint1.GetValueOrDefault();
            set => __pbn__Joint1 = value;
        }
        public bool ShouldSerializeJoint1() => __pbn__Joint1 != null;
        public void ResetJoint1() => __pbn__Joint1 = null;
        private int? __pbn__Joint1;

        [global::ProtoBuf.ProtoMember(62, Name = @"joint2")]
        public int Joint2
        {
            get => __pbn__Joint2.GetValueOrDefault();
            set => __pbn__Joint2 = value;
        }
        public bool ShouldSerializeJoint2() => __pbn__Joint2 != null;
        public void ResetJoint2() => __pbn__Joint2 = null;
        private int? __pbn__Joint2;

        [global::ProtoBuf.ProtoMember(70, Name = @"spring_frequency")]
        public float SpringFrequency
        {
            get => __pbn__SpringFrequency.GetValueOrDefault();
            set => __pbn__SpringFrequency = value;
        }
        public bool ShouldSerializeSpringFrequency() => __pbn__SpringFrequency != null;
        public void ResetSpringFrequency() => __pbn__SpringFrequency = null;
        private float? __pbn__SpringFrequency;

        [global::ProtoBuf.ProtoMember(71, Name = @"spring_damping_ratio")]
        public float SpringDampingRatio
        {
            get => __pbn__SpringDampingRatio.GetValueOrDefault();
            set => __pbn__SpringDampingRatio = value;
        }
        public bool ShouldSerializeSpringDampingRatio() => __pbn__SpringDampingRatio != null;
        public void ResetSpringDampingRatio() => __pbn__SpringDampingRatio = null;
        private float? __pbn__SpringDampingRatio;

        [global::ProtoBuf.ProtoMember(90, Name = @"max_torque")]
        public float MaxTorque
        {
            get => __pbn__MaxTorque.GetValueOrDefault();
            set => __pbn__MaxTorque = value;
        }
        public bool ShouldSerializeMaxTorque() => __pbn__MaxTorque != null;
        public void ResetMaxTorque() => __pbn__MaxTorque = null;
        private float? __pbn__MaxTorque;

        [global::ProtoBuf.ProtoMember(100, Name = @"max_length")]
        public float MaxLength
        {
            get => __pbn__MaxLength.GetValueOrDefault();
            set => __pbn__MaxLength = value;
        }
        public bool ShouldSerializeMaxLength() => __pbn__MaxLength != null;
        public void ResetMaxLength() => __pbn__MaxLength = null;
        private float? __pbn__MaxLength;

        [global::ProtoBuf.ProtoMember(110, Name = @"bodies")]
        public int[] Bodies { get; set; }

        [global::ProtoBuf.ProtoMember(111, Name = @"joints")]
        public int[] Joints { get; set; }

        [global::ProtoBuf.ProtoMember(112, Name = @"target_volume")]
        public float TargetVolume
        {
            get => __pbn__TargetVolume.GetValueOrDefault();
            set => __pbn__TargetVolume = value;
        }
        public bool ShouldSerializeTargetVolume() => __pbn__TargetVolume != null;
        public void ResetTargetVolume() => __pbn__TargetVolume = null;
        private float? __pbn__TargetVolume;

    }

    [global::ProtoBuf.ProtoContract()]
    public partial class PbBody : global::ProtoBuf.IExtensible
    {
        private global::ProtoBuf.IExtension __pbn__extensionData;
        global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
            => global::ProtoBuf.Extensible.GetExtensionObject(ref __pbn__extensionData, createIfMissing);

        [global::ProtoBuf.ProtoMember(1, Name = @"tag")]
        public long Tag
        {
            get => __pbn__Tag.GetValueOrDefault();
            set => __pbn__Tag = value;
        }
        public bool ShouldSerializeTag() => __pbn__Tag != null;
        public void ResetTag() => __pbn__Tag = null;
        private long? __pbn__Tag;

        [global::ProtoBuf.ProtoMember(2, Name = @"type", IsRequired = true)]
        public PbBodyType Type { get; set; }

        [global::ProtoBuf.ProtoMember(9, Name = @"absolute_velocity")]
        public float AbsoluteVelocity
        {
            get => __pbn__AbsoluteVelocity.GetValueOrDefault();
            set => __pbn__AbsoluteVelocity = value;
        }
        public bool ShouldSerializeAbsoluteVelocity() => __pbn__AbsoluteVelocity != null;
        public void ResetAbsoluteVelocity() => __pbn__AbsoluteVelocity = null;
        private float? __pbn__AbsoluteVelocity;

        [global::ProtoBuf.ProtoMember(10, Name = @"position")]
        public PbVec2 Position { get; set; }

        [global::ProtoBuf.ProtoMember(11, Name = @"angle")]
        public float Angle
        {
            get => __pbn__Angle.GetValueOrDefault();
            set => __pbn__Angle = value;
        }
        public bool ShouldSerializeAngle() => __pbn__Angle != null;
        public void ResetAngle() => __pbn__Angle = null;
        private float? __pbn__Angle;

        [global::ProtoBuf.ProtoMember(12, Name = @"linear_velocity")]
        public PbVec2 LinearVelocity { get; set; }

        [global::ProtoBuf.ProtoMember(13, Name = @"angular_velocity")]
        public float AngularVelocity
        {
            get => __pbn__AngularVelocity.GetValueOrDefault();
            set => __pbn__AngularVelocity = value;
        }
        public bool ShouldSerializeAngularVelocity() => __pbn__AngularVelocity != null;
        public void ResetAngularVelocity() => __pbn__AngularVelocity = null;
        private float? __pbn__AngularVelocity;

        [global::ProtoBuf.ProtoMember(14, Name = @"force")]
        public PbVec2 Force { get; set; }

        [global::ProtoBuf.ProtoMember(15, Name = @"torque")]
        public float Torque
        {
            get => __pbn__Torque.GetValueOrDefault();
            set => __pbn__Torque = value;
        }
        public bool ShouldSerializeTorque() => __pbn__Torque != null;
        public void ResetTorque() => __pbn__Torque = null;
        private float? __pbn__Torque;

        [global::ProtoBuf.ProtoMember(16, Name = @"mass")]
        public float Mass
        {
            get => __pbn__Mass.GetValueOrDefault();
            set => __pbn__Mass = value;
        }
        public bool ShouldSerializeMass() => __pbn__Mass != null;
        public void ResetMass() => __pbn__Mass = null;
        private float? __pbn__Mass;

        [global::ProtoBuf.ProtoMember(17)]
        public float I
        {
            get => __pbn__I.GetValueOrDefault();
            set => __pbn__I = value;
        }
        public bool ShouldSerializeI() => __pbn__I != null;
        public void ResetI() => __pbn__I = null;
        private float? __pbn__I;

        [global::ProtoBuf.ProtoMember(50, Name = @"linear_damping")]
        public float LinearDamping
        {
            get => __pbn__LinearDamping.GetValueOrDefault();
            set => __pbn__LinearDamping = value;
        }
        public bool ShouldSerializeLinearDamping() => __pbn__LinearDamping != null;
        public void ResetLinearDamping() => __pbn__LinearDamping = null;
        private float? __pbn__LinearDamping;

        [global::ProtoBuf.ProtoMember(51, Name = @"angular_damping")]
        public float AngularDamping
        {
            get => __pbn__AngularDamping.GetValueOrDefault();
            set => __pbn__AngularDamping = value;
        }
        public bool ShouldSerializeAngularDamping() => __pbn__AngularDamping != null;
        public void ResetAngularDamping() => __pbn__AngularDamping = null;
        private float? __pbn__AngularDamping;

        [global::ProtoBuf.ProtoMember(52, Name = @"gravity_scale")]
        public float GravityScale
        {
            get => __pbn__GravityScale.GetValueOrDefault();
            set => __pbn__GravityScale = value;
        }
        public bool ShouldSerializeGravityScale() => __pbn__GravityScale != null;
        public void ResetGravityScale() => __pbn__GravityScale = null;
        private float? __pbn__GravityScale;

        [global::ProtoBuf.ProtoMember(53, Name = @"bullet")]
        public bool Bullet
        {
            get => __pbn__Bullet.GetValueOrDefault();
            set => __pbn__Bullet = value;
        }
        public bool ShouldSerializeBullet() => __pbn__Bullet != null;
        public void ResetBullet() => __pbn__Bullet = null;
        private bool? __pbn__Bullet;

        [global::ProtoBuf.ProtoMember(54, Name = @"allow_sleep")]
        public bool AllowSleep
        {
            get => __pbn__AllowSleep.GetValueOrDefault();
            set => __pbn__AllowSleep = value;
        }
        public bool ShouldSerializeAllowSleep() => __pbn__AllowSleep != null;
        public void ResetAllowSleep() => __pbn__AllowSleep = null;
        private bool? __pbn__AllowSleep;

        [global::ProtoBuf.ProtoMember(55, Name = @"awake")]
        public bool Awake
        {
            get => __pbn__Awake.GetValueOrDefault();
            set => __pbn__Awake = value;
        }
        public bool ShouldSerializeAwake() => __pbn__Awake != null;
        public void ResetAwake() => __pbn__Awake = null;
        private bool? __pbn__Awake;

        [global::ProtoBuf.ProtoMember(56, Name = @"active")]
        public bool Active
        {
            get => __pbn__Active.GetValueOrDefault();
            set => __pbn__Active = value;
        }
        public bool ShouldSerializeActive() => __pbn__Active != null;
        public void ResetActive() => __pbn__Active = null;
        private bool? __pbn__Active;

        [global::ProtoBuf.ProtoMember(57, Name = @"fixed_rotation")]
        public bool FixedRotation
        {
            get => __pbn__FixedRotation.GetValueOrDefault();
            set => __pbn__FixedRotation = value;
        }
        public bool ShouldSerializeFixedRotation() => __pbn__FixedRotation != null;
        public void ResetFixedRotation() => __pbn__FixedRotation = null;
        private bool? __pbn__FixedRotation;

        [global::ProtoBuf.ProtoMember(60, Name = @"UUID")]
        [global::System.ComponentModel.DefaultValue("")]
        public string Uuid
        {
            get => __pbn__Uuid ?? "";
            set => __pbn__Uuid = value;
        }
        public bool ShouldSerializeUuid() => __pbn__Uuid != null;
        public void ResetUuid() => __pbn__Uuid = null;
        private string __pbn__Uuid;

        [global::ProtoBuf.ProtoMember(100, Name = @"fixtures")]
        public global::System.Collections.Generic.List<PbFixture> Fixtures { get; } = new global::System.Collections.Generic.List<PbFixture>();

    }

    [global::ProtoBuf.ProtoContract()]
    public partial class PbWorld : global::ProtoBuf.IExtensible
    {
        private global::ProtoBuf.IExtension __pbn__extensionData;
        global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
            => global::ProtoBuf.Extensible.GetExtensionObject(ref __pbn__extensionData, createIfMissing);

        [global::ProtoBuf.ProtoMember(1, Name = @"tag")]
        public long Tag
        {
            get => __pbn__Tag.GetValueOrDefault();
            set => __pbn__Tag = value;
        }
        public bool ShouldSerializeTag() => __pbn__Tag != null;
        public void ResetTag() => __pbn__Tag = null;
        private long? __pbn__Tag;

        [global::ProtoBuf.ProtoMember(2, Name = @"gravity")]
        public PbVec2 Gravity { get; set; }

        [global::ProtoBuf.ProtoMember(3, Name = @"allow_sleep")]
        public bool AllowSleep
        {
            get => __pbn__AllowSleep.GetValueOrDefault();
            set => __pbn__AllowSleep = value;
        }
        public bool ShouldSerializeAllowSleep() => __pbn__AllowSleep != null;
        public void ResetAllowSleep() => __pbn__AllowSleep = null;
        private bool? __pbn__AllowSleep;

        [global::ProtoBuf.ProtoMember(4, Name = @"auto_clear_forces")]
        public bool AutoClearForces
        {
            get => __pbn__AutoClearForces.GetValueOrDefault();
            set => __pbn__AutoClearForces = value;
        }
        public bool ShouldSerializeAutoClearForces() => __pbn__AutoClearForces != null;
        public void ResetAutoClearForces() => __pbn__AutoClearForces = null;
        private bool? __pbn__AutoClearForces;

        [global::ProtoBuf.ProtoMember(5, Name = @"warm_starting")]
        public bool WarmStarting
        {
            get => __pbn__WarmStarting.GetValueOrDefault();
            set => __pbn__WarmStarting = value;
        }
        public bool ShouldSerializeWarmStarting() => __pbn__WarmStarting != null;
        public void ResetWarmStarting() => __pbn__WarmStarting = null;
        private bool? __pbn__WarmStarting;

        [global::ProtoBuf.ProtoMember(6, Name = @"continuous_physics")]
        public bool ContinuousPhysics
        {
            get => __pbn__ContinuousPhysics.GetValueOrDefault();
            set => __pbn__ContinuousPhysics = value;
        }
        public bool ShouldSerializeContinuousPhysics() => __pbn__ContinuousPhysics != null;
        public void ResetContinuousPhysics() => __pbn__ContinuousPhysics = null;
        private bool? __pbn__ContinuousPhysics;

        [global::ProtoBuf.ProtoMember(7, Name = @"sub_stepping")]
        public bool SubStepping
        {
            get => __pbn__SubStepping.GetValueOrDefault();
            set => __pbn__SubStepping = value;
        }
        public bool ShouldSerializeSubStepping() => __pbn__SubStepping != null;
        public void ResetSubStepping() => __pbn__SubStepping = null;
        private bool? __pbn__SubStepping;

        [global::ProtoBuf.ProtoMember(20, Name = @"bodies")]
        public global::System.Collections.Generic.List<PbBody> Bodies { get; } = new global::System.Collections.Generic.List<PbBody>();

        [global::ProtoBuf.ProtoMember(21, Name = @"joints")]
        public global::System.Collections.Generic.List<PbJoint> Joints { get; } = new global::System.Collections.Generic.List<PbJoint>();

    }

    [global::ProtoBuf.ProtoContract()]
    public enum PbBodyType
    {
        [global::ProtoBuf.ProtoEnum(Name = @"STATIC")]
        Static = 0,
        [global::ProtoBuf.ProtoEnum(Name = @"DYNAMIC")]
        Dynamic = 1,
        [global::ProtoBuf.ProtoEnum(Name = @"KINEMATIC")]
        Kinematic = 2,
    }

    [global::ProtoBuf.ProtoContract()]
    public enum PbShapeType
    {
        [global::ProtoBuf.ProtoEnum(Name = @"CIRCLE")]
        Circle = 1,
        [global::ProtoBuf.ProtoEnum(Name = @"POLYGON")]
        Polygon = 2,
        [global::ProtoBuf.ProtoEnum(Name = @"EDGE")]
        Edge = 3,
        [global::ProtoBuf.ProtoEnum(Name = @"LOOP")]
        Loop = 4,
    }

    [global::ProtoBuf.ProtoContract()]
    public enum PbJointType
    {
        [global::ProtoBuf.ProtoEnum(Name = @"DISTANCE")]
        Distance = 1,
        [global::ProtoBuf.ProtoEnum(Name = @"REVOLUTE")]
        Revolute = 2,
        [global::ProtoBuf.ProtoEnum(Name = @"PRISMATIC")]
        Prismatic = 3,
        [global::ProtoBuf.ProtoEnum(Name = @"PULLEY")]
        Pulley = 4,
        [global::ProtoBuf.ProtoEnum(Name = @"MOUSE")]
        Mouse = 5,
        [global::ProtoBuf.ProtoEnum(Name = @"GEAR")]
        Gear = 6,
        [global::ProtoBuf.ProtoEnum(Name = @"WHEEL")]
        Wheel = 7,
        [global::ProtoBuf.ProtoEnum(Name = @"WELD")]
        Weld = 8,
        [global::ProtoBuf.ProtoEnum(Name = @"FRICTION")]
        Friction = 9,
        [global::ProtoBuf.ProtoEnum(Name = @"ROPE")]
        Rope = 10,
        [global::ProtoBuf.ProtoEnum(Name = @"CONSTANT_VOLUME")]
        ConstantVolume = 11,
        [global::ProtoBuf.ProtoEnum(Name = @"LINE")]
        Line = 12,
    }

}

#pragma warning restore CS0612, CS0618, CS1591, CS3021, IDE0079, IDE1006, RCS1036, RCS1057, RCS1085, RCS1192
#endregion
