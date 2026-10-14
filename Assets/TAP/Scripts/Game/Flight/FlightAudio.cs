using System;
using System.Collections.Generic;
using FMOD.Studio;
using FMODUnity;
using TAP.Core;
using TAP.Persistence;
using TAP.Simulation;
using UnityEngine;

namespace TAP.Game
{
    public enum FlightAudioPhase { Standby, Launch, HighAtmosphere, Orbit, Transit, Touchdown, Reentry }

    /// <summary>One shared musical clock and the active vessel's flight sounds.</summary>
    public sealed class FlightAudio : MonoBehaviour
    {
        private static readonly string[] MusicEvents = {
            "event:/Music/Flight", "event:/Music/FarHorizon", "event:/Music/NightDrift", "event:/Music/Starward",
            "event:/Music/BlueSilence", "event:/Music/GravityWell", "event:/Music/DawnPatrol", "event:/Music/AmberSkies",
            "event:/Music/ClockworkVoyage", "event:/Music/Homecoming"
        };
        private static int _nextMusic = (Environment.TickCount & int.MaxValue) % MusicEvents.Length;
        private static readonly string[] Layers = { "Pad", "Strings", "Cello", "Horns", "Celeste", "Pizzicato", "Pulse" };
        private static readonly float[][] Gains = {
            new[] { .20f, .06f, .04f, 0f, .12f, 0f, 0f },
            new[] { .24f, .60f, .66f, .72f, .26f, .74f, .55f },
            new[] { .64f, .56f, .30f, .28f, .50f, .22f, .12f },
            new[] { .70f, .22f, .15f, 0f, .60f, 0f, 0f },
            new[] { .58f, .10f, .12f, 0f, .23f, 0f, 0f },
            new[] { .38f, .64f, .36f, .32f, .46f, .12f, 0f },
            new[] { .24f, .32f, .62f, .20f, .10f, .52f, .72f }
        };
        private const float BarSeconds = 4f * 60f / 72f;
        private FlightSceneController _scene;
        private EventInstance _music, _engine, _wind, _reentry, _jetpack;
        private Bus _masterBus, _musicBus, _effectsBus;
        private Vessel _vessel;
        private FlightAudioPhase _phase, _candidate;
        private readonly float[] _gains = new float[7];
        private readonly float[] _blendFrom = new float[7];
        private float _candidateSince, _blendStart, _duckUntil, _lastRcs = -10, _lastImpact = -10, _lastExplosion = -10;
        private bool _ready, _running, _chuteOpen, _contact;
        private readonly Dictionary<LandingLegModule, bool> _legs = new Dictionary<LandingLegModule, bool>();
        private readonly Dictionary<DockingPortModule, DockingPortModule.PortState> _ports = new Dictionary<DockingPortModule, DockingPortModule.PortState>();
        private float _lastMechanism = -10, _lastStep = -10;
        private int _stepVariant;
        private int _musicPosition;
        private float _duckVolume = 1;
        private bool _rotateMusic = true;

        public static void Create(FlightSceneController scene)
        {
            var audio = scene.gameObject.AddComponent<FlightAudio>();
            audio._scene = scene;
            scene.Camera.gameObject.AddComponent<StudioListener>();
        }

        private void Start()
        {
            try
            {
                _masterBus = RuntimeManager.GetBus("bus:/");
                _musicBus = RuntimeManager.GetBus("bus:/Music");
                _effectsBus = RuntimeManager.GetBus("bus:/Effects");
                _music = CreateMusic();
                _engine = RuntimeManager.CreateInstance("event:/Vessel/Engine");
                _wind = RuntimeManager.CreateInstance("event:/Vessel/Wind");
                _reentry = RuntimeManager.CreateInstance("event:/Vessel/Reentry");
                _jetpack = RuntimeManager.CreateInstance("event:/Vessel/Jetpack_Loop");
                _music.setVolume(0);
                _engine.setVolume(0); _wind.setVolume(0); _reentry.setVolume(0); _jetpack.setVolume(0);
                _phase = _candidate = FlightAudioPhase.Standby;
                Array.Copy(Gains[(int)_phase], _gains, 7);
                for (int i = 0; i < 7; i++) _music.setParameterByName(Layers[i], _gains[i]);
                _music.start(); _engine.start(); _wind.start(); _reentry.start(); _jetpack.start();
                GameEvents.Raised += OnEvent;
                _scene.Notification += OnNotification;
                _ready = true;
            }
            catch (Exception ex)
            {
                Debug.LogError("[Flight audio] FMOD banks could not be loaded: " + ex.Message);
                enabled = false;
            }
        }

