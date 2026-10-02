using System;
using System.Collections.Generic;
using TAP.Core;
using TAP.Simulation;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

namespace TAP.Game
{
    public sealed class PlanetAtmosphereCloudFeature : ScriptableRendererFeature
    {
        public Shader Shader;
        PlanetPass pass;
        public override void Create() { pass?.Dispose(); pass=new PlanetPass(Shader); }
        public override void AddRenderPasses(ScriptableRenderer renderer,ref RenderingData data)
        {
            if(PlanetVisualController.Instance==null || data.cameraData.cameraType!=CameraType.Game || !pass.ShouldRender(data.cameraData.camera)) return;
            renderer.EnqueuePass(pass);
        }
        protected override void Dispose(bool disposing) { pass?.Dispose(); pass=null; }
        sealed class PlanetPass : ScriptableRenderPass
        {
            sealed class History
            {
                public RTHandle Scatter,Extinction;
                public Material Material;
                public MaterialPropertyBlock Properties=new MaterialPropertyBlock();
                public Vector3d Position;
                public Vector3d Origin;
                public Matrix4x4 VP;
                public Quaternion Rotation;
                public double UT;
                public int Width,Height,Frame,Mode,Quality;
                public bool MapActive;
                public ProfilingSampler IntegrateSampler,CompositeSampler;
                public void Dispose() { Scatter?.Release();Extinction?.Release();CoreUtils.Destroy(Material); }
            }
            sealed class Draw
            {
                public Material Material; public MaterialPropertyBlock Properties;
                public TextureHandle Source,Depth,Scatter,Extinction,FrontScatter,FrontExtinction,OldScatter,OldExtinction;
            }
            readonly Shader shader;
            readonly Plane[] planes=new Plane[6];
            readonly Dictionary<(EntityId,string),History> histories=new Dictionary<(EntityId,string),History>();
            static readonly int SourceId=UnityEngine.Shader.PropertyToID("_SourceColor"),DepthId=UnityEngine.Shader.PropertyToID("_Depth"),ScatterId=UnityEngine.Shader.PropertyToID("_Scatter"),ExtinctionId=UnityEngine.Shader.PropertyToID("_Extinction"),
                FrontScatterId=UnityEngine.Shader.PropertyToID("_FrontScatter"),FrontExtinctionId=UnityEngine.Shader.PropertyToID("_FrontExtinction"),HSId=UnityEngine.Shader.PropertyToID("_HistoryScatter"),HTId=UnityEngine.Shader.PropertyToID("_HistoryExtinction");
            public PlanetPass(Shader shader) {
                this.shader=shader; renderPassEvent=RenderPassEvent.BeforeRenderingTransparents;
                ConfigureInput(ScriptableRenderPassInput.Depth); requiresIntermediateTexture=true;
                PlanetVisualController.Destroyed+=ClearHistory;
            }
            void ClearHistory() { foreach(var h in histories.Values) h.Dispose();histories.Clear(); }
            public void Dispose() { PlanetVisualController.Destroyed-=ClearHistory;ClearHistory(); }
            bool Visible(PlanetVisualController visuals,Camera camera,CelestialBody body,PlanetVisualProfile profile,out Vector3d position,out float scale,out bool volume)
            {
                position=Vector3d.zero;scale=1;volume=false;
                if(!body.HasAtmosphere || (!visuals.HasScattering(body) && (PlanetGraphics.Clouds==PlanetCloudMode.Off || profile.Weather==null))) return false;
                if(!visuals.View(camera,body,out position,out scale,out volume)) return false;
                if(body.Radius/Math.Max(body.Radius,position.magnitude)<.0004) return false;
                var center=camera.transform.position-(Vector3)(position/scale);
                return PlanetVisualMath.BodyInFrustum(planes,center,(float)((body.Radius+body.Atmosphere.Height)/scale));
            }
            public bool ShouldRender(Camera camera)
            {
                var visuals=PlanetVisualController.Instance;if(visuals==null || shader==null) return false;
                GeometryUtility.CalculateFrustumPlanes(camera,planes);
                foreach(var entry in visuals.Profiles) if(Visible(visuals,camera,entry.Key,entry.Value,out _,out _,out _)) return true;
                return false;
            }
            public override void RecordRenderGraph(RenderGraph graph,ContextContainer frame)
            {
                var visuals=PlanetVisualController.Instance;
                if(visuals==null || shader==null) return;
                var camera=frame.Get<UniversalCameraData>().camera;
                GeometryUtility.CalculateFrustumPlanes(camera,planes);
                var resources=frame.Get<UniversalResourceData>();
                if(resources.isActiveTargetBackBuffer || !resources.cameraDepthTexture.IsValid()) return;
                foreach(var entry in visuals.Profiles) {
                    var body=entry.Key;var profile=entry.Value;
                    if(!Visible(visuals,camera,body,profile,out var position,out float scale,out bool volume)) continue;
                    var key=(camera.GetEntityId(),body.Id);
                    if(!histories.TryGetValue(key,out var history)) { history=new History { Material=CoreUtils.CreateEngineMaterial(shader),IntegrateSampler=new ProfilingSampler("Planet integration "+body.Id),CompositeSampler=new ProfilingSampler("Planet composition "+body.Id) };histories.Add(key,history); }
                    var desc=graph.GetTextureDesc(resources.activeColorTexture); desc.name="Planet scattering "+body.Id;
                    desc.width=Math.Max(1,desc.width/2);desc.height=Math.Max(1,desc.height/2);desc.msaaSamples=MSAASamples.None;desc.bindTextureMS=false;
                    desc.depthBufferBits=DepthBits.None;desc.colorFormat=GraphicsFormat.R16G16B16A16_SFloat;desc.clearBuffer=false;
                    int width=desc.width,height=desc.height;
                    bool resized=history.Width!=width || history.Height!=height;
                    bool historyChanged=resized || (volume && history.Scatter==null) || (!volume && history.Scatter!=null);
                    if(historyChanged) {
                        history.Scatter?.Release();history.Extinction?.Release();
                        history.Scatter=volume?RTHandles.Alloc(width,height,colorFormat:GraphicsFormat.R16G16B16A16_SFloat,filterMode:FilterMode.Bilinear,name:"Cloud history scatter"):null;
                        history.Extinction=volume?RTHandles.Alloc(width,height,colorFormat:GraphicsFormat.R16G16B16A16_SFloat,filterMode:FilterMode.Bilinear,name:"Cloud history transmission"):null;
                        history.Width=width;history.Height=height;
                    }
                    var sim=visuals.Scene.Sim;double ut=camera==visuals.Scene.Map.MapCamera?sim.UT:sim.RenderUT;
                    var origin=sim.Frame.Origin;
                    bool reset=historyChanged || history.Frame!=Time.frameCount-1 || Math.Abs(ut-history.UT)>1 || (position-history.Position).magnitude>2500
                        || Quaternion.Angle(camera.transform.rotation,history.Rotation)>12 || (origin-history.Origin).magnitude>1
                        || history.Mode!=(int)PlanetGraphics.Clouds || history.Quality!=(int)PlanetGraphics.Quality || history.MapActive!=visuals.Scene.Map.Active;
                    var props=history.Properties; props.Clear();
                    props.SetVector("_BlitScaleBias",new Vector4(1,1,0,0));
                    // Unity's view matrix includes its camera-space Z reflection; a Quaternion inverse does not.
                    var vp=PlanetVisualMath.ViewProjection(camera);
                    props.SetMatrix("_InverseVP",vp.inverse);props.SetMatrix("_PreviousVP",history.VP);
                    props.SetMatrix("_WorldToBody",Matrix4x4.Rotate(body.RotationAtUT(ut).Inverse().ToQuaternion()));
                    props.SetVector("_CameraRelative",(Vector3)position);props.SetVector("_PreviousCamera",(Vector3)history.Position);
                    props.SetVector("_CameraForward",camera.transform.forward);props.SetFloat("_Scale",scale);
                    props.SetFloat("_PixelAngle",2*Mathf.Tan(camera.fieldOfView*.5f*Mathf.Deg2Rad)/Math.Max(1,camera.pixelHeight));
                    props.SetVector("_SunDirection",(Vector3)sim.System.SunDirectionFrom(body,Vector3d.zero,ut));
                    // Renderer light units are irradiance/pi: a white Lambertian surface shows albedo * light.
                    props.SetVector("_SunIrradiance",(Vector4)(visuals.Scene.Sky.SunColor*(Mathf.PI*profile.SkyBrightness)));
                    props.SetVector("_Radii",new Vector4((float)body.Radius,(float)(body.Radius+body.Atmosphere.Height),0,0));
                    props.SetVector("_Rayleigh",new Vector4(profile.Rayleigh.r,profile.Rayleigh.g,profile.Rayleigh.b,profile.RayleighHeight));
                    props.SetVector("_Mie",new Vector4(profile.Mie,profile.MieHeight,profile.MieAnisotropy,0));props.SetVector("_Ozone",profile.Ozone);
                    props.SetVector("_CloudParams",new Vector4(profile.CloudBase,profile.CloudTop,1,profile.CloudDensity));
                    float detail=visuals.Library!=null&&visuals.Library.CloudNoise!=null?profile.CloudLayerDetail*(PlanetGraphics.Quality>=PlanetQuality.Medium?1:.6f):0;
                    props.SetVector("_CloudLayer",new Vector4(profile.CloudOpticalDepth,.55f,profile.CloudBillowScale,.045f));
                    props.SetVector("_CloudVolume",new Vector4(profile.CloudBillowScale,profile.CloudBillowScale*.13f,.38f,Mathf.Min(2000,(profile.CloudTop-profile.CloudBase)*.5f)));
                    if(profile.Weather!=null) props.SetVector("_Weather_TexelSize",new Vector4(1f/profile.Weather.width,1f/profile.Weather.height,profile.Weather.width,profile.Weather.height));
                    float wind=(float)PlanetVisualMath.WindOffset(ut,profile.WindSpeed,Math.PI*2*body.Radius);
                    float motion=(float)PlanetVisualMath.WindOffset(ut,profile.WindSpeed,16000);
                    props.SetVector("_Wind",new Vector4(wind,motion,0,motion*.3f));
                    int mode=PlanetGraphics.Clouds==PlanetCloudMode.Off || profile.Weather==null?0:volume?2:1;
                    props.SetFloat("_CloudMode",mode);props.SetFloat("_CloudSteps",PlanetGraphics.CloudSteps);
                    props.SetFloat("_VolumeBlend",volume?PlanetVisualMath.CloudVolumeBlend(position.magnitude,body.Radius):0);
                    props.SetFloat("_AtmosphereEnabled",visuals.HasScattering(body)?1:0);props.SetFloat("_AtmoSteps",PlanetGraphics.AtmosphereSteps);
                    props.SetFloat("_Jitter",mode==2?(.5f+Time.frameCount*.61803398875f)%1:.5f);
                    props.SetFloat("_HistoryWeight",reset||mode!=2?0:.7f);props.SetVector("_BufferSize",new Vector4(width,height,1f/width,1f/height));
                    props.SetFloat("_CloudNormalEnabled",profile.WeatherNormal!=null?1:0);
                    props.SetFloat("_LayerDetailStrength",detail);
                    // Static profile assets are material-owned; every per-frame graph texture is declared and bound below.
                    var m=history.Material;
                    m.SetTexture("_Weather",profile.Weather);m.SetTexture("_WeatherNormal",profile.WeatherNormal);
                    m.SetTexture("_TransmittanceLut",profile.Transmittance);m.SetTexture("_MultiScatterLut",profile.MultiScattering);
                    if(visuals.Library!=null) { m.SetTexture("_CloudNoise",visuals.Library.CloudNoise);m.SetTexture("_CloudDetail",visuals.Library.CloudDetail); }
                    // Texture-only views do not allocate or copy temporal history. Bind an already
                    // declared texture to the unused history inputs; history weight is zero there.
                    var oldS=volume?graph.ImportTexture(history.Scatter):resources.activeColorTexture;
                    var oldT=volume?graph.ImportTexture(history.Extinction):resources.activeColorTexture;
                    var scatter=graph.CreateTexture(desc); desc.name="Planet extinction "+body.Id;var extinction=graph.CreateTexture(desc);
                    desc.name="Planet front scattering "+body.Id;var frontScatter=graph.CreateTexture(desc);
                    desc.name="Planet front extinction "+body.Id;var frontExtinction=graph.CreateTexture(desc);
                    using(var builder=graph.AddRasterRenderPass<Draw>("Planet atmosphere and clouds "+body.Id,out var draw,history.IntegrateSampler)) {
                        draw.Material=m;draw.Properties=props;draw.Depth=resources.cameraDepthTexture;draw.OldScatter=oldS;draw.OldExtinction=oldT;
                        builder.UseTexture(draw.Depth,AccessFlags.Read);builder.UseTexture(oldS,AccessFlags.Read);builder.UseTexture(oldT,AccessFlags.Read);
                        builder.SetRenderAttachment(scatter,0,AccessFlags.Write);builder.SetRenderAttachment(extinction,1,AccessFlags.Write);
                        builder.SetRenderAttachment(frontScatter,2,AccessFlags.Write);builder.SetRenderAttachment(frontExtinction,3,AccessFlags.Write);
                        builder.SetRenderFunc(static (Draw data,RasterGraphContext context)=> {
                            data.Properties.SetTexture(DepthId,data.Depth);data.Properties.SetTexture(HSId,data.OldScatter);data.Properties.SetTexture(HTId,data.OldExtinction);
                            context.cmd.DrawProcedural(Matrix4x4.identity,data.Material,0,MeshTopology.Triangles,3,1,data.Properties);
                        });
                    }
                    // Subsequent transparent draws reuse the camera's depth attachment. Preserve its MSAA count.
                    var outputDesc=graph.GetTextureDesc(resources.activeColorTexture);outputDesc.name="Planet composite "+body.Id;outputDesc.clearBuffer=false;
                    var output=graph.CreateTexture(outputDesc);
                    using(var builder=graph.AddRasterRenderPass<Draw>("Planet depth-aware composition "+body.Id,out var draw,history.CompositeSampler)) {
                        draw.Material=m;draw.Properties=props;
                        draw.Source=resources.activeColorTexture;draw.Depth=resources.cameraDepthTexture;draw.Scatter=scatter;draw.Extinction=extinction;
                        draw.FrontScatter=frontScatter;draw.FrontExtinction=frontExtinction;
                        builder.UseTexture(draw.Source,AccessFlags.Read);builder.UseTexture(draw.Depth,AccessFlags.Read);builder.UseTexture(scatter,AccessFlags.Read);builder.UseTexture(extinction,AccessFlags.Read);
                        builder.UseTexture(frontScatter,AccessFlags.Read);builder.UseTexture(frontExtinction,AccessFlags.Read);
                        builder.SetRenderAttachment(output,0,AccessFlags.Write);
                        builder.SetRenderFunc(static (Draw data,RasterGraphContext context)=> {
                            data.Properties.SetTexture(SourceId,data.Source);data.Properties.SetTexture(DepthId,data.Depth);data.Properties.SetTexture(ScatterId,data.Scatter);data.Properties.SetTexture(ExtinctionId,data.Extinction);
                            data.Properties.SetTexture(FrontScatterId,data.FrontScatter);data.Properties.SetTexture(FrontExtinctionId,data.FrontExtinction);
                            context.cmd.DrawProcedural(Matrix4x4.identity,data.Material,1,MeshTopology.Triangles,3,1,data.Properties);
                        });
                    }
                    if(volume) {
                        graph.AddBlitPass(scatter,oldS,Vector2.one,Vector2.zero,passName:"Store cloud scatter history");
                        graph.AddBlitPass(extinction,oldT,Vector2.one,Vector2.zero,passName:"Store cloud extinction history");
                    }
                    resources.cameraColor=output;
                    history.Position=position;history.Origin=origin;history.VP=vp;history.Rotation=camera.transform.rotation;history.UT=ut;history.Frame=Time.frameCount;
                    history.Mode=(int)PlanetGraphics.Clouds;history.Quality=(int)PlanetGraphics.Quality;history.MapActive=visuals.Scene.Map.Active;
                }
            }
        }
    }
}
