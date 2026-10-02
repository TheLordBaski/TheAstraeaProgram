using TAP.Game;
using TMPro;
using UnityEngine;

namespace TAP.UI
{
    public sealed class PlanetGraphicsPanel : MonoBehaviour
    {
        UIKit.ButtonRef preset,clouds,scatter;
        public static PlanetGraphicsPanel Create(Transform parent)
        {
            var panel=UIKit.Panel(parent,"Planet graphics");
            UIKit.Place(panel.rectTransform,new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(520,390));UIKit.VLayout(panel,12,24);
            var ui=panel.gameObject.AddComponent<PlanetGraphicsPanel>();
            UIKit.Size(UIKit.Label(panel.transform,"PLANET GRAPHICS",26,UIKit.Accent,TextAlignmentOptions.Center,FontStyles.Bold),44);
            ui.preset=UIKit.Button(panel.transform,"Quality",()=>PlanetGraphics.SetQuality((PlanetQuality)(((int)PlanetGraphics.Quality+1)%4)),19);UIKit.Size(ui.preset.Button,46);
            ui.clouds=UIKit.Button(panel.transform,"Clouds",()=>PlanetGraphics.SetClouds((PlanetCloudMode)(((int)PlanetGraphics.Clouds+1)%3)),19);UIKit.Size(ui.clouds.Button,46);
            ui.scatter=UIKit.Button(panel.transform,"Surface rocks",()=>PlanetGraphics.SetScatter(!PlanetGraphics.Scatter),19);UIKit.Size(ui.scatter.Button,46);
            var note=UIKit.Label(panel.transform,"Changes apply immediately and are saved.\nMap view uses cloud layers. Biome view hides clouds.",16,UIKit.TextDim,TextAlignmentOptions.Center);UIKit.Size(note,54);
            var close=UIKit.Button(panel.transform,"Back",()=>panel.gameObject.SetActive(false),18);UIKit.Size(close.Button,42);
            PlanetGraphics.Changed+=ui.Refresh;ui.Refresh();panel.gameObject.SetActive(false);return ui;
        }
        void Refresh() { preset.Text.text="Quality: "+PlanetGraphics.Quality;clouds.Text.text="Clouds: "+PlanetGraphics.Clouds;scatter.Text.text="Surface rocks: "+(PlanetGraphics.Scatter?"On":"Off"); }
        void OnDestroy() { PlanetGraphics.Changed-=Refresh; }
    }
}
