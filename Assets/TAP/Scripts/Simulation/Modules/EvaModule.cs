using System.Collections.Generic;
using TAP.Core;
using TAP.Parts;
using UnityEngine;

namespace TAP.Simulation
{
    /// <summary>
    /// EVA crew member (a one-part vessel). Uses the same gravity/orbit simulation as vessels.
    /// Walking is force-driven with traction limited by local gravity (so low-g walking is floaty),
    /// jumping applies an impulse, and a limited-propellant jetpack provides translation.
    /// </summary>
    public sealed class EvaModule : PartModule
    {
        // Inputs (set by the game input layer for the active EVA vessel)
        public Vector2 MoveInput;         // x: right, y: forward (camera relative)
        public float VerticalInput;       // jetpack up/down
        public bool RunInput;
        public bool JumpPressed;
        public bool JetpackOn;
        public Quaternion CameraRotation = Quaternion.identity;

        public const float WalkSpeed = 1.8f;
        public const float RunSpeed = 3.6f;
        public const float JumpSpeed = 2.4f;
        public const float JetpackThrust = 320f;   // N
        public const double JetpackIsp = 100;      // s
        public const float BoardRange = 3.0f;
        public const float BoardMaxRelSpeed = 2.5f;

        public bool Grounded;
        /// <summary>What the ground probe is standing on (terrain or a vessel part), null in the air.</summary>
        public Collider GroundCollider;
        /// <summary>Standing on the planet's surface rather than on a part.</summary>
        public bool OnTerrain => Grounded && GroundCollider != null && ((1 << GroundCollider.gameObject.layer) & Layers.GroundMask) != 0;
        public Vector3 GroundNormal = Vector3.up;
        public string CrewName => Vessel.Record.evaCrew;
        private double _lastJumpUT = -10;
        private Quaternion _facing = Quaternion.identity;
        private bool _facingInit;
        private float _walkPhase;

        public override string ModuleName => "EVA";

        public override void OnInit()
        {
            // Replace the placeholder collider with a frictionless capsule.
            foreach (var c in Part.GetComponents<Collider>()) Object.Destroy(c);
            var cap = Part.gameObject.AddComponent<CapsuleCollider>();
            cap.radius = 0.3f;
            cap.height = 1.3f;
            cap.direction = 1;
            cap.center = Vector3.zero;
            cap.material = FrictionlessMaterial;
            Part.gameObject.layer = Layers.EVA;
            if (Vessel != null && Vessel.Rb != null)
            {
                Vessel.Rb.constraints = RigidbodyConstraints.FreezeRotation;
                Vessel.Rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            }
        }

        private static PhysicsMaterial _frictionless;
        private static PhysicsMaterial FrictionlessMaterial
        {
            get
            {
                if (_frictionless == null)
                {
                    _frictionless = new PhysicsMaterial("EvaFrictionless")
                    {
                        dynamicFriction = 0, staticFriction = 0, bounciness = 0,
                        frictionCombine = PhysicsMaterialCombine.Minimum, bounceCombine = PhysicsMaterialCombine.Minimum,
                    };
                }
                return _frictionless;
            }
        }

        public double Propellant
        {
            get { var r = Part.GetResource("Monoprop"); return r != null ? r.Amount : 0; }
        }