        public static FlightAudioPhase SelectPhase(Situation situation, bool inAir, float atmosphereFraction,
            double agl, double verticalSpeed, float heating, bool stellar, FlightAudioPhase previous = FlightAudioPhase.Standby)
        {
            if (situation == Situation.Prelaunch) return FlightAudioPhase.Standby;
            if (inAir && verticalSpeed < -5 && heating > (previous == FlightAudioPhase.Reentry ? .02f : .05f)) return FlightAudioPhase.Reentry;
            if (situation == Situation.Landed || situation == Situation.Splashed) return FlightAudioPhase.Touchdown;
            if (!stellar && agl < 1500 && verticalSpeed < -.5) return FlightAudioPhase.Touchdown;
            if (inAir) return atmosphereFraction > (previous == FlightAudioPhase.HighAtmosphere ? .30f : .35f) || verticalSpeed < 0
                ? FlightAudioPhase.HighAtmosphere : FlightAudioPhase.Launch;
            if (!stellar && situation == Situation.Orbiting) return FlightAudioPhase.Orbit;
            if (!stellar && agl < 10000 && verticalSpeed > 1) return FlightAudioPhase.Launch;
            return FlightAudioPhase.Transit;
        }

        private void LateUpdate()
        {
            if (!_ready || _scene.Sim == null) return;
            var sim = _scene.Sim;
            var v = sim.ActiveVessel;
            var settings = GameSettings.Current.audio;
            // AudioOptions.Volume includes master; FMOD has its own master bus, so apply each slider once.
            _masterBus.setVolume(settings.master);
            _musicBus.setVolume(settings.music);
            _effectsBus.setVolume(settings.effects);
            _effectsBus.setPaused(sim.Paused);
            _effectsBus.setMute(v == null || sim.Warp.OnRails);
            if (v == null) { UpdateMusic(FlightAudioPhase.Standby, 0, sim.Paused); return; }

            double thrust = 0, rated = 0, heat = 0;
            float rcs = 0;
            bool chute = false;
            EvaModule eva = null;
            bool changedVessel = _vessel != v;
            if (changedVessel) { _legs.Clear(); _ports.Clear(); }
            foreach (var part in v.Parts)
            {
                var engine = part.GetModule<EngineModule>();
                if (engine != null && engine.IsRunning)
                {
                    thrust += engine.CurrentThrust;
                    rated += engine.Def.thrustVac;
                }
                var jets = part.GetModule<RcsModule>();
                if (jets?.NozzleThrottle != null)
                    foreach (float value in jets.NozzleThrottle) rcs = Mathf.Max(rcs, value);
                var parachute = part.GetModule<ParachuteModule>();
                chute |= parachute != null && parachute.IsOpen;
                eva = part.GetModule<EvaModule>() ?? eva;
                var leg = part.GetModule<LandingLegModule>();
                if (leg != null)
                {
                    if (!leg.Broken && _legs.TryGetValue(leg, out bool deployed) && deployed != leg.Deployed)
                        Mechanism("Leg_Servo_0" + UnityEngine.Random.Range(1, 3));
                    _legs[leg] = leg.Deployed;
                }
                var port = part.GetModule<DockingPortModule>();
                if (port != null)
                {
                    if (_ports.TryGetValue(port, out var state) && state == DockingPortModule.PortState.Docked
                        && port.State == DockingPortModule.PortState.Cooldown)
                        Mechanism("Undock_Release_0" + UnityEngine.Random.Range(1, 3));
                    _ports[port] = port.State;
                }
                double area = Math.Max(1, Math.PI * part.Def.diameter * part.Def.height);
                heat = Math.Max(heat, part.HeatFlux / area);
            }
            float heating = Mathf.Clamp01((float)(heat / 150000));
            float output = sim.Warp.OnRails ? 0 : Mathf.Clamp01((float)(thrust / Math.Max(1, rated)));
            float air = Mathf.Clamp01(Mathf.Sqrt((float)(v.StaticPressure / 101325)));
            bool contact = v.GroundContact || v.Splashed;
            double atmosphereHeight = v.MainBody.Atmosphere?.Height ?? 1;
            var next = SelectPhase(v.Situation, v.InAtmosphere, (float)(v.Altitude / atmosphereHeight),
                v.AltitudeAGL, v.VerticalSpeed, heating, v.MainBody.IsStar, _phase);
            if (changedVessel)
            {
                _vessel = v;
                _running = output > .001f; _chuteOpen = chute; _contact = contact;
                _phase = _candidate = next;
                Array.Copy(_gains, _blendFrom, 7);
                _blendStart = Time.unscaledTime;
            }

            UpdateMusic(next, output, sim.Paused);
            if (sim.Paused) return;
            _engine.setParameterByName("EngineOutput", output);
            _engine.setParameterByName("AirPresence", air);
            float distance = _scene.Map.Active ? 25 : _scene.Camera.Distance;
            _engine.setVolume(1f / (1f + distance / 100f));
            _wind.setVolume(sim.Warp.OnRails ? 0 : Mathf.Clamp01((float)(v.DynamicPressure / 40000)) * .55f);
            _reentry.setVolume(sim.Warp.OnRails ? 0 : heating * .8f);
            float jet = eva != null && eva.JetpackOn && eva.Propellant > 0
                ? Mathf.Clamp01(new Vector3(eva.MoveInput.x, eva.MoveInput.y, eva.VerticalInput).magnitude) : 0;
            _jetpack.setVolume(sim.Warp.OnRails ? 0 : jet * .5f);
            if (sim.Warp.OnRails) return;

            if (eva != null && eva.Grounded && !eva.JetpackOn && eva.MoveInput.sqrMagnitude > .02f
                && v.SurfaceSpeed > .15 && Time.unscaledTime - _lastStep > (eva.RunInput ? .32f : .55f))
            {
                _stepVariant = (_stepVariant + 1) % 3;
                Shot("Step_" + (eva.OnTerrain ? "Surface" : "Metal") + "_0" + (_stepVariant + 1), .65f);
                _lastStep = Time.unscaledTime;
            }

            if (!changedVessel)
            {
                bool running = output > .001f;
                if (running && !_running) Shot("Ignition", 1);
                if (!running && _running) Shot("Shutdown", .8f);
                if (chute && !_chuteOpen) Shot("Parachute", .9f);
                if (contact && !_contact && Time.unscaledTime - _lastImpact > 2 && v.Situation != Situation.Prelaunch)
                {
                    Shot("Touchdown", .8f); _lastImpact = Time.unscaledTime;
                }
                _running = running; _chuteOpen = chute; _contact = contact;
            }
            if (rcs > .01f && Time.unscaledTime - _lastRcs > .24f)
            {
                Shot("RCS", rcs * .4f); _lastRcs = Time.unscaledTime;
            }
        }

