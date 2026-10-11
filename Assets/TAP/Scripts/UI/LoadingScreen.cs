using System.Collections;
using TAP.Game;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace TAP.UI
{
    /// <summary>
    /// Scenes load asynchronously behind this screen (FND-07): a title, a progress bar and a tip. It lives across scene loads
    /// and takes over <see cref="GameSession.AsyncLoader"/> when the game starts.
    /// </summary>
    public sealed class LoadingScreen : MonoBehaviour
    {
        private static LoadingScreen _instance;
        private VisualElement _screen, _bar;
        private Label _title, _tip;
        private int _tipIndex = -1;

        // ponytail: FND-10 moves the tips into the string tables.
        private static readonly string[] Tips =
        {
            "Launching east gives you Tellus's spin as free speed.",
            "Z sets full throttle and X cuts it. Space fires the next stage.",
            "Press M to open the map and plan maneuver nodes on your orbit.",
            "F5 quicksaves. Hold F9 to quickload.",
            "Point the heat shield retrograde for reentry, and stage the parachute below about 18 km.",
            "The pause menu can revert a flight to the moment of launch, or back to the assembly building.",
            "In the assembly building, V shows the centre of mass, thrust and pressure. A stable rocket keeps the centre of pressure below the centre of mass.",
            "Press F1 in flight for the control guide.",
        };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Register()
        {
            _instance = null;
            GameSession.AsyncLoader = Load;
        }

        private static bool Load(string scene)
        {
            if (_instance == null) _instance = Create();
            // A new request while one is running (a scene that can't start sends the player elsewhere) replaces it.
            _instance.StopAllCoroutines();
            _instance.StartCoroutine(_instance.Run(scene));
            return true;
        }

        private static LoadingScreen Create()
        {
            var doc = UIKit.CreateDocument("LoadingScreen", 1000, "Loading");
            DontDestroyOnLoad(doc.gameObject);
            var ls = doc.gameObject.AddComponent<LoadingScreen>();
            var root = doc.rootVisualElement;
            ls._screen = root.Q("loading");
            ls._bar = root.Q("loading-bar");
            ls._title = root.Q<Label>("loading-title");
            ls._tip = root.Q<Label>("loading-tip");
            return ls;
        }

        private static string TitleFor(string scene)
        {
            switch (scene)
            {
                case GameSession.HubScene: return "SPACE CENTER";
                case GameSession.EditorScene: return "ASSEMBLY BUILDING";
                case GameSession.FlightScene: return "FLIGHT";
                default: return "THE ASTRAEA PROGRAM";
            }
        }

        private IEnumerator Run(string scene)
        {
            _title.text = TitleFor(scene);
            _tipIndex = (_tipIndex + 1 + Random.Range(0, Tips.Length - 1)) % Tips.Length;
            _tip.text = Tips[_tipIndex];
            UIKit.SetBar(_bar, 0, UIKit.Accent);
            UIKit.Show(_screen, true);
            // Two frames so the screen is drawn before the scene's own start-up blocks the main thread.
            yield return null;
            yield return null;
            var op = SceneManager.LoadSceneAsync(scene);
            while (!op.isDone)
            {
                UIKit.SetBar(_bar, op.progress / 0.9f, UIKit.Accent);
                yield return null;
            }
            // The new scene has started; let it draw once before the screen goes.
            yield return null;
            yield return null;
            UIKit.Show(_screen, false);
        }
    }
}
