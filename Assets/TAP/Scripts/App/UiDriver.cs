using UnityEngine;
using UnityEngine.UIElements;

namespace TAP.App
{
    /// <summary>What the scripted tests share: finding a screen element by name and pressing a button as a keyboard or controller would.</summary>
    public static class UiDriver
    {
        public static VisualElement Find(string name) => Find<VisualElement>(name);

        public static T Find<T>(string name) where T : VisualElement
        {
            foreach (var doc in Object.FindObjectsByType<UIDocument>())
            {
                if (doc.rootVisualElement == null) continue;
                var e = doc.rootVisualElement.Q<T>(name);
                if (e != null) return e;
            }
            return null;
        }

        /// <summary>The first visible button showing this English text (or its translation, when another language is on).</summary>
        public static Button FindByText(string text, string inDocument = null)
        {
            string shown = TAP.Game.Loc.Translate(text);
            foreach (var doc in Object.FindObjectsByType<UIDocument>())
            {
                if (doc.rootVisualElement == null || (inDocument != null && doc.name != inDocument)) continue;
                foreach (var b in doc.rootVisualElement.Query<Button>().ToList())
                    if ((b.text == text || b.text == shown) && IsShown(b)) return b;
            }
            return null;
        }

        /// <summary>The first visible button with a style class (the "Close" of whichever dialog is open: <c>dialog__close</c>).</summary>
        public static Button FindByClass(string className)
        {
            foreach (var doc in Object.FindObjectsByType<UIDocument>())
            {
                if (doc.rootVisualElement == null) continue;
                foreach (var b in doc.rootVisualElement.Query<Button>(className: className).ToList()) if (IsShown(b)) return b;
            }
            return null;
        }

        private static bool IsShown(VisualElement e)
        {
            for (var p = e; p != null; p = p.parent) if (p.resolvedStyle.display == DisplayStyle.None) return false;
            return true;
        }

        public static void Press(Button b)
        {
            using (var e = NavigationSubmitEvent.GetPooled())
            {
                e.target = b;
                b.SendEvent(e);
            }
        }
    }
}
