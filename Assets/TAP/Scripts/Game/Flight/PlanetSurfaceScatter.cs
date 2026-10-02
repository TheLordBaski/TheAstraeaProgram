using System;
using System.Collections.Generic;
using TAP.Core;
using TAP.Simulation;
using UnityEngine;
using UnityEngine.Rendering;

namespace TAP.Game
{
    /// <summary>Deterministic visual stones, body-fixed and deliberately independent of physics colliders.</summary>
    [DefaultExecutionOrder(1150)]
    public sealed class PlanetSurfaceScatter : MonoBehaviour
    {
        public PlanetVisualController Visuals;
        struct Stone { public Vector3d Position; public Quaternion Rotation; public Vector3 Scale; }
        readonly List<Stone> stones=new List<Stone>();
        readonly Matrix4x4[] matrices=new Matrix4x4[1023];
        Mesh mesh; Material material; CelestialBody body; (int,int,int) cell; PlanetQuality quality;
        void LateUpdate()
        {
            var scene=Visuals.Scene;var sim=scene.Sim;
            if(!SystemInfo.supportsInstancing || !PlanetGraphics.Scatter || scene.Map.Active || sim.Frame==null) return;
            var b=sim.Frame.Body;
            Vector3d camera=sim.Frame.RenderOrigin(sim.RenderAlpha)+(Vector3d)scene.Camera.Cam.transform.position;
            if(b.IsStar || b.Terrain==null) return;
            var dir=b.InertialToBodyFixed(camera,sim.RenderUT).normalized;
            if(camera.magnitude-b.Radius-b.Terrain.Height(dir)>2500) return;
            CubeSphere.FromSphere(dir,out int face,out double u,out double v);
            double grid=40/b.Radius; var key=(face,(int)Math.Floor(u/grid),(int)Math.Floor(v/grid));
            if(body!=b || key!=cell || quality!=PlanetGraphics.Quality) { body=b;cell=key;quality=PlanetGraphics.Quality;Build(grid); }
            if(mesh==null || material==null) return;
            int count=0;var rotation=b.RotationAtUT(sim.RenderUT);var origin=sim.Frame.RenderOrigin(sim.RenderAlpha);
            foreach(var stone in stones) {
                Vector3 p=(Vector3)(rotation*stone.Position-origin);
                float distance=(p-scene.Camera.Cam.transform.position).sqrMagnitude;
                if(distance>650*650 || distance<20*20) continue;
                matrices[count++]=Matrix4x4.TRS(p,rotation.ToQuaternion()*stone.Rotation,stone.Scale);
            }
            if(count>0) Graphics.DrawMeshInstanced(mesh,0,material,matrices,count,null,ShadowCastingMode.Off,true,Layers.Terrain,scene.Camera.Cam,LightProbeUsage.Off);
        }
        void Build(double grid)
        {
            stones.Clear();
            if(mesh==null) {
                var go=GameObject.CreatePrimitive(PrimitiveType.Sphere);mesh=Instantiate(go.GetComponent<MeshFilter>().sharedMesh);Destroy(go);
                mesh.name="Visual stone";var vertices=mesh.vertices;
                for(int i=0;i<vertices.Length;i++) { var p=vertices[i]; float n=Mathf.PerlinNoise(p.x*8+17,p.z*8+31);vertices[i]=Vector3.Scale(p*(.75f+n*.4f),new Vector3(1,.7f,.85f)); }
                mesh.vertices=vertices;mesh.RecalculateNormals();mesh.RecalculateBounds();
            }
            if(material!=null) Destroy(material);
            material=new Material(Shader.Find("Universal Render Pipeline/Lit")) { name="Surface stones",enableInstancing=true };
            // Source textures remain imported assets, shared by all instances.
            if(Visuals.Library!=null) material.SetTexture("_BaseMap",body.HasAtmosphere?Visuals.Library.TellusStone:Visuals.Library.LumaStone);
            material.SetColor("_BaseColor",body.HasAtmosphere?new Color(.38f,.36f,.30f):new Color(.55f,.55f,.56f));material.SetFloat("_Smoothness",.08f);
            int extent=quality==PlanetQuality.Low?4:quality==PlanetQuality.Medium?7:quality==PlanetQuality.High?10:12;
            for(int y=-extent;y<=extent;y++) for(int x=-extent;x<=extent;x++) {
                int a=cell.Item2+x,c=cell.Item3+y;
                unchecked {
                    uint hash=(uint)(a*73856093^c*19349663^cell.Item1*83492791);hash^=hash>>13;hash*=1274126177;
                    double uu=(a+.15+(hash&255)/365.0)*grid,vv=(c+.15+((hash>>8)&255)/365.0)*grid;
                    if(uu<-1 || uu>1 || vv<-1 || vv>1) continue;
                    var d=CubeSphere.ToSphere(cell.Item1,uu,vv);double h=body.Terrain.Height(d);
                    if(body.HasOcean && h<2) continue;
                    var biome=body.Terrain.BiomeAt(d); if(biome.Material==8 || biome.Material==6) continue;
                    float size=.3f+((hash>>16)&255)/255f*2.4f;
                    var up=(Vector3)d;
                    stones.Add(new Stone { Position=d*(body.Radius+h+size*.1),Rotation=Quaternion.FromToRotation(Vector3.up,up)*Quaternion.Euler(0,hash%360,0),Scale=new Vector3(size*1.4f,size,size) });
                }
            }
        }
        void OnDestroy() { if(mesh!=null) Destroy(mesh);if(material!=null) Destroy(material); }
    }
}