        private void UpdateMusic(FlightAudioPhase next, float output, bool paused)
        {
            float now = Time.unscaledTime;
            _music.getTimelinePosition(out int milliseconds);
            if (_rotateMusic && milliseconds < _musicPosition)
            {
                try
                {
                    var upcoming = CreateMusic();
                    for (int i = 0; i < 7; i++) upcoming.setParameterByName(Layers[i], _gains[i]);
                    upcoming.setVolume(0);
                    upcoming.start();
                    _music.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
                    _music.release();
                    _music = upcoming;
                    milliseconds = 0;
                }
                catch (Exception ex)
                {
                    // Keep the current score if an authoring bank is incomplete during live editing.
                    Debug.LogError("[Flight audio] Music rotation disabled: " + ex.Message);
                    _rotateMusic = false;
                }
            }
            _musicPosition = milliseconds;
            if (!paused)
            {
                if (_candidate != next) { _candidate = next; _candidateSince = now; }
                bool barStart = milliseconds / 1000f % BarSeconds < Time.unscaledDeltaTime + .02f;
                if (next != _phase && now - _candidateSince >= 1.5f && barStart)
                {
                    _phase = next;
                    Array.Copy(_gains, _blendFrom, 7);
                    _blendStart = now;
                }
            }
            float blend = Mathf.SmoothStep(0, 1, Mathf.Clamp01((now - _blendStart) / (2 * BarSeconds)));
            for (int i = 0; i < 7; i++)
            {
                _gains[i] = Mathf.Lerp(_blendFrom[i], Gains[(int)_phase][i], blend);
                _music.setParameterByName(Layers[i], _gains[i]);
            }
            float duck = now < _duckUntil ? .45f : Mathf.Lerp(1, .65f, output);
            _duckVolume = Mathf.Lerp(_duckVolume, duck, 1 - Mathf.Exp(-Time.unscaledDeltaTime * 4));
            _music.setVolume(_duckVolume * (_rotateMusic ? MusicEdgeGain(milliseconds) : 1));
        }