        public override void OnPreStep(double dt)
        {
            var v = Vessel;
            var rb = v.Rb;
            if (rb == null || rb.isKinematic) return;
            Vector3 g = v.GravityAccel;
            Vector3 up = g.sqrMagnitude > 1e-6f ? -g.normalized : (CameraRotation * Vector3.up);
            float gMag = g.magnitude;

            // Ground probe
            Grounded = false;
            GroundCollider = null;
            Vector3 center = rb.worldCenterOfMass;
            Vector3 groundVel = v.SurfaceFrameVelocityAt(center);
            if (Physics.SphereCast(center, 0.27f, -up, out RaycastHit hit, 0.52f, Layers.GroundMask | (1 << Layers.Parts), QueryTriggerInteraction.Ignore))
            {
                float slope = Vector3.Angle(hit.normal, up);
                if (slope < 60f)
                {
                    Grounded = true;
                    GroundCollider = hit.collider;
                    GroundNormal = hit.normal;
                    if (hit.rigidbody != null) groundVel = hit.rigidbody.GetPointVelocity(hit.point);
                    v.LastGroundContactUT = FlightSim.Instance.UT;
                    v.AddContactFlag();
                }
            }

            // Camera-relative move direction in the local horizontal plane (or camera plane in space)
            Vector3 camFwd = CameraRotation * Vector3.forward;
            Vector3 camRight = CameraRotation * Vector3.right;
            Vector3 moveWorld;
            bool nearSurface = gMag > 0.05f && (Grounded || v.AltitudeAGL < 50);
            if (nearSurface)
            {
                Vector3 f = Vector3.ProjectOnPlane(camFwd, up);
                if (f.sqrMagnitude < 1e-4f) f = Vector3.ProjectOnPlane(CameraRotation * Vector3.up, up);
                f.Normalize();
                Vector3 r = Vector3.Cross(up, f);
                moveWorld = f * MoveInput.y + r * MoveInput.x;
            }
            else moveWorld = camFwd * MoveInput.y + camRight * MoveInput.x;
            if (moveWorld.sqrMagnitude > 1) moveWorld.Normalize();

            float mass = rb.mass;
            if (Grounded && !JetpackOn)
            {
                Vector3 n = GroundNormal;
                Vector3 relVel = rb.linearVelocity - groundVel;
                Vector3 tangential = relVel - Vector3.Dot(relVel, n) * n;
                Vector3 desired = Vector3.ProjectOnPlane(moveWorld, n).normalized * moveWorld.magnitude * (RunInput ? RunSpeed : WalkSpeed);
                Vector3 accel = (desired - tangential) / 0.18f;
                float maxAccel = Mathf.Max(0.3f, gMag * 1.1f);
                if (accel.magnitude > maxAccel) accel = accel.normalized * maxAccel;
                v.AddForceAtPosition(Part, accel * mass, center);
                if (JumpPressed && FlightSim.Instance.UT - _lastJumpUT > 0.35)
                {
                    _lastJumpUT = FlightSim.Instance.UT;
                    rb.AddForce(up * JumpSpeed * mass, ForceMode.Impulse);
                    v.ForceUnlockThisStep = true;
                }
                if (desired.sqrMagnitude > 0.01f) _walkPhase += (float)dt * desired.magnitude * 3f;
            }

            if (JetpackOn)
            {
                Vector3 thrustDir = moveWorld + (nearSurface ? up : CameraRotation * Vector3.up) * VerticalInput;
                if (thrustDir.sqrMagnitude > 1) thrustDir.Normalize();
                if (thrustDir.sqrMagnitude > 1e-4f)
                {
                    double mdot = JetpackThrust * thrustDir.magnitude / (JetpackIsp * MathD.G0);
                    double got = v.Resources.Request(Part, "Monoprop", mdot * dt);
                    float frac = mdot > 0 ? (float)(got / (mdot * dt)) : 0;
                    if (frac > 0.01f) v.AddForceAtPosition(Part, thrustDir * JetpackThrust * frac, center);
                }
            }
            JumpPressed = false;

            // Orientation: upright near surfaces, camera-aligned in free space.
            Quaternion target;
            if (nearSurface)
            {
                Vector3 face = moveWorld.sqrMagnitude > 0.01f ? Vector3.ProjectOnPlane(moveWorld, up) : Vector3.ProjectOnPlane(_facing * Vector3.forward, up);
                if (face.sqrMagnitude < 1e-4f) face = Vector3.ProjectOnPlane(camFwd, up);
                if (face.sqrMagnitude < 1e-4f) face = Vector3.Cross(up, Vector3.right);
                target = Quaternion.LookRotation(face.normalized, up);
            }
            else
            {
                target = Quaternion.LookRotation(camFwd, CameraRotation * Vector3.up);
            }
            if (!_facingInit) { _facing = rb.rotation; _facingInit = true; }
            _facing = Quaternion.RotateTowards(_facing, target, (float)dt * 220f);
            rb.MoveRotation(_facing);
            rb.angularVelocity = Vector3.zero;
        }

        public override void OnRenderUpdate(float dt)
        {
            if (Part.ModelRoot == null) return;
            float bob = Grounded ? Mathf.Abs(Mathf.Sin(_walkPhase)) * 0.04f : 0f;
            Part.ModelRoot.localPosition = new Vector3(0, bob, 0);
        }

        public override void Save(Dictionary<string, string> s)
        {
            s["jetpack"] = B(JetpackOn);
        }

        public override void Load(Dictionary<string, string> s)
        {
            JetpackOn = GetBool(s, "jetpack");
        }

        public override void CollectInfo(List<string> info)
        {
            info.Add($"EVA: {CrewName}  Jetpack {(JetpackOn ? "ON" : "off")} ({Propellant:F2} kg)");
        }
    }

    /// <summary>Planted flag: static surface object with a plaque.</summary>
    public sealed class FlagModule : PartModule
    {
        public string Plaque = "";
        public override string ModuleName => "Flag";
        public override void Save(Dictionary<string, string> s) { s["plaque"] = Plaque ?? ""; }
        public override void Load(Dictionary<string, string> s) { Plaque = GetString(s, "plaque", ""); }
        public override void CollectInfo(List<string> info) { info.Add(Plaque); }
    }

    public static class PartModuleFactory
    {
        public static void AddModules(Part p)
        {
            var d = p.Def;
            void Add(PartModule m) { m.Part = p; p.Modules.Add(m); }
            if (d.command != null) Add(new CommandModule());
            if (d.crew != null) Add(new CrewModule());
            if (d.reactionWheel != null) Add(new ReactionWheelModule());
            if (d.engine != null) Add(new EngineModule());
            if (d.decoupler != null) Add(new DecouplerModule());
            if (d.parachute != null) Add(new ParachuteModule());
            if (d.landingLeg != null) Add(new LandingLegModule());
            if (d.heatShield != null) Add(new HeatShieldModule());
            if (d.rcs != null) Add(new RcsModule());
            if (d.dockingPort != null) Add(new DockingPortModule());
            if (d.fin != null) Add(new FinModule());
            if (d.model?.type == "eva") Add(new EvaModule());
            if (d.model?.type == "flag") Add(new FlagModule());
        }
    }
}
