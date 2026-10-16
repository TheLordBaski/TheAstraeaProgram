using System;
using System.IO;
using System.Threading.Tasks;
using TAP.Core;
using TAP.Simulation;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace TAP.Editor
{
    /// <summary>Offline presentation assets. Never changes terrain definitions, collision or simulation saves.</summary>
    public static class PlanetVisualBaker
    {
        const string Root = "Assets/TAP/Resources/PlanetVisuals/";
        static readonly string[] Tiles = { "sparse_grass", "forest_ground_04", "coast_sand_01", "rocky_terrain_02", "snow_02", "moon_01" };
        public static string Status { get; private set; } = "Idle";
        static Task<Bake> pending;
        static CelestialSystem system;
        static int bodyIndex;
        static bool weatherOnly;
        sealed class Settings
        {
            public int Seed; public float Coverage;
            public AtmosphereLuts.Params Sky;
            public int LargeCloudBanks,SmallCloudPatches,ThinCloudFronts;
            public WeatherSourceMode WeatherMode;
            public Color32[] Source; public int SourceWidth,SourceHeight; public float CloudRelief;
            public string SurfaceKey,WeatherKey,AtmosphereKey;
            public SurfaceAlbedoSynth.Set Exemplars; public int AlbedoWidth;
        }
        sealed class Bake { public CelestialBody Body; public Color32[] Color, Normal, Mask, Weather, WeatherNormal; public Color[] Lut, MultiScatter; public int Width, AlbedoWidth; public Settings Settings; }

        [MenuItem("TAP/Planet visuals/Bake all")]
        public static void Start()
        {
            if (pending != null) throw new InvalidOperationException("A planet bake is already running.");
            Directory.CreateDirectory(Root);
            AssetDatabase.Refresh();
            weatherOnly=false;BuildLibrary();
            system = CelestialSystem.LoadFromResources(); bodyIndex = 0;
            EditorApplication.update -= Poll;
            EditorApplication.update += Poll;
            Next();
        }
        [MenuItem("TAP/Planet visuals/Bake weather only")]
        public static void StartWeatherOnly()
        {
            if(pending!=null) throw new InvalidOperationException("A planet bake is already running.");
            weatherOnly=true;system=CelestialSystem.LoadFromResources();bodyIndex=0;
            EditorApplication.update-=Poll;EditorApplication.update+=Poll;Next();
        }
        static void Next()
        {
            while (bodyIndex < system.Bodies.Count && (system.Bodies[bodyIndex].IsStar || (weatherOnly && !system.Bodies[bodyIndex].HasAtmosphere))) bodyIndex++;
            if (bodyIndex >= system.Bodies.Count) { Status = "Complete"; EditorApplication.update -= Poll; AssetDatabase.SaveAssets(); return; }
            var b = system.Bodies[bodyIndex++];
            var path=Root+b.Id+".asset";var profile=AssetDatabase.LoadAssetAtPath<PlanetVisualProfile>(path);
            if(profile==null) {
                profile=PlanetVisualProfile.Fallback(b);profile.CloudCoverage=b.HasAtmosphere&&b.HasOcean?.30f:0;
                AssetDatabase.CreateAsset(profile,path);
            }
            // Snapshot authoring values on the main thread. A mid-bake edit cannot bless stale worker output.
            var settings=new Settings { Seed=profile.WeatherSeed,Coverage=profile.CloudCoverage,Sky=AtmosphereLuts.Params.Of(b,profile),WeatherMode=profile.WeatherMode,
                LargeCloudBanks=Math.Max(0,profile.LargeCloudBanks),SmallCloudPatches=Math.Max(0,profile.SmallCloudPatches),ThinCloudFronts=Math.Max(0,profile.ThinCloudFronts),
                CloudRelief=profile.CloudRelief,SurfaceKey=profile.ExpectedSurfaceKey(b),AtmosphereKey=profile.ExpectedAtmosphereKey(b) };
            // Source art stays outside version control (Docs/PLANET_VISUALS_SOURCES.md). Without it a bake would replace
            // the committed maps with procedural weather and flat colours, so it stops instead.
            string missing=b.HasAtmosphere && !string.IsNullOrWhiteSpace(profile.WeatherSourceFile) && !File.Exists(profile.WeatherSourceFile)
                ? b.Id+" weather source "+profile.WeatherSourceFile+" is missing." : null;
            if(missing==null && !weatherOnly && profile.SatelliteAlbedo && b.Terrain is LayeredTerrain) {
                settings.Exemplars=SurfaceAlbedoSynth.Load();
                if(!settings.Exemplars.Any) missing=b.Id+" satellite exemplars ("+SurfaceAlbedoSynth.Folder+") are missing.";
                settings.AlbedoWidth=PlanetVisualProfile.SatelliteAlbedoWidth;
            } else settings.AlbedoWidth=4096;
            if(missing!=null) {
                Status="Failed: "+missing;Debug.LogError("Planet visual bake stopped: "+missing+" See Docs/PLANET_VISUALS_SOURCES.md.");
                EditorApplication.update-=Poll;return;
            }
            LoadWeatherSource(profile,settings);settings.WeatherKey=profile.ExpectedWeatherKey();
            Status = "Baking " + b.Id;
            pending = Task.Run(() => {
                var result=weatherOnly ? new Bake {Body=b,Settings=settings,Weather=GenerateWeather(b,settings)} : Generate(b,4096,settings);
                if(result.Weather!=null)result.WeatherNormal=WeatherNormals(result.Weather,4096,b.Radius,settings.CloudRelief);
                return result;
            });
        }
        [MenuItem("TAP/Planet visuals/Bake atmosphere only")]
        public static void StartAtmosphereOnly()
        {
            if(pending!=null)throw new InvalidOperationException("A planet bake is already running.");
            foreach(var body in CelestialSystem.LoadFromResources().Bodies) {
                if(!body.HasAtmosphere)continue;
                var p=AssetDatabase.LoadAssetAtPath<PlanetVisualProfile>(Root+body.Id+".asset");if(p==null)continue;
                var sky=AtmosphereLuts.Params.Of(body,p);
                var lut=AtmosphereLuts.Transmittance(sky);
                SaveSky(p,body,lut,AtmosphereLuts.MultiScattering(sky,lut));
                p.AtmosphereBakeKey=p.ExpectedAtmosphereKey(body);EditorUtility.SetDirty(p);
            }
            AssetDatabase.SaveAssets();Status="Complete";
        }
        // Half-float lookup tables keep sunset and limb gradients free of 8-bit banding. Stored as binary EXR images
        // (readable on the CPU for the sunlight tint), never as texel hex in a text asset.
        static void SaveSky(PlanetVisualProfile p,CelestialBody body,Color[] transmittance,Color[] multi)
        {
            p.Transmittance=SaveExr(transmittance,AtmosphereLuts.TransmittanceWidth,AtmosphereLuts.TransmittanceHeight,body.Id+"_transmittance");
            p.MultiScattering=SaveExr(multi,AtmosphereLuts.MultiSize,AtmosphereLuts.MultiSize,body.Id+"_multiscatter");
            foreach(var old in new[]{"_transmittance.png","_transmittance.asset","_multiscatter.asset"}) AssetDatabase.DeleteAsset(Root+body.Id+old);
        }
        static Texture2D SaveExr(Color[] pixels,int w,int h,string name)
        {
            var t=new Texture2D(w,h,TextureFormat.RGBAHalf,false,true);
            try { t.SetPixels(pixels);t.Apply(false,false);File.WriteAllBytes(Root+name+".exr",t.EncodeToEXR(Texture2D.EXRFlags.None)); }
            finally { UnityEngine.Object.DestroyImmediate(t); }
            string path=Root+name+".exr";AssetDatabase.ImportAsset(path);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType=TextureImporterType.Default;importer.sRGBTexture=false;importer.mipmapEnabled=false;importer.isReadable=true;
            importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Bilinear;importer.npotScale=TextureImporterNPOTScale.None;
            importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings { name="Standalone",overridden=true,maxTextureSize=Math.Max(w,h),format=TextureImporterFormat.RGBAHalf });
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        // Generated textures live in the import cache: the repository keeps a manifest (and source images), never
        // the texels as hex text in an asset.
        static Texture2DArray ArrayFromManifest(string name,string[] layers,bool linear,int size,int aniso)
        {
            string path=Root+name+".texarray";
            string json=JsonUtility.ToJson(new TextureArrayImporter.Manifest { linear=linear,size=size,aniso=aniso,layers=layers },true);
            if(!File.Exists(path) || File.ReadAllText(path)!=json) { File.WriteAllText(path,json);AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate); }
            return AssetDatabase.LoadAssetAtPath<Texture2DArray>(path);
        }
        static Texture3D NoiseFromManifest(string name,string kind,int seed)
        {
            string path=Root+name+".cloudnoise";
            string json=JsonUtility.ToJson(new CloudNoiseImporter.Manifest { kind=kind,seed=seed },true);
            if(!File.Exists(path) || File.ReadAllText(path)!=json) { File.WriteAllText(path,json);AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate); }
            return AssetDatabase.LoadAssetAtPath<Texture3D>(path);
        }

        [MenuItem("TAP/Planet visuals/Rebuild material library")]
        public static void RebuildLibrary() { BuildLibrary();AssetDatabase.SaveAssets();Status="Complete"; }
        static void Poll()
        {
            if (pending == null || !pending.IsCompleted) return;
            var task = pending; pending = null;
            if (task.IsFaulted) { Status = "Failed: " + task.Exception; Debug.LogError(Status); EditorApplication.update -= Poll; return; }
            var data = task.Result; var b = data.Body;
            var path = Root + b.Id + ".asset";
            var p = AssetDatabase.LoadAssetAtPath<PlanetVisualProfile>(path);
            if (p == null) { p = PlanetVisualProfile.Fallback(b); p.CloudCoverage=b.HasAtmosphere && b.HasOcean?.30f:0; AssetDatabase.CreateAsset(p, path); }
            if(!weatherOnly) {
                p.TerrainHash = TerrainFingerprint.Hash(b.Def.terrain);
                p.SurfaceBakeKey=data.Settings.SurfaceKey;p.AtmosphereBakeKey=data.Settings.AtmosphereKey;
                p.Surface = Save(data.Color, data.AlbedoWidth, data.AlbedoWidth / 2, b.Id + "_surface", false);
                p.SurfaceNormal = Save(data.Normal, data.Width, data.Width / 2, b.Id + "_normal", true);
                p.SurfaceMask = Save(data.Mask, data.Width, data.Width / 2, b.Id + "_mask", true);
            }
            if (data.Weather != null) {p.Weather = Save(data.Weather,4096,2048,b.Id+"_weather",true);p.WeatherBakeKey=data.Settings.WeatherKey;}
            if (data.WeatherNormal != null) p.WeatherNormal = Save(data.WeatherNormal,4096,2048,b.Id+"_weather_normal",true);
            if (data.Lut != null) SaveSky(p,b,data.Lut,data.MultiScatter);
            EditorUtility.SetDirty(p); AssetDatabase.SaveAssets(); Next();
        }
        static Texture2D Save(Color32[] pixels, int w, int h, string name, bool linear, bool mip = true)
        {
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false, linear); t.SetPixels32(pixels); t.Apply();
            string path = Root + name + ".png"; File.WriteAllBytes(path, t.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(t);
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.sRGBTexture = !linear; importer.mipmapEnabled = mip; importer.maxTextureSize = Math.Max(4096, w);
            importer.filterMode = mip ? FilterMode.Trilinear : FilterMode.Bilinear;
            importer.isReadable = !mip;
            importer.wrapModeU = mip?TextureWrapMode.Repeat:TextureWrapMode.Clamp; importer.wrapModeV = TextureWrapMode.Clamp;
            importer.textureCompression = mip ? TextureImporterCompression.CompressedHQ : TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport(); return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
        static Bake Generate(CelestialBody b, int w,Settings settings)
        {
            int h = w / 2; var g = b.Terrain;
            int aw = settings.AlbedoWidth > 0 ? settings.AlbedoWidth : w, ah = aw / 2;
            bool synth = settings.Exemplars != null && settings.Exemplars.Any && g is LayeredTerrain;
            var r = new Bake { Body = b, Width = w, AlbedoWidth = aw, Settings=settings,Color = new Color32[aw*ah], Normal = new Color32[w*h], Mask = new Color32[w*h] };
            var heights = new float[w*h];
            var options = new ParallelOptions { MaxDegreeOfParallelism = Math.Max(2, Environment.ProcessorCount - 2) };
            Parallel.For(0, h, options, y => {
                var f = new double[Math.Max(1, g.FieldCount)];
                for (int x = 0; x < w; x++) {
                    int i = y*w+x; var d = PlanetMaps.DirectionOf(x,y,w,h);
                    double height = g.Sample(d,f,0,true); heights[i] = (float)(b.HasOcean ? Math.Max(0,height) : height);
                    if (!synth && aw == w) r.Color[i] = g.Colorize(d,height,f,0,1);
                    int m = g.BiomeAt(d).Material;
                    int tile = !b.HasAtmosphere && !b.HasOcean ? 5 : m == 1 ? 1 : m == 3 ? 2 : m == 4 || m == 2 ? 3 : m == 5 || m == 6 ? 4 : 0;
                    r.Mask[i] = new Color32((byte)(tile*40), (byte)(b.HasOcean && height < 0 ? 255 : 0), (byte)Math.Min(255, Math.Max(0,-height/20)),255);
                }
            });
            Parallel.For(0,h,options,y=> {
                double lat = ((y+.5)/h-.5)*Math.PI;
                float dx = (float)(2*Math.PI*b.Radius/w*Math.Max(.01,Math.Cos(lat))), dy = (float)(Math.PI*b.Radius/h);
                for(int x=0;x<w;x++) {
                    int i=y*w+x;
                    float sx=(heights[y*w+(x+1)%w]-heights[y*w+(x+w-1)%w])/(2*dx);
                    float sy=(heights[Math.Min(h-1,y+1)*w+x]-heights[Math.Max(0,y-1)*w+x])/(2*dy);
                    var n=new Vector3(-sx,-sy,1).normalized;
                    r.Normal[i]=(Color32)new Color(n.x*.5f+.5f,n.y*.5f+.5f,n.z*.5f+.5f,1);
                }
            });
            if (synth) SynthesizeAlbedo(b, (LayeredTerrain)g, settings.Exemplars, r, heights, w, options);
            else if (aw != w) Parallel.For(0, ah, options, y => {
                var f = new double[Math.Max(1, g.FieldCount)];
                for (int x = 0; x < aw; x++) { var d = PlanetMaps.DirectionOf(x,y,aw,ah); r.Color[y*aw+x] = g.Colorize(d,g.Sample(d,f,0,true),f,0,1); }
            });
            if (b.HasAtmosphere) {
                r.Weather=GenerateWeather(b,settings);
                r.Lut=AtmosphereLuts.Transmittance(settings.Sky);
                r.MultiScatter=AtmosphereLuts.MultiScattering(settings.Sky,r.Lut);
            }
            return r;
        }
        // Each colour rule of the body's own terrain is painted with its landscape's satellite texture; the rules mix
        // exactly as Colorize mixes their flat colours. The mask's alpha keeps the dominant landscape for the shader's
        // closer detail layers.
        static void SynthesizeAlbedo(CelestialBody b,LayeredTerrain g,SurfaceAlbedoSynth.Set set,Bake r,float[] heights,int w,ParallelOptions options)
        {
            int aw=r.AlbedoWidth,ah=aw/2,h=w/2,rules=g.ColorRuleCount;
            var classes=new int[rules];var colors=new Color32[rules];
            for(int i=0;i<rules;i++) { classes[i]=SurfaceAlbedoSynth.ClassOf(g.ColorRuleNote(i));colors[i]=g.ColorRuleColor(i); }
            var noise=new Noise3D(g.Def.seed^0x5a7e11);
            var dominant=new byte[aw*ah];
            Parallel.For(0,ah,options,y=> {
                var f=new double[Math.Max(1,g.FieldCount)];var weights=new double[rules+1];
                for(int x=0;x<aw;x++) {
                    var d=PlanetMaps.DirectionOf(x,y,aw,ah);
                    double height=g.Sample(d,f,0,true);
                    var nc=r.Normal[Math.Min(h-1,y*h/ah)*w+Math.Min(w-1,x*w/aw)];
                    g.ColorWeights(d,height,f,0,nc.b/255.0*2-1,weights);
                    var p=d*b.Radius;var c=Vector3.zero;double best=0;int bestClass=-1;
                    for(int k=0;k<rules;k++) {
                        if(weights[k]<.002) continue;
                        c+=SurfaceAlbedoSynth.Paint(set,classes[k],colors[k],p,d,noise,b.Radius)*(float)weights[k];
                        if(classes[k]>=0 && weights[k]>best) { best=weights[k];bestClass=classes[k]; }
                    }
                    c+=new Vector3(.216f,.216f,.216f)*(float)weights[rules];
                    r.Color[y*aw+x]=SurfaceAlbedoSynth.ToSrgb(c);
                    dominant[y*aw+x]=(byte)(bestClass<0?255:bestClass*40);
                }
            });
            for(int y=0;y<h;y++) for(int x=0;x<w;x++) { int i=y*w+x;var m=r.Mask[i];m.a=dominant[Math.Min(ah-1,y*ah/h)*aw+Math.Min(aw-1,x*aw/w)];r.Mask[i]=m; }
        }
        static void LoadWeatherSource(PlanetVisualProfile profile,Settings settings)
        {
            profile.WeatherSourceHash="";
            if(string.IsNullOrWhiteSpace(profile.WeatherSourceFile))return;
            string project=Path.GetFullPath(Path.Combine(Application.dataPath,".."))+Path.DirectorySeparatorChar;
            string path=Path.GetFullPath(Path.Combine(project,profile.WeatherSourceFile));
            if(!path.StartsWith(project,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Weather source must be inside this project.");
            byte[] bytes=File.ReadAllBytes(path);
            using(var sha=System.Security.Cryptography.SHA256.Create())profile.WeatherSourceHash=BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","");
            var texture=new Texture2D(2,2,TextureFormat.RGBA32,false,true);
            try {
                if(!texture.LoadImage(bytes))throw new InvalidOperationException("Weather source must be a readable PNG or JPEG.");
                settings.Source=texture.GetPixels32();settings.SourceWidth=texture.width;settings.SourceHeight=texture.height;
            } finally {UnityEngine.Object.DestroyImmediate(texture);}
        }
        // Source art is cropped into seeded regional systems; it is never sampled at runtime.
        static float SourceSample(Settings settings,double u,double v)
        {
            u-=Math.Floor(u);v=Math.Max(.08,Math.Min(.92,v));
            double x=u*settings.SourceWidth-.5,y=v*settings.SourceHeight-.5;
            int x0=(int)Math.Floor(x),y0=Math.Max(0,Math.Min(settings.SourceHeight-2,(int)Math.Floor(y)));
            float a=(float)(x-Math.Floor(x)),b=(float)(y-y0);
            int left=(x0+settings.SourceWidth)%settings.SourceWidth,right=(left+1)%settings.SourceWidth;
            var pixels=settings.Source;int stride=settings.SourceWidth;
            return Mathf.Lerp(Mathf.Lerp(pixels[y0*stride+left].r,pixels[y0*stride+right].r,a),Mathf.Lerp(pixels[(y0+1)*stride+left].r,pixels[(y0+1)*stride+right].r,a),b)/255f;
        }
        static Color32[] WeatherNormals(Color32[] weather,int width,double radius,float relief)
        {
            int height=width/2;var result=new Color32[weather.Length];
            float H(int x,int y) {var c=weather[Math.Max(0,Math.Min(height-1,y))*width+(x+width)%width];return c.r/255f*(.65f+c.g/255f*.35f)*relief;}
            Parallel.For(0,height,new ParallelOptions {MaxDegreeOfParallelism=2},y=>{
                double lat=((y+.5)/height-.5)*Math.PI;
                float dx=(float)(2*Math.PI*radius/width*Math.Max(.02,Math.Cos(lat))),dy=(float)(Math.PI*radius/height);
                for(int x=0;x<width;x++) {
                    var normal=new Vector3(-(H(x+1,y)-H(x-1,y))/(2*dx),-(H(x,y+1)-H(x,y-1))/(2*dy),1).normalized;
                    result[y*width+x]=(Color32)new Color(normal.x*.5f+.5f,normal.y*.5f+.5f,normal.z*.5f+.5f,1);
                }
            });return result;
        }
        // Regional systems set the layout; fine noise only breaks up their interior and edges.
        // Sampling these in body-fixed 3D avoids both a longitude seam and a global speckle field.
        sealed class WeatherSystem
        {
            public Vector3d Centre,Along,Across;
            public double Length,Width,Bend,Opacity;
            public int Kind;
        }
        static WeatherSystem[] WeatherSystems(double radius,Settings settings)
        {
            var random=new SeededRandom(settings.Seed^0x47a12b);
            int smallStart=settings.LargeCloudBanks,thinStart=smallStart+settings.SmallCloudPatches;
            var systems=new WeatherSystem[thinStart+settings.ThinCloudFronts];
            double extent=Math.Sqrt(Math.Max(0,settings.Coverage)/.30);
            for(int i=0;i<systems.Length;i++) {
                // A few broad banks, smaller broken patches, then translucent streaks.
                int kind=i<smallStart?0:i<thinStart?1:2;
                double latitude=random.NextDouble()*1.8-.9,longitude=random.NextDouble()*Math.PI*2;
                var centre=new Vector3d(Math.Sqrt(1-latitude*latitude)*Math.Cos(longitude),latitude,Math.Sqrt(1-latitude*latitude)*Math.Sin(longitude));
                var east=Vector3d.Cross(Vector3d.up,centre).normalized;
                var north=Vector3d.Cross(centre,east);
                double angle=random.NextDouble()*Math.PI*2;
                var along=east*Math.Cos(angle)+north*Math.Sin(angle);
                double length=kind==0?220000+random.NextDouble()*110000:kind==1?55000+random.NextDouble()*80000:120000+random.NextDouble()*160000;
                double width=kind==0?55000+random.NextDouble()*60000:kind==1?25000+random.NextDouble()*35000:22000+random.NextDouble()*30000;
                // Preserve kilometre-scale weather on Tellus, but keep custom tiny worlds in bounds.
                systems[i]=new WeatherSystem {Centre=centre,Along=along,Across=Vector3d.Cross(centre,along),
                    Length=Math.Min(length,radius*.55)*extent,Width=Math.Min(width,radius*.2)*extent,
                    Bend=(random.NextDouble()-.5)*1.5,Opacity=kind==2?.16+random.NextDouble()*.2:kind==1?.30+random.NextDouble()*.60:.65+random.NextDouble()*.3,Kind=kind};
            }
            return systems;
        }
        static float Smooth(double lo,double hi,double value)
        {
            double t=Math.Max(0,Math.Min(1,(value-lo)/(hi-lo)));return (float)(t*t*(3-2*t));
        }
        // Whole-planet source map: R optical coverage, G cloud type (0 thin and low, 1 thick and tall). The seed rotates
        // it in longitude; polar haze and ice that a cloud composite cannot separate from cloud are thinned out.
        static Color32[] GlobalWeather(Settings s,int width)
        {
            int height=width/2;var pixels=new Color32[width*height];var bright=new float[width*height];
            double rotation=(s.Seed&65535)/65536.0;
            double lo=.30-.26*Math.Max(0,Math.Min(1,s.Coverage)),hi=Math.Min(.95,lo+.62);
            Parallel.For(0,height,y=> {
                double v=(y+.5)/height;
                for(int x=0;x<width;x++) {
                    // Box filter over the source texels covering this pixel (the source is usually 2x larger).
                    double u=(x+.5)/width+rotation,du=.25/width,dv=.25/height;
                    bright[y*width+x]=(SourcePixel(s,u-du,v-dv)+SourcePixel(s,u+du,v-dv)+SourcePixel(s,u-du,v+dv)+SourcePixel(s,u+du,v+dv))*.25f;
                }
            });
            var type=Blur(bright,width,height,8);
            Parallel.For(0,height,y=> {
                double lat=((y+.5)/height-.5)*180;
                double polar=1-.55*Smooth(58,80,Math.Abs(lat));
                if(lat<-62) polar*=1-.55*Smooth(62,74,-lat);
                for(int x=0;x<width;x++) {
                    int i=y*width+x;
                    double c=Math.Pow(Math.Max(0,Math.Min(1,(bright[i]-lo)/(hi-lo))),1.12)*polar;
                    double t=Math.Max(0,Math.Min(1,(type[i]-.22)/.5));
                    pixels[i]=(Color32)new Color((float)c,(float)t,1,1);
                }
            });
            return pixels;
        }
        static float SourcePixel(Settings s,double u,double v)
        {
            u-=Math.Floor(u);
            int x=Math.Min(s.SourceWidth-1,(int)(u*s.SourceWidth)),y=Math.Max(0,Math.Min(s.SourceHeight-1,(int)(v*s.SourceHeight)));
            var c=s.Source[y*s.SourceWidth+x];return (c.r*.3f+c.g*.59f+c.b*.11f)/255f;
        }
        static float[] Blur(float[] src,int w,int h,int radius)
        {
            var tmp=new float[src.Length];var dst=new float[src.Length];
            Parallel.For(0,h,y=> { double sum=0;int row=y*w;
                for(int k=-radius;k<=radius;k++) sum+=src[row+((k%w)+w)%w];
                for(int x=0;x<w;x++) { tmp[row+x]=(float)(sum/(2*radius+1)); sum+=src[row+(x+radius+1)%w]-src[row+((x-radius)%w+w)%w]; } });
            Parallel.For(0,w,x=> { for(int y=0;y<h;y++) { double sum=0;int n=0;
                for(int k=Math.Max(0,y-radius);k<=Math.Min(h-1,y+radius);k++) { sum+=tmp[k*w+x];n++; }
                dst[y*w+x]=(float)(sum/n); } });
            return dst;
        }
        static Color32[] GenerateWeather(CelestialBody body,Settings settings,int width=4096)
        {
            int height=width/2;
            if(settings.WeatherMode==WeatherSourceMode.Global && settings.Source!=null) return settings.Coverage<=0?new Color32[width*height]:GlobalWeather(settings,width);
            var pixels=new Color32[width*height];var noise=new Noise3D(settings.Seed);
            if(settings.Coverage<=0)return pixels;
            var systems=WeatherSystems(body.Radius,settings);
            if(systems.Length==0)return pixels;
            Parallel.For(0,height,new ParallelOptions {MaxDegreeOfParallelism=2},y=> {
                for(int x=0;x<width;x++) {
                    var direction=PlanetMaps.DirectionOf(x,y,width,height);
                    var p=direction*body.Radius;
                    var w=p/80000;
                    var warp=new Vector3d(noise.Fbm(w+new Vector3d(31,17,83),3),noise.Fbm(w+new Vector3d(73,5,42),3),noise.Fbm(w+new Vector3d(11,92,26),3))*27000;
                    var q=(p+warp).normalized*body.Radius;
                    double clusters=noise.Fbm(q/34000+new Vector3d(52,77,4),3);
                    double cells=noise.Fbm(q/5500+new Vector3d(6,19,71),3);
                    double streak=noise.Fbm(new Vector3d(q.x/42000,q.y/4500,q.z/19000)+new Vector3d(82,3,43),3);
                    float cover=0,regional=0;
                    for(int systemIndex=0;systemIndex<systems.Length;systemIndex++) {
                        var system=systems[systemIndex];
                        if(Vector3d.Dot(direction,system.Centre)<.65)continue;
                        var delta=q-system.Centre*body.Radius;
                        double u=Vector3d.Dot(delta,system.Along)/system.Length;
                        double v=Vector3d.Dot(delta,system.Across)/system.Width+system.Bend*(u*u-.25);
                        double r2=u*u+v*v;
                        if(r2>2.2)continue;
                        float envelope=1-Smooth(.18,1.65,r2+clusters*.95);
                        regional=Math.Max(regional,envelope);
                        float shape;
                        if(settings.Source!=null) {
                            // Distinct crops avoid repeating one cloud stamp. The resulting optical
                            // map controls distant layers, near volumes and terrain shadows together.
                            double phase=(settings.Seed&65535)/65535.0+systemIndex*.61803398875;
                            double sourceU=phase+u*.19+v*.035;
                            double sourceV=.22+(systemIndex*.38196601125%1)*.50+v*.16-u*.025;
                            float source=(float)Math.Pow(Math.Max(0,(SourceSample(settings,sourceU,sourceV)-.035)/.965),.65);
                            shape=envelope*source;
                            if(system.Kind==2)shape*=.45f+.55f*Smooth(-.25,.45,streak);
                        } else if(system.Kind==2) {
                            shape=envelope*Smooth(.05,.55,.2+streak*.65+clusters*.5);
                        } else {
                            float bank=envelope*Smooth(.16,.65,envelope+clusters*.8);
                            float broken=envelope*Smooth(.02,.30,clusters+envelope*.25)*Smooth(.05,.32,cells)*.7f;
                            float grain=.18f+.82f*Smooth(-.16,.20,cells);
                            float gaps=.25f+.75f*Smooth(-.38,-.08,clusters);
                            shape=Math.Max(bank*(system.Kind==0?1:.65f)*grain*gaps,broken);
                        }
                        cover=Math.Max(cover,shape*(float)system.Opacity);
                    }
                    // R is continuous optical coverage, including thin clouds; never threshold it
                    // again after filtering. G adds billow detail, B retains the regional envelope.
                    pixels[y*width+x]=(Color32)new Color(cover,(float)(cells*.5+.5),regional,1);
                }
            });
            return pixels;
        }
        const string Materials="Assets/TAP/Art/PlanetMaterials/",SatelliteTiles="Assets/TAP/Art/SatelliteDetail/";
        static void BuildLibrary()
        {
            string path=Root+"Library.asset"; var lib=AssetDatabase.LoadAssetAtPath<PlanetVisualLibrary>(path);
            if(lib==null) { lib=ScriptableObject.CreateInstance<PlanetVisualLibrary>(); AssetDatabase.CreateAsset(lib,path); }
            lib.TellusStone=AssetDatabase.LoadAssetAtPath<Texture2D>(Materials+"rocky_terrain_02_Diffuse.jpg");
            lib.LumaStone=AssetDatabase.LoadAssetAtPath<Texture2D>(Materials+"moon_01_Diffuse.jpg");
            lib.Albedo=ArrayFromManifest("GroundAlbedo",Layers("_Diffuse.jpg"),false,2048,4);
            lib.Normal=ArrayFromManifest("GroundNormal",Layers("_nor_gl.png"),true,2048,4);
            lib.Material=ArrayFromManifest("GroundMaterial",Layers("_arm.jpg"),true,2048,4);
            lib.SatelliteDetail=BuildSatelliteDetail();
            lib.CloudNoise=NoiseFromManifest("CloudNoise","shape",1931);
            lib.CloudDetail=NoiseFromManifest("CloudDetail","detail",7717);
            // Earlier bakes stored these texels as hex text in assets; the manifests above replace them.
            foreach(var old in new[]{"Albedo","Normal","Material","SatelliteDetail","CloudNoise","CloudDetail"}) AssetDatabase.DeleteAsset(Root+old+".asset");
            EditorUtility.SetDirty(lib);
            static string[] Layers(string suffix) => System.Array.ConvertAll(Tiles,t=>Materials+t+suffix);
        }
        [MenuItem("TAP/Planet visuals/Bake satellite detail")]
        public static void BakeSatelliteDetail()
        {
            var lib=AssetDatabase.LoadAssetAtPath<PlanetVisualLibrary>(Root+"Library.asset");if(lib==null) return;
            lib.SatelliteDetail=BuildSatelliteDetail();EditorUtility.SetDirty(lib);AssetDatabase.SaveAssets();
        }
        // One layer per landscape class (SurfaceAlbedoSynth.Classes): its texture as a ratio to its mean, stored x0.5 in
        // linear PNG tiles (versioned, binary) and built into a BC7 array on import, so the shader adds structure at
        // finer scales without shifting the baked colour. Without the local exemplars the committed tiles are kept.
        static Texture2DArray BuildSatelliteDetail()
        {
            var set=SurfaceAlbedoSynth.Load();
            var layers=new string[SurfaceAlbedoSynth.Classes.Length];
            for(int layer=0;layer<layers.Length;layer++) layers[layer]=SatelliteTiles+SurfaceAlbedoSynth.Classes[layer]+".png";
            if(set.Any) {
                Directory.CreateDirectory(SatelliteTiles);
                for(int layer=0;layer<layers.Length;layer++) {
                    var e=set.ByClass[layer].Length>0?set.ByClass[layer][0]:null;if(e==null) continue;
                    var pixels=new Color32[e.Size*e.Size];
                    for(int i=0;i<pixels.Length;i++) pixels[i]=new Color32(Half(e.R[i]/e.Mean.x),Half(e.G[i]/e.Mean.y),Half(e.B[i]/e.Mean.z),255);
                    var t=new Texture2D(e.Size,e.Size,TextureFormat.RGBA32,false,true);
                    try { t.SetPixels32(pixels);t.Apply(false,false);File.WriteAllBytes(layers[layer],t.EncodeToPNG()); }
                    finally { UnityEngine.Object.DestroyImmediate(t); }
                    AssetDatabase.ImportAsset(layers[layer]);
                }
            }
            if(!File.Exists(layers[0])) return null;
            string manifest=Root+"SatelliteDetail.texarray";
            var array=ArrayFromManifest("SatelliteDetail",layers,true,1024,8);
            // Tiles may change without the manifest changing: import the array again from the new texels.
            if(set.Any) { AssetDatabase.ImportAsset(manifest,ImportAssetOptions.ForceUpdate);array=AssetDatabase.LoadAssetAtPath<Texture2DArray>(manifest); }
            return array;
            static byte Half(float ratio) => (byte)Mathf.Clamp(Mathf.RoundToInt(ratio*.5f*255),0,255);
        }
    }
}