        private static EventInstance CreateMusic()
        {
            var music = RuntimeManager.CreateInstance(MusicEvents[_nextMusic]);
            _nextMusic = (_nextMusic + 1) % MusicEvents.Length;
            return music;
        }

        /// <summary>One bar of silence-to-music at each edge lets scores change key without overlapping phrases.</summary>
        public static float MusicEdgeGain(int milliseconds)
        {
            float seconds = milliseconds / 1000f;
            return Mathf.SmoothStep(0, 1, Mathf.Min(seconds, 32 * BarSeconds - seconds) / BarSeconds);
        }

        private void OnNotification(string text, bool important)
        {
            if (important) _duckUntil = Time.unscaledTime + 2.5f;
        }

        private void Mechanism(string name)
        {
            if (_scene.Sim.Paused || _scene.Sim.Warp.OnRails || Time.unscaledTime - _lastMechanism < .25f) return;
            Shot(name, .7f);
            _lastMechanism = Time.unscaledTime;
        }

        private void OnEvent(GameEvent e)
        {
            if (!_ready || _scene.Sim == null || _scene.Sim.Paused || _scene.Sim.Warp.OnRails) return;
            string activeId = _scene.Sim.ActiveVessel?.Id ?? _vessel?.Id;
            if (e.VesselId != activeId || _vessel == null) return;
            if (e.Kind == GameEventKind.Docked) Shot("Dock_Latch_0" + UnityEngine.Random.Range(1, 3), .85f);
            if (e.Kind == GameEventKind.EvaStarted) Shot("Hatch_Open", .7f);
            if (e.Kind == GameEventKind.EvaEnded) Shot("Hatch_Close", .7f);
            if (e.Kind == GameEventKind.Staged)
            {
                Shot("Stage0" + UnityEngine.Random.Range(1, 4), 1);
                _duckUntil = Time.unscaledTime + 2;
                _legs.Clear(); _ports.Clear();
            }
            else if (e.Kind == GameEventKind.PartDestroyed && Time.unscaledTime - _lastExplosion > .15f)
            {
                Shot("Explosion", 1);
                _lastExplosion = Time.unscaledTime;
                _duckUntil = Time.unscaledTime + 2;
                _legs.Clear(); _ports.Clear();
            }
        }

        private void Shot(string name, float volume)
        {
            var instance = RuntimeManager.CreateInstance("event:/Vessel/" + name);
            var source = _scene.Sim.ActiveVessel ?? _vessel;
            instance.set3DAttributes(RuntimeUtils.To3DAttributes(source.transform.position));
            instance.setVolume(volume);
            instance.start();
            instance.release();
        }

        private void OnDestroy()
        {
            GameEvents.Raised -= OnEvent;
            if (_scene != null) _scene.Notification -= OnNotification;
            foreach (var instance in new[] { _music, _engine, _wind, _reentry, _jetpack })
            {
                if (!instance.isValid()) continue;
                instance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
                instance.release();
            }
            if (_effectsBus.isValid())
            {
                _effectsBus.stopAllEvents(FMOD.Studio.STOP_MODE.IMMEDIATE);
                _effectsBus.setPaused(false); _effectsBus.setMute(false);
            }
        }
    }
}
