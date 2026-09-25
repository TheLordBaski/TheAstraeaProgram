using System;
using System.Collections.Generic;
using UnityEngine;

namespace TAP.Parts
{
    public enum ResourceFlow
    {
        /// <summary>Only from the part itself (solid fuel, ablator).</summary>
        Part,
        /// <summary>From all parts reachable without crossing a crossfeed-blocking part (liquid propellant).</summary>
        Stack,
        /// <summary>From anywhere in the vessel (monopropellant, electric charge).</summary>
        Vessel,
    }

    [Serializable]
    public class ResourceDefinition
    {
        public string id;
        public string name;
        public string unit = "kg";
        /// <summary>Mass per unit (kg). Electric charge has zero density.</summary>
        public double density = 1.0;
        public ResourceFlow flow = ResourceFlow.Stack;
        public float[] color = { 1, 1, 1 };
        public Color Color => new Color(color[0], color[1], color[2]);
    }

    [Serializable]
    public class AttachNodeDefinition
    {
        public string id;
        public float[] pos = { 0, 0, 0 };
        public float[] dir = { 0, 1, 0 };
        /// <summary>0 = 0.625 m, 1 = 1.25 m, 2 = 2.5 m.</summary>
        public int size = 1;
        public Vector3 Position => new Vector3(pos[0], pos[1], pos[2]);
        public Vector3 Direction => new Vector3(dir[0], dir[1], dir[2]).normalized;
    }

    [Serializable]
    public class SurfaceAttachDefinition
    {
        /// <summary>This part may be attached to the surface of other parts.</summary>
        public bool allowed;
        /// <summary>Other parts may be surface-attached onto this part.</summary>
        public bool onto = true;
        /// <summary>Attachment point in part space (where the part touches the parent surface).</summary>
        public float[] pos = { 0, 0, 0 };
        /// <summary>Outward direction from the parent surface, in part space (points away from parent).</summary>
        public float[] dir = { 1, 0, 0 };
        public Vector3 Position => new Vector3(pos[0], pos[1], pos[2]);
        public Vector3 Direction => new Vector3(dir[0], dir[1], dir[2]).normalized;
    }

    [Serializable]
    public class ResourceAmount
    {
        public string id;
        public double amount;
        public double max = -1;
        public double Max => max < 0 ? amount : max;
    }

    [Serializable]
    public class DragDefinition
    {
        /// <summary>Drag coefficient of the +Y face when exposed to flow.</summary>
        public float cdTop = 0.8f;
        /// <summary>Drag coefficient of the -Y face when exposed.</summary>
        public float cdBottom = 0.8f;
        /// <summary>Cross-flow drag coefficient for the side silhouette.</summary>
        public float cdSide = 1.0f;
        /// <summary>Extra lift-like normal force factor (pointy noses are destabilising).</summary>
        public float bodyLift = 0.0f;
    }

    [Serializable]
    public class ModelDefinition
    {
        /// <summary>Procedural generator id (tank, capsule, engine, srb, decoupler, ...) or "asset".</summary>
        public string type = "tank";
        public string asset;
        public float diameter = 1.25f;
        public float topDiameter = -1;
        public float bottomDiameter = -1;
        public float height = 1f;
        public string style = "white";
        // Engine specifics
        public float bellTop = 0.35f;
        public float bellBottom = 0.9f;
        public float bellLength = 0.9f;
        public float mountHeight = 0.4f;
        public int count = 1;
        public float TopD => topDiameter > 0 ? topDiameter : diameter;
        public float BottomD => bottomDiameter > 0 ? bottomDiameter : diameter;
    }

    [Serializable]
    public class EngineDefinition
    {
        /// <summary>"liquid" (throttleable, restartable) or "solid" (burns to depletion).</summary>
        public string type = "liquid";
        public double thrustVac;     // N
        public double ispVac;        // s
        public double ispAsl;        // s
        public float gimbalRange;    // degrees
        public float minThrottle = 0f;
        /// <summary>Throttle response (fraction per second); 0 = instant.</summary>
        public float throttleResponse = 3f;
        public string propellant = "LiquidFuel";
        /// <summary>Electric charge generated at full thrust (EC/s).</summary>
        public double alternator;
        /// <summary>Heat generated at full thrust into the engine (W).</summary>
        public double heatProduction = 0;
        /// <summary>Thrust limiter (percent) default for SRBs.</summary>
        public float thrustLimit = 100f;
        /// <summary>Local thrust direction (exhaust goes the opposite way). Default: thrust along +Y.</summary>
        public float[] thrustDir = { 0, 1, 0 };
        /// <summary>Nozzle exit position (part space), used for effects and force application.</summary>
        public float[] nozzlePos = { 0, -0.5f, 0 };
        public float exhaustScale = 1f;
        public float[] exhaustColor = { 1f, 0.62f, 0.25f };
        public Vector3 ThrustDirection => new Vector3(thrustDir[0], thrustDir[1], thrustDir[2]).normalized;
        public Vector3 NozzlePosition => new Vector3(nozzlePos[0], nozzlePos[1], nozzlePos[2]);
    }

