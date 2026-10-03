using System;
using NUnit.Framework;
using TAP.Core;
using TAP.Game;
using TAP.Simulation;
using UnityEngine;

namespace TAP.Tests
{
    public class PlanetVisualTests
    {
        [Test] public void CloudHandoverUsesAltitudeAndIsAlreadyTextureOnlyAtCameraSwitch()
        {
            const double r=600000;
            Assert.That(PlanetVisualMath.CloudVolumeBlend(r+12000,r),Is.EqualTo(1));
            Assert.That(PlanetVisualMath.CloudVolumeBlend(r+42000,r),Is.EqualTo(0).Within(1e-6));
            float previous=1;
            for(int i=0;i<=100;i++) {
                float value=PlanetVisualMath.CloudVolumeBlend(r+12000+i*300,r);
                Assert.That(value,Is.InRange(0,previous));previous=value;
            }
            foreach(double switchRadius in new[]{6d,6.6d}) {
                Assert.That(PlanetVisualMath.CloudVolumeBlend(r*switchRadius,r),Is.Zero);
                Assert.That(Math.Abs(PlanetVisualMath.CloudVolumeBlend(r*switchRadius-1,r)-PlanetVisualMath.CloudVolumeBlend(r*switchRadius+1,r)),Is.LessThan(1e-5));
            }
        }
        [Test] public void EffectCullingKeepsTheAtmosphericLimbButRejectsBodiesOutsideTheView()
        {
            var go=new GameObject("Planet effect frustum");var camera=go.AddComponent<Camera>();camera.fieldOfView=60;camera.aspect=1;camera.nearClipPlane=.1f;camera.farClipPlane=100;
            try {
                var planes=GeometryUtility.CalculateFrustumPlanes(camera);
                Assert.That(PlanetVisualMath.BodyInFrustum(planes,new Vector3(0,0,10),1),Is.True);
                Assert.That(PlanetVisualMath.BodyInFrustum(planes,new Vector3(7,0,10),2),Is.True);
                Assert.That(PlanetVisualMath.BodyInFrustum(planes,new Vector3(20,0,10),2),Is.False);
                Assert.That(PlanetVisualMath.BodyInFrustum(planes,new Vector3(0,0,-10),2),Is.False);
                Assert.That(PlanetVisualMath.BodyInFrustum(planes,Vector3.zero,3),Is.True);
            } finally {UnityEngine.Object.DestroyImmediate(go);}
        }
        [Test] public void BakeKeysInvalidateOnlyAffectedAssetsAndIgnoreLocale()
        {
            var body=CelestialSystem.LoadFromResources().HomeBody;var p=PlanetVisualProfile.Fallback(body);
            var culture=System.Globalization.CultureInfo.CurrentCulture;
            try {
                var surface=p.ExpectedSurfaceKey(body);var weather=p.ExpectedWeatherKey();var atmosphere=p.ExpectedAtmosphereKey(body);
                System.Globalization.CultureInfo.CurrentCulture=new System.Globalization.CultureInfo("sk-SK");
                Assert.That(p.ExpectedAtmosphereKey(body),Is.EqualTo(atmosphere));
                p.WeatherSeed++;
                Assert.That(p.ExpectedWeatherKey(),Is.Not.EqualTo(weather));
                var seededWeather=p.ExpectedWeatherKey();p.CloudCoverage=.2f;
                Assert.That(p.ExpectedWeatherKey(),Is.Not.EqualTo(seededWeather));
                foreach(var field in new[]{nameof(p.LargeCloudBanks),nameof(p.SmallCloudPatches),nameof(p.ThinCloudFronts)}) {
                    var beforeCountChange=p.ExpectedWeatherKey();var count=typeof(PlanetVisualProfile).GetField(field);
                    count.SetValue(p,(int)count.GetValue(p)+1);
                    Assert.That(p.ExpectedWeatherKey(),Is.Not.EqualTo(beforeCountChange),field+" must invalidate weather");
                }
                var previous=p.ExpectedWeatherKey();p.WeatherSourceFile="Art/Clouds/source.png";
                Assert.That(p.ExpectedWeatherKey(),Is.Not.EqualTo(previous));
                previous=p.ExpectedWeatherKey();p.WeatherSourceHash="changed source bytes";
                Assert.That(p.ExpectedWeatherKey(),Is.Not.EqualTo(previous));
                previous=p.ExpectedWeatherKey();p.CloudRelief+=500;
                Assert.That(p.ExpectedWeatherKey(),Is.Not.EqualTo(previous));
                previous=p.ExpectedWeatherKey();p.SurfaceSaturation=.5f;p.SurfaceNormalStrength=2;
                Assert.That(p.ExpectedWeatherKey(),Is.EqualTo(previous),"Live surface controls must not invalidate weather");
                Assert.That(p.ExpectedSurfaceKey(body),Is.EqualTo(surface));Assert.That(p.ExpectedAtmosphereKey(body),Is.EqualTo(atmosphere));
                p.Mie*=2;
                Assert.That(p.ExpectedAtmosphereKey(body),Is.Not.EqualTo(atmosphere));
                Assert.That(p.ExpectedSurfaceKey(body,2048),Is.Not.EqualTo(surface));
                p.Version++;
                Assert.That(p.ExpectedSurfaceKey(body),Is.Not.EqualTo(surface));
            } finally { System.Globalization.CultureInfo.CurrentCulture=culture;UnityEngine.Object.DestroyImmediate(p); }
        }
        [Test] public void NewOverlaySeparatesLocalAndScaledDepth()
        {
            var go=new GameObject("Stack depth contract");go.AddComponent<Camera>();
            try {
                var data=go.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
                data.renderType=UnityEngine.Rendering.Universal.CameraRenderType.Overlay;
                Assert.That(data.clearDepth,Is.True);
            } finally { UnityEngine.Object.DestroyImmediate(go); }
        }
        [Test] public void CameraRayIncludesUnityViewReflectionAndIgnoresFloatingOrigin()
        {
            var go=new GameObject("Camera contract");var camera=go.AddComponent<Camera>();
            try {
                camera.transform.SetPositionAndRotation(new Vector3(125,-77,38),Quaternion.Euler(20,83,-13));
                var initial=PlanetVisualMath.ViewProjection(camera);
                var inverse=initial.inverse;
                var p=inverse*new Vector4(0,0,1,1);
                var ray=new Vector3(p.x,p.y,p.z).normalized;
                Assert.That(Vector3.Dot(ray,camera.transform.forward),Is.GreaterThan(.99999f));
                camera.transform.position+=new Vector3(18000,5000,-22000);
                Assert.That(PlanetVisualMath.ViewProjection(camera),Is.EqualTo(initial));
            } finally { UnityEngine.Object.DestroyImmediate(go); }
        }
        [Test] public void SurfaceUVMatchesMapAtEveryLatitudeAndDateline()
        {
            for(int y=0;y<16;y++) for(int x=0;x<32;x++) {
                PlanetVisualMath.SurfaceUv(PlanetMaps.DirectionOf(x,y,32,16),out var u,out var v);
                Assert.That(u,Is.EqualTo((x+.5)/32).Within(1e-8));Assert.That(v,Is.EqualTo((y+.5)/16).Within(1e-8));
            }
        }
        [Test] public void ShellIntersectionHandlesSurfaceOrbitAndMiss()
        {
            Assert.That(PlanetVisualMath.RaySphere(new Vector3d(600010,0,0),new Vector3d(-1,0,0),600000,out var near,out var far),Is.True);
            Assert.That(near,Is.EqualTo(10).Within(1e-6));Assert.That(far,Is.EqualTo(1200010).Within(1e-6));
            Assert.That(PlanetVisualMath.RaySphere(new Vector3d(0,700000,0),Vector3d.right,600000,out _,out _),Is.False);
        }
        [Test] public void WindRemainsPeriodicAtLargeUTAndAcrossNegativeTime()
        {
            Assert.That(PlanetVisualMath.WindOffset(1e12,18,16000),Is.InRange(0,1));
            double cycle=16000.0/18;
            Assert.That(PlanetVisualMath.WindOffset(12345+cycle,18,16000),Is.EqualTo(PlanetVisualMath.WindOffset(12345,18,16000)).Within(1e-9));
            Assert.That(PlanetVisualMath.WindOffset(double.NaN,18,16000),Is.Zero);
        }
        [Test] public void SkyTablesAreFiniteAndReddenTowardsTheHorizon()
        {
            var body=CelestialSystem.LoadFromResources().HomeBody;var p=PlanetVisualProfile.Fallback(body);
            try {
                var a=AtmosphereLuts.Params.Of(body,p);
                var t=AtmosphereLuts.Transmittance(a);
                var up=AtmosphereLuts.SampleTransmittance(t,a.Bottom,1,a);
                var low=AtmosphereLuts.SampleTransmittance(t,a.Bottom,.05,a);
                Assert.That(up.z,Is.InRange(.5,1));Assert.That(low.z,Is.LessThan(up.z));
                Assert.That(low.x/low.z,Is.GreaterThan(up.x/up.z),"A low sun must be redder");
                Assert.That(AtmosphereLuts.SampleTransmittance(t,a.Bottom,-.2,a).magnitude,Is.Zero.Within(1e-9),"Below the horizon the planet blocks the sun");
                Assert.That(AtmosphereLuts.SampleTransmittance(t,a.Top,.2,a).x,Is.GreaterThan(.9));
                // Straight up from the ground and grazing the top are the table's two corners (texel centres).
                var corner=AtmosphereLuts.TransmittanceUv(a.Bottom,1,a.Bottom,a.Top);
                Assert.That(corner.x,Is.EqualTo(.5f/AtmosphereLuts.TransmittanceWidth).Within(1e-5));
                Assert.That(corner.y,Is.EqualTo(.5f/AtmosphereLuts.TransmittanceHeight).Within(1e-5));
                var ms=AtmosphereLuts.MultiScattering(a,t);
                foreach(var c in ms) Assert.That(float.IsFinite(c.r)&&float.IsFinite(c.g)&&float.IsFinite(c.b)&&c.r>=0&&c.b>=0,Is.True);
                int n=AtmosphereLuts.MultiSize;
                Assert.That(ms[n-1].b,Is.GreaterThan(ms[0].b*10),"Noon skylight must exceed the night side");
            } finally { UnityEngine.Object.DestroyImmediate(p); }
        }
        [Test] public void GeneratedSkyTablesCarryACurrentKey()
        {
            var body=CelestialSystem.LoadFromResources().HomeBody;var p=PlanetVisualProfile.Fallback(body);
            try {
                Assert.That(p.GenerateSkyTables(body),Is.True);
                Assert.That(p.Transmittance.width,Is.EqualTo(AtmosphereLuts.TransmittanceWidth));
                Assert.That(p.MultiScattering.width,Is.EqualTo(AtmosphereLuts.MultiSize));
                Assert.That(p.AtmosphereBakeKey,Is.EqualTo(p.ExpectedAtmosphereKey(body)));
                var airless=PlanetVisualProfile.Fallback(CelestialSystem.LoadFromResources().Get("luma"));
                try { Assert.That(airless.GenerateSkyTables(CelestialSystem.LoadFromResources().Get("luma")),Is.False); }
                finally { UnityEngine.Object.DestroyImmediate(airless); }
            } finally { UnityEngine.Object.DestroyImmediate(p.Transmittance);UnityEngine.Object.DestroyImmediate(p.MultiScattering);UnityEngine.Object.DestroyImmediate(p); }
        }
        [Test] public void AirlessBodyFallbackHasNoClouds()
        {
            var b=CelestialSystem.LoadFromResources().Get("luma");var p=PlanetVisualProfile.Fallback(b);
            try { Assert.That(p.CloudCoverage,Is.Zero);Assert.That(p.Lunar,Is.True); } finally { UnityEngine.Object.DestroyImmediate(p); }
        }
        [Test] public void PlumeOverAirWithoutWeatherMapBindsNoCloudTextures()
        {
            // Testworld: air, but no bake, so its generated profile has no weather map.
            var b=Galaxy.LoadFromResources().LoadSystem("debug").Get("testworld");var p=PlanetVisualProfile.Fallback(b);
            var weather=new Texture2D(4,4);var noise=new Texture3D(4,4,4,TextureFormat.R8,false);
            try {
                Assert.That(b.HasAtmosphere,Is.True);Assert.That(p.Weather,Is.Null);
                var block=new MaterialPropertyBlock();
                foreach(PlanetCloudMode clouds in Enum.GetValues(typeof(PlanetCloudMode))) {
                    block.Clear();
                    Assert.DoesNotThrow(()=>PlanetVisualController.BindPlumeClouds(block,p,null,clouds));
                    Assert.DoesNotThrow(()=>PlanetVisualController.BindPlumeClouds(block,p,noise,clouds));
                    Assert.That(block.GetFloat("_PlanetCloudMode"),Is.Zero,clouds.ToString());
                }
                p.Weather=weather;
                PlanetVisualController.BindPlumeClouds(block,p,null,PlanetCloudMode.Volumetric);
                Assert.That(block.GetFloat("_PlanetCloudMode"),Is.EqualTo(1),"volumes need the noise; layers do not");
                PlanetVisualController.BindPlumeClouds(block,p,noise,PlanetCloudMode.Volumetric);
                Assert.That(block.GetFloat("_PlanetCloudMode"),Is.EqualTo(2));
                Assert.That(block.GetTexture("_PlanetWeather"),Is.SameAs(weather));Assert.That(block.GetTexture("_PlanetNoise"),Is.SameAs(noise));
            } finally { UnityEngine.Object.DestroyImmediate(weather);UnityEngine.Object.DestroyImmediate(noise);UnityEngine.Object.DestroyImmediate(p); }
        }
    }
}
