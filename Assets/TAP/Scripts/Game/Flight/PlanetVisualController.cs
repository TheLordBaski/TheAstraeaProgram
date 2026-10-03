using System;
using System.Collections.Generic;
using TAP.Core;
using TAP.Simulation;
using UnityEngine;

namespace TAP.Game
{
    /// <summary>Adapts double-precision simulation coordinates to explicit physical render views.</summary>
    [DefaultExecutionOrder(1100)]
    public sealed class PlanetVisualController : MonoBehaviour
    {
        public static PlanetVisualController Instance { get; private set; }
        public static event Action Destroyed;
        public FlightSceneController Scene;
        public PlanetVisualLibrary Library;
        public readonly Dictionary<CelestialBody, PlanetVisualProfile> Profiles = new Dictionary<CelestialBody, PlanetVisualProfile>();
        readonly Dictionary<CelestialBody, Material> local = new Dictionary<CelestialBody, Material>();
        readonly Dictionary<CelestialBody, Material> map = new Dictionary<CelestialBody, Material>();
        readonly List<UnityEngine.Object> owned = new List<UnityEngine.Object>();
        MaterialPropertyBlock plumeProperties;
        Action<MeshRenderer> bindPlume;
        // Every preset uses the scattering sky; Low only integrates it with fewer samples.
        public bool Scattering => true;
        public bool HasScattering(CelestialBody body) => Scattering && Profiles.TryGetValue(body,out var p) && p.Transmittance!=null && p.MultiScattering!=null;
        public static PlanetVisualController Create(FlightSceneController scene)
        {
            var v = new GameObject("Planet visuals").AddComponent<PlanetVisualController>(); v.Scene=scene; Instance=v;
            v.plumeProperties=new MaterialPropertyBlock();
            v.bindPlume=v.BindPlume;
            v.Library=Resources.Load<PlanetVisualLibrary>("PlanetVisuals/Library");
            foreach(var b in scene.Sim.System.Bodies) {
                if(b.IsStar) continue;
                var p=Resources.Load<PlanetVisualProfile>("PlanetVisuals/"+b.Id);
                if(p!=null && ((!string.IsNullOrEmpty(p.TerrainHash) && p.TerrainHash!=TerrainFingerprint.Hash(b.Def.terrain))
                    || (!string.IsNullOrEmpty(p.SurfaceBakeKey) && p.SurfaceBakeKey!=p.ExpectedSurfaceKey(b)))) {
                    // An authoring edit must never leave old geography painted onto the new terrain.
                    Debug.LogWarning("Planet visual bake is stale for "+b.Id+"; using generated terrain colours. Re-bake with TAP > Planet visuals > Bake all.");
                    p=null;
                }
                if(p!=null) {
                    bool weather=b.HasAtmosphere && p.Weather!=null && !string.IsNullOrEmpty(p.WeatherBakeKey) && p.WeatherBakeKey!=p.ExpectedWeatherKey();
                    bool atmosphere=b.HasAtmosphere && (p.Transmittance==null || p.MultiScattering==null || p.AtmosphereBakeKey!=p.ExpectedAtmosphereKey(b));
                    if(weather || atmosphere) {
                        p=Instantiate(p);v.owned.Add(p);
                        if(weather) {p.Weather=null;p.WeatherNormal=null;Debug.LogWarning("Planet weather bake is stale for "+b.Id+"; re-bake with TAP > Planet visuals > Bake weather only.");}
                        if(atmosphere) {p.Transmittance=null;p.MultiScattering=null;}
                    }
                }
                if(p==null) { p=PlanetVisualProfile.Fallback(b); v.owned.Add(p); }
                // A stale or missing sky bake is cheap to rebuild here (a few tens of milliseconds per body).
                if(b.HasAtmosphere && (p.Transmittance==null || p.MultiScattering==null) && p.GenerateSkyTables(b)) { v.owned.Add(p.Transmittance);v.owned.Add(p.MultiScattering); }
                v.Profiles.Add(b,p);
                var mat=new Material(scene.Planets.TerrainMaterial) { name="Surface "+b.Id }; v.owned.Add(mat); v.local[b]=mat;
                scene.Planets.Terrains[b].SetVisualMaterial(mat); v.Bind(mat,p,1,false);
                foreach(var fb in scene.FarView.Bodies) if(fb.Body==b) v.Bind(fb.Material,p,(float)ScaledSpace.Scale,false);
                if(p.Surface!=null) {
                    var mm=new Material(mat) { name="Map surface "+b.Id }; v.map[b]=mm;
                    v.Bind(mm,p,(float)MapView.Scale,true); scene.Map.SetVisualMaterial(b,mm,p.Surface);
                }
            }
            v.gameObject.AddComponent<PlanetSurfaceScatter>().Visuals=v;
            return v;
        }
        void Bind(Material m,PlanetVisualProfile p,float scale,bool isMap)
        {
            m.SetFloat("_DistanceScale",scale); m.SetFloat("_SurfaceEnabled",p.Surface!=null?1:0); m.SetFloat("_MapMode",isMap?1:0);
            m.SetTexture("_BaseMap",p.Surface); m.SetTexture("_SurfaceNormal",p.SurfaceNormal); m.SetTexture("_SurfaceMask",p.SurfaceMask);
            m.SetTexture("_Weather",p.Weather);
            if(Library!=null) { m.SetTexture("_GroundAlbedo",Library.Albedo);m.SetTexture("_GroundNormal",Library.Normal);m.SetTexture("_GroundMaterial",Library.Material);m.SetTexture("_SatelliteDetail",Library.SatelliteDetail); }
            m.SetFloat("_SkyAmbient",0); m.SetColor("_SpaceAmbient",new Color(.018f,.022f,.032f));
        }
        void UpdateMaterial(Material m,CelestialBody b,Vector3 center,double ut,bool isMap)
        {
            var p=Profiles[b];
            m.SetVector("_BodyCenter",center); m.SetMatrix("_WorldToBody",Matrix4x4.Rotate(b.RotationAtUT(ut).Inverse().ToQuaternion()));
            m.SetFloat("_Radius",(float)b.Radius);
            m.SetFloat("_DetailStrength",isMap?0:PlanetGraphics.Quality==PlanetQuality.Low?.25f:p.GroundDetail);
            m.SetVector("_CloudParams",new Vector4(p.CloudBase,p.CloudCoverage,p.CloudShadow,(float)PlanetVisualMath.WindOffset(ut,p.WindSpeed,2*Math.PI*b.Radius)));
            m.SetFloat("_CloudEnabled",PlanetGraphics.Clouds!=PlanetCloudMode.Off && (!isMap || !MapView.ShowBiomes) && p.Weather!=null?1:0);
            m.SetFloat("_OceanTime",(float)(PlanetVisualMath.WindOffset(ut,1,3600)*Math.PI*2));
            m.SetFloat("_OceanRoughness",p.OceanRoughness);
            // ShaderLab Color properties convert authored sRGB on upload in a Linear project.
            m.SetFloat("_SurfaceSaturation",p.SurfaceSaturation);m.SetColor("_SurfaceTint",p.SurfaceTint);
            m.SetFloat("_SurfaceNormalStrength",p.SurfaceNormalStrength);
            m.SetVector("_RegionalDetail",new Vector4(PlanetGraphics.Quality>=PlanetQuality.High?p.RegionalDetail:0,p.RegionalDetailScale,0,0));
            m.SetColor("_OceanColour",p.OceanColour);m.SetColor("_OceanShallow",p.OceanShallowColour);m.SetFloat("_OceanColourBlend",p.OceanColourBlend);
            // Three triplanar octaves that only run where they are resolved: cheap enough for every preset.
            m.SetFloat("_SatelliteStrength",Library!=null && Library.SatelliteDetail!=null?p.SatelliteDetail:0);
            m.SetFloat("_WaterSpecular",1);
            m.SetFloat("_OrbitalNear",p.SatelliteAlbedo && p.Surface!=null?1:0);
            m.SetFloat("_TextureBias",PlanetGraphics.TextureLimit);
            // Body lighting follows its own phase, independent of the vessel's horizon/eclipse.
            Vector3 d=(Vector3)Scene.Sim.System.SunDirectionFrom(b,Vector3d.zero,ut);
            m.SetVector("_BodySunDir",new Vector4(d.x,d.y,d.z,1)); m.SetVector("_BodySunColor",Scene.Sky.SunColor);
            m.SetFloat("_SkyAmbient",isMap || m.GetFloat("_DistanceScale")>1?0:(float)Scene.Sky.AtmosphereFactor);
            m.SetColor("_HazeColor",Color.black);m.SetFloat("_Transmittance",1);
        }
        void LateUpdate()
        {
            var sim=Scene.Sim; double ut=sim.RenderUT;
            var origin=sim.Frame.RenderOrigin(sim.RenderAlpha);
            foreach(var kv in local) UpdateMaterial(kv.Value,kv.Key,(Vector3)(sim.Frame.BodyPosition(kv.Key,ut)-origin),ut,false);
            foreach(var fb in Scene.FarView.Bodies) {
                if(fb.Body.IsStar) continue;
                UpdateMaterial(fb.Material,fb.Body,fb.Root.position,ut,false);
                if(HasScattering(fb.Body) && fb.Shell!=null) fb.Shell.Hide();
            }
            foreach(var shell in Scene.Planets.Atmospheres) if(HasScattering(shell.Body)) shell.Hide();
            if(HasScattering(sim.Frame.Body)) {
                if(Scene.Sky.SkyMaterial!=null) {
                    Scene.Sky.SkyMaterial.SetFloat("_Atmosphere",0);
                    Scene.Sky.SkyMaterial.SetFloat("_StarBrightness",1.2f*Scene.Sky.StarVisibility);
                }
                RenderSettings.fog=false;
                var body=sim.Frame.Body;var p=Profiles[body];
                var lut=p.Transmittance;
                var camera=origin+(Vector3d)Scene.Camera.Cam.transform.position;
                double top=body.Radius+body.Atmosphere.Height;
                if(!Scene.Map.Active && lut.isReadable && camera.magnitude<top) {
                    var dir=sim.System.SunDirectionFrom(body,camera,ut);
                    double r=Math.Max(body.Radius,camera.magnitude),mu=Vector3d.Dot(camera.normalized,dir);
                    var uv=AtmosphereLuts.TransmittanceUv(r,mu,body.Radius,top);
                    Color trans=lut.GetPixelBilinear(uv.x,uv.y)*(float)AtmosphereLuts.SunVisibility(r,mu,body.Radius);
                    float strength=Mathf.Max(trans.r,Mathf.Max(trans.g,trans.b));
                    Scene.Sky.Sun.intensity=Scene.Sky.SunIntensity*strength;
                    Scene.Sky.Sun.color=(Scene.Sky.SunColor/Scene.Sky.SunIntensity*trans/Mathf.Max(strength,.001f)).gamma;
                    Scene.Sky.SunTint=trans.gamma;
                }
            }
            foreach(var kv in map) {
                var go=Scene.Map.BodyObject(kv.Key); UpdateMaterial(kv.Value,kv.Key,go.transform.position,sim.UT,true);
                var halo=go.transform.Find("Halo"); if(halo!=null) halo.gameObject.SetActive(!HasScattering(kv.Key) && !MapView.ShowBiomes);
            }
            Scene.Effects.VisitPlumeRenderers(bindPlume);
        }
        void BindPlume(MeshRenderer renderer)
        {
            if(renderer==null || !renderer.enabled) return;
            renderer.GetPropertyBlock(plumeProperties);
            var sim=Scene.Sim;var body=sim.Frame.Body;
            bool available=body.HasAtmosphere && Profiles.TryGetValue(body,out var profile) && !Scene.Map.Active;
            plumeProperties.SetFloat("_PlanetEnabled",available?1:0);
            if(available) {
                var p=Profiles[body];double ut=sim.RenderUT;
                var origin=sim.Frame.RenderOrigin(sim.RenderAlpha);
                plumeProperties.SetVector("_PlanetCamera",(Vector3)(origin+(Vector3d)Scene.Camera.Cam.transform.position));
                plumeProperties.SetVector("_PlanetBodyCenter",(Vector3)(-origin));
                plumeProperties.SetMatrix("_PlanetWorldToBody",Matrix4x4.Rotate(body.RotationAtUT(ut).Inverse().ToQuaternion()));
                plumeProperties.SetFloat("_PlanetRadius",(float)body.Radius);
                plumeProperties.SetFloat("_PlanetScattering",HasScattering(body)?1:0);
                plumeProperties.SetVector("_PlanetRayleigh",new Vector4(p.Rayleigh.r,p.Rayleigh.g,p.Rayleigh.b,p.RayleighHeight));
                plumeProperties.SetVector("_PlanetMie",new Vector4(p.Mie,p.MieHeight,0,0));plumeProperties.SetVector("_PlanetOzone",p.Ozone);
                plumeProperties.SetVector("_PlanetClouds",new Vector4(p.CloudBase,p.CloudTop,1,p.CloudDensity));
                plumeProperties.SetVector("_PlanetCloudShape",new Vector4(p.CloudBillowScale,p.CloudOpticalDepth,0,0));
                BindPlumeClouds(plumeProperties,p,Library!=null?Library.CloudNoise:null,PlanetGraphics.Clouds);
                float wind=(float)PlanetVisualMath.WindOffset(ut,p.WindSpeed,2*Math.PI*body.Radius);
                float motion=(float)PlanetVisualMath.WindOffset(ut,p.WindSpeed,16000);
                plumeProperties.SetVector("_PlanetWind",new Vector4(wind,motion,0,motion*.3f));
            }
            renderer.SetPropertyBlock(plumeProperties);plumeProperties.Clear();
        }
        /// <summary>
        /// A property block throws on a null texture, unlike a material. A world with air but no weather map (any
        /// generated profile) therefore gets cloud mode 0 and no texture bindings; the shader samples neither then.
        /// </summary>
        public static void BindPlumeClouds(MaterialPropertyBlock block,PlanetVisualProfile p,Texture3D noise,PlanetCloudMode clouds)
        {
            int mode=clouds==PlanetCloudMode.Off || p.Weather==null?0:clouds==PlanetCloudMode.Volumetric && noise!=null?2:1;
            block.SetFloat("_PlanetCloudMode",mode);
            if(p.Weather!=null) block.SetTexture("_PlanetWeather",p.Weather);
            if(noise!=null) block.SetTexture("_PlanetNoise",noise);
        }
        public bool View(Camera camera,CelestialBody body,out Vector3d cameraRelative,out float scale,out bool volume)
        {
            scale=1;volume=false;cameraRelative=Vector3d.zero;
            var sim=Scene.Sim;
            if(camera==Scene.Map.MapCamera) {
                if(!Scene.Map.Active || MapView.ShowBiomes) return false;
                scale=(float)MapView.Scale;
                cameraRelative=(Vector3d)camera.transform.position*scale+Scene.Map.FocusAbsolute(sim.UT)-body.GetPositionAtUT(sim.UT);
                return true;
            }
            if(Scene.Map.Active) return false;
            bool isLocal=false;
            foreach(var fb in Scene.FarView.Bodies) if(fb.Body==body) isLocal=fb.Local;
            if(camera==Scene.Camera.Cam && !isLocal || camera==Scene.FarView.Cam && isLocal) return false;
            if(camera!=Scene.Camera.Cam && camera!=Scene.FarView.Cam) return false;
            scale=camera==Scene.FarView.Cam?(float)ScaledSpace.Scale:1;
            cameraRelative=sim.Frame.RenderOrigin(sim.RenderAlpha)+(Vector3d)Scene.Camera.Cam.transform.position-sim.Frame.BodyPosition(body,sim.RenderUT);
            volume=PlanetGraphics.Clouds==PlanetCloudMode.Volumetric && PlanetVisualMath.CloudVolumeBlend(cameraRelative.magnitude,body.Radius)>0;
            return true;
        }
        void OnDestroy()
        {
            if(Instance==this) Instance=null;
            Destroyed?.Invoke();
            foreach(var o in owned) if(o!=null) Destroy(o);
        }
    }
}