    [Serializable]
    public class DecouplerDefinition
    {
        /// <summary>Separation impulse (N*s) applied equally and oppositely.</summary>
        public double ejectionImpulse = 1500;
        /// <summary>"stack" (serial) or "radial" (parallel booster).</summary>
        public string kind = "stack";
        /// <summary>For stack decouplers: which node's connection is severed (the part stays with the other side).</summary>
        public string explosiveNode = "top";
    }

    [Serializable]
    public class ParachuteDefinition
    {
        /// <summary>Cd*A when semi-deployed (reefed), m^2.</summary>
        public double semiDeployedArea = 8;
        /// <summary>Cd*A when fully deployed, m^2.</summary>
        public double deployedArea = 500;
        /// <summary>Minimum static pressure to begin semi-deployment (Pa).</summary>
        public double minPressure = 4000;
        /// <summary>Altitude above terrain for full deployment (m).</summary>
        public double deployAltitude = 1000;
        /// <summary>Time to go from stowed to semi-deployed (s).</summary>
        public double semiDeployTime = 1.0;
        /// <summary>Time to fully open (s).</summary>
        public double deployTime = 4.0;
        /// <summary>Maximum canopy load before tearing (N).</summary>
        public double maxLoad = 90000;
        /// <summary>Canopy temperature limit (K).</summary>
        public double maxCanopyTemp = 700;
        public float canopyDiameter = 12f;
        public float[] canopyColor = { 0.95f, 0.45f, 0.1f };
    }

    [Serializable]
    public class LandingLegDefinition
    {
        /// <summary>Leg length from mount to foot when deployed (m).</summary>
        public float length = 1.6f;
        /// <summary>Outward splay of the leg (degrees from vessel axis).</summary>
        public float splayDeg = 25f;
        /// <summary>Suspension travel (m).</summary>
        public float travel = 0.35f;
        /// <summary>Spring constant (N/m).</summary>
        public float spring = 60000f;
        /// <summary>Damping (N*s/m).</summary>
        public float damper = 9000f;
        /// <summary>Max compression speed before the leg breaks (m/s).</summary>
        public float impactTolerance = 12f;
        /// <summary>Max sustained load before collapse (N).</summary>
        public float maxLoad = 120000f;
        public float footRadius = 0.18f;
        public float friction = 0.9f;
        public bool startDeployed = false;
    }

    [Serializable]
    public class HeatShieldDefinition
    {
        /// <summary>Temperature at which ablation starts (K).</summary>
        public double ablationStartTemp = 550;
        /// <summary>Heat absorbed per kg ablated (J/kg).</summary>
        public double ablationHeat = 1.6e7;
        /// <summary>Maximum ablation rate at full effect (kg/s).</summary>
        public double maxAblationRate = 0.8;
        /// <summary>Conductance between shield and the part it protects (W/K), low = insulating.</summary>
        public double insulation = 2.0;
    }

    [Serializable]
    public class ReactionWheelDefinition
    {
        /// <summary>Max torque per axis (N*m).</summary>
        public double torque = 5000;
        /// <summary>Electric charge per second at full torque on all axes.</summary>
        public double ecPerSecond = 0.6;
    }

    [Serializable]
    public class RcsDefinition
    {
        /// <summary>Thrust per nozzle (N).</summary>
        public double thrust = 1000;
        public double ispVac = 240;
        public double ispAsl = 100;
        /// <summary>Nozzle positions and outward (exhaust) directions in part space.</summary>
        public float[][] nozzles;
        public string propellant = "Monoprop";
    }

