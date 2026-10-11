using System;
using System.Collections;
using System.IO;
using TAP.Game;
using UnityEngine;
using UnityEngine.UIElements;

namespace TAP.UI
{
    /// <summary>
    /// <i>Report a problem</i> and the exception notice (FND-12). The dialog takes a screenshot first (before it covers the game), asks what
    /// happened, writes the zip (<see cref="BugReport"/>) and offers to open its folder and the feedback page. An unhandled exception shows a
    /// small notice in the corner that does not stop the game: write a report, or dismiss it.
    /// </summary>
    public sealed class ProblemReportUI : MonoBehaviour
    {
        private static ProblemReportUI _instance;
        private VisualElement _dialog, _notice;
        private Label _noticeText, _status, _intro;
        private TextField _note;
        private Button _create, _openFolder, _openFeedback;
        private byte[] _screenshot;
        private string _reportPath;
        private float _noticeUntil;

        public static bool IsOpen => _instance != null && UIKit.Shown(_instance._dialog);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (_instance != null) return;
            var doc = UIKit.CreateDocument("ProblemReport", 800, null, "Problems");
            DontDestroyOnLoad(doc.gameObject);
            var ui = doc.gameObject.AddComponent<ProblemReportUI>();
            _instance = ui;
            ui.Build(doc.rootVisualElement);
            GameLog.ExceptionRaised += ui.OnException;
        }

        private void OnDestroy()
        {
            GameLog.ExceptionRaised -= OnException;
            if (_instance == this) _instance = null;
        }

        private void Build(VisualElement root)
        {
            // the notice: a small panel in the corner
            _notice = UIKit.Element("panel panel--deep problem-notice");
            _noticeText = UIKit.Label("", "wrap problem-notice__text");
            var row = UIKit.Element("problem-notice__row");
            row.Add(UIKit.Button("Write a report", () => { UIKit.Show(_notice, false); Open(NoticeNote()); }, "problem-btn"));
            row.Add(UIKit.Button("Dismiss", () => UIKit.Show(_notice, false), "problem-btn"));
            _notice.Add(_noticeText);
            _notice.Add(row);
            UIKit.Show(_notice, false);
            root.Add(_notice);

            // the dialog
            _dialog = UIKit.Element("screen dialog");
            var panel = UIKit.Element("panel panel--deep dialog__panel problem-panel");
            var head = UIKit.Element("dialog__head");
            head.Add(UIKit.Label("REPORT A PROBLEM", "dialog__title"));
            head.Add(UIKit.Button("Close", Close, "dialog__close"));
            panel.Add(head);
            _intro = UIKit.Label("This saves one file with the logs, your game as it is now, a screenshot and your settings. Nothing is sent: " +
                                 "attach the file when you describe the problem on the feedback page.", "wrap text-dim problem-intro");
            panel.Add(_intro);
            panel.Add(UIKit.Label("What happened?", "problem-label"));
            _note = UIKit.Field("", "problem-note");
            _note.multiline = true;
            panel.Add(_note);
            _status = UIKit.Label("", "wrap problem-status");
            panel.Add(_status);
            var buttons = UIKit.Element("problem-notice__row");
            _create = UIKit.Button("Create the report", CreateReport, "problem-btn btn--go");
            _openFolder = UIKit.Button("Open the folder", () => Application.OpenURL("file:///" + Path.GetDirectoryName(_reportPath).Replace('\\', '/')), "problem-btn");
            _openFeedback = UIKit.Button("Open the feedback page", () => Application.OpenURL(GameLog.FeedbackUrl), "problem-btn");
            buttons.Add(_create);
            buttons.Add(_openFolder);
            buttons.Add(_openFeedback);
            panel.Add(buttons);
            _dialog.Add(panel);
            UIKit.Show(_dialog, false);
            root.Add(_dialog);
        }

        private string _lastException;

        private void OnException(GameLog.Entry e)
        {
            _lastException = e.Message.Split('\n')[0];
            _noticeText.text = "Something went wrong: " + Loc.Name(_lastException);
            UIKit.Show(_notice, true);
            _noticeUntil = Time.unscaledTime + 30f;
        }

        private string NoticeNote() => string.IsNullOrEmpty(_lastException) ? "" : "Exception: " + _lastException + "\n\n";

        private void Update()
        {
            GameLog.Pump();
            if (UIKit.Shown(_notice) && Time.unscaledTime > _noticeUntil) UIKit.Show(_notice, false);
        }

        // ------------------------------------------------------------------ the dialog

        /// <summary>Opens the dialog; the screenshot is taken before it appears.</summary>
        public static void Open(string note = "")
        {
            if (_instance == null) return;
            _instance.StartCoroutine(_instance.OpenAfterScreenshot(note));
        }

        public static void Close()
        {
            if (_instance != null) UIKit.Show(_instance._dialog, false);
        }

        private IEnumerator OpenAfterScreenshot(string note)
        {
            // The dialog is not up yet; wait for the frame to be drawn, then take the picture.
            yield return new WaitForEndOfFrame();
            _screenshot = null;
            try
            {
                var tex = ScreenCapture.CaptureScreenshotAsTexture();
                if (tex != null) { _screenshot = tex.EncodeToPNG(); Destroy(tex); }
            }
            catch (Exception e) { Debug.LogWarning("The screenshot for the report failed: " + e.Message); }
            _note.value = note ?? "";
            _status.text = "";
            _reportPath = null;
            _create.SetEnabled(true);
            UIKit.Show(_openFolder, false);
            UIKit.Show(_openFeedback, false);
            UIKit.Show(_dialog, true);
        }

        private void CreateReport()
        {
            try
            {
                _reportPath = BugReport.Create(_note.value, _screenshot);
                _status.text = "Saved " + Loc.Name(_reportPath) + "\nAttach it on the feedback page.";
                _create.SetEnabled(false);
                UIKit.Show(_openFolder, true);
                UIKit.Show(_openFeedback, true);
            }
            catch (Exception e)
            {
                _status.text = "The report could not be saved: " + Loc.Name(e.Message);
            }
        }
    }
}