    [Serializable]
    public class CrewDefinition
    {
        public int seats = 1;
        /// <summary>Hatch position (part space) — EVA spawn and boarding point.</summary>
        public float[] hatchPos = { 0, 0, 0.6f };
        public float[] hatchNormal = { 0, 0, 1 };
        public Vector3 HatchPosition => new Vector3(hatchPos[0], hatchPos[1], hatchPos[2]);
        public Vector3 HatchNormal => new Vector3(hatchNormal[0], hatchNormal[1], hatchNormal[2]).normalized;
    }

    [Serializable]
    public class CommandDefinition
    {
        /// <summary>Requires crew to control (false = autonomous probe core).</summary>
        public bool requiresCrew = true;
        public bool sas = true;
        /// <summary>EC per second consumed while the vessel is controlled from this part.</summary>
        public double ecPerSecond = 0.02;
    }

    [Serializable]
    public class DockingPortDefinition
    {
        public string nodeId = "top";
        /// <summary>Capture range (m) and max relative speed (m/s) for docking.</summary>
        public float captureRange = 0.6f;
        public float captureSpeed = 1.0f;
        public float captureAngleDeg = 15f;
        public double undockImpulse = 150;
    }

    [Serializable]
    public class FinDefinition
    {
        /// <summary>Planform area (m^2).</summary>
        public float area = 0.5f;
        /// <summary>Lift slope per radian.</summary>
        public float liftSlope = 3.5f;
        /// <summary>Normal direction of the fin plate (part space).</summary>
        public float[] normal = { 0, 0, 1 };
        public float stallDeg = 25f;
        public Vector3 Normal => new Vector3(normal[0], normal[1], normal[2]).normalized;
    }

    [Serializable]
    public class PartDefinition
    {
        public string id;
        public string title;
        public string manufacturer = "Astraea Works";
        public string category = "Structural";
        public string description;
        public double dryMass;
        /// <summary>Bounding cylinder used for aero/thermal areas and inertia: diameter, height (m).</summary>
        public float diameter = 1.25f;
        public float height = 1f;
        public ModelDefinition model = new ModelDefinition();
        public List<AttachNodeDefinition> nodes = new List<AttachNodeDefinition>();
        public SurfaceAttachDefinition surfaceAttach = new SurfaceAttachDefinition();
        public List<ResourceAmount> resources = new List<ResourceAmount>();

        /// <summary>Internal temperature limit (K).</summary>
        public double maxTemp = 1300;
        /// <summary>Skin temperature limit (K).</summary>
        public double skinMaxTemp = 1500;
        /// <summary>Specific heat of the part (J/kg/K).</summary>
        public double specificHeat = 800;
        /// <summary>Skin areal mass (kg/m^2) — thermal inertia of the skin layer.</summary>
        public double skinMassPerArea = 8;
        public double emissivity = 0.4;
        /// <summary>Impact speed the part survives (m/s).</summary>
        public double impactTolerance = 8;
        /// <summary>Connection strength to its parent: max force (N) and torque (N*m).</summary>
        public double breakingForce = 120000;
        public double breakingTorque = 120000;
        /// <summary>Whether propellant can flow through this part (decouplers block).</summary>
        public bool crossfeed = true;
        public DragDefinition drag = new DragDefinition();

        public EngineDefinition engine;
        public DecouplerDefinition decoupler;
        public ParachuteDefinition parachute;
        public LandingLegDefinition landingLeg;
        public HeatShieldDefinition heatShield;
        public ReactionWheelDefinition reactionWheel;
        public RcsDefinition rcs;
        public CrewDefinition crew;
        public CommandDefinition command;
        public DockingPortDefinition dockingPort;
        public FinDefinition fin;

        /// <summary>True if the part participates in staging by default.</summary>
        public bool IsStageable => engine != null || decoupler != null || parachute != null;

        public AttachNodeDefinition FindNode(string nodeId)
        {
            foreach (var n in nodes) if (n.id == nodeId) return n;
            return null;
        }

        public double ResourceMass(PartDatabase db)
        {
            double m = 0;
            foreach (var r in resources)
            {
                var rd = db.GetResource(r.id);
                if (rd != null) m += r.amount * rd.density;
            }
            return m;
        }

        public double WetMass(PartDatabase db) => dryMass + ResourceMass(db);
    }

    [Serializable]
    public class PartCatalog
    {
        public List<ResourceDefinition> resources = new List<ResourceDefinition>();
        public List<string> categories = new List<string>();
        public List<PartDefinition> parts = new List<PartDefinition>();
    }
}
