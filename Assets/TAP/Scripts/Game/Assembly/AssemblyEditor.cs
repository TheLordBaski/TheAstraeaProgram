using System;
using System.Collections.Generic;
using TAP.Construction;
using TAP.Core;
using TAP.Parts;
using TAP.Persistence;
using UnityEngine;

namespace TAP.Game
{
    /// <summary>Links a scene object in the assembly building to its index in the craft design.</summary>
    public sealed class EditorPartTag : MonoBehaviour
    {
        public int Index;
        /// <summary>0 = the craft, k + 1 = detached (set-aside) group k.</summary>
        public int Assembly;
    }

    /// <summary>
    /// Interactive vehicle assembly: pick parts from the palette or the craft, snap them to free stack
    /// nodes or attach them to surfaces (with radial symmetry and angle snapping), rotate, delete, undo/redo,
    /// custom staging. All attachment math works on the CraftDesign records (root-part space); the scene
    /// objects are rebuilt from the design after each change.
    /// </summary>
    public sealed partial class AssemblyEditor : MonoBehaviour
    {
        public static AssemblyEditor Instance { get; private set; }
        public PartDatabase Db;
        public CraftDesign Design;
        public CraftAssembler Asm;
        public Camera Cam;
        public EditorCamera CameraRig;
        public Transform CraftRoot;
        public readonly List<GameObject> PartObjects = new List<GameObject>();
        public int Symmetry = 1;
        public bool AngleSnap = true;
        /// <summary>When true, staging is recomputed automatically after every change.</summary>
        public bool AutoStaging = true;
        public bool ShowMarkers = true;
        public float FloorHeight = 0f;

        /// <summary>Raised after every change of the design (parts, staging, name).</summary>
        public event Action DesignChanged;
        /// <summary>Short user feedback messages (toasts).</summary>
        public event Action<string> Message;
        /// <summary>Right-click on a placed part (index).</summary>
        public event Action<int> PartContextRequested;
        /// <summary>Set by the UI: true while the pointer is over an interactive UI element.</summary>
        public Func<bool> PointerOverUi = () => false;

        public static readonly int[] SymmetryModes = { 1, 2, 3, 4, 6, 8 };

        // Held parts: a subtree being moved, a copy, or a new part from the palette.
        // Poses are relative to the held root (root at the origin, identity rotation).
        private List<PartNodeRecord> _held;
        private bool _heldFromCraft;
        /// <summary>The root was picked up: the whole craft is held and the craft part list is empty.</summary>
        private bool _heldWholeCraft;
        private readonly List<List<GameObject>> _ghosts = new List<List<GameObject>>();
        private Quaternion _heldRotation = Quaternion.identity;
        private readonly List<string> _undo = new List<string>();
        private readonly List<string> _redo = new List<string>();

        public bool IsHolding => _held != null;
        public string HeldPartId => _held != null ? _held[0].partId : null;
        public bool CanUndo => _undo.Count > 0;
        public bool CanRedo => _redo.Count > 0;

        private void Awake()
        {
            Instance = this;
            Db = PartDatabase.Instance;
            Design = new CraftDesign();
            Asm = new CraftAssembler(Design, Db);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Init(Camera cam, EditorCamera rig)
        {
            Cam = cam;
            CameraRig = rig;
            CraftRoot = new GameObject("Craft").transform;
            if (GameSession.EditorCraft != null && GameSession.EditorCraft.parts.Count > 0)
                LoadDesign(SaveStorage.DeepClone(GameSession.EditorCraft), false, false);
            else
                NewCraft(false);
            FrameCraft(true);
        }

        // ------------------------------------------------------------------ design lifecycle

        public void NewCraft(bool undoable = true)
        {
            if (undoable) PushUndo();
            CancelHold();
            Design = new CraftDesign { name = "Untitled Rocket" };
            Asm = new CraftAssembler(Design, Db);
            AutoStaging = true;
            Rebuild();
        }

        public void LoadDesign(CraftDesign d, bool undoable = true, bool announce = true)
        {
            if (undoable) PushUndo();
            CancelHold();
            // Drop parts this build does not know (e.g. a craft file from a newer version).
            int unknown = 0;
            for (int i = d.parts.Count - 1; i >= 1; i--)
                if (Db.Get(d.parts[i].partId) == null) { new CraftAssembler(d, Db).RemoveSubtree(i); unknown++; }
            if (d.parts.Count > 0 && Db.Get(d.parts[0].partId) == null) { d.parts.Clear(); unknown++; }
            Design = d;
            Asm = new CraftAssembler(Design, Db);
            // Detect customised staging (differs from the automatic plan).
            var copy = SaveStorage.DeepClone(d);
            new CraftAssembler(copy, Db).AutoStage();
            AutoStaging = true;
            for (int i = 0; i < d.parts.Count; i++)
                if (copy.parts[i].stage != d.parts[i].stage) { AutoStaging = false; break; }
            Rebuild();
            if (announce) Message?.Invoke(unknown > 0 ? $"Loaded {d.name} ({unknown} unknown parts removed)" : $"Loaded {d.name}");
        }

        public void Rename(string name)
        {
            if (string.IsNullOrWhiteSpace(name) || name.Trim() == Design.name) return;
            PushUndo();
            Design.name = name.Trim();
            StoreSessionCraft();
            DesignChanged?.Invoke();
        }

        public bool Save()
        {
            try
            {
                SaveStorage.SaveCraft(Design);
                StoreSessionCraft();
                Message?.Invoke($"Saved \"{Design.name}\"");
                return true;
            }
            catch (Exception e)
            {
                Message?.Invoke("Save failed: " + e.Message);
                return false;
            }
        }

        /// <summary>Keeps the session copy in sync so leaving and re-entering the building restores the craft.</summary>
        public void StoreSessionCraft()
        {
            GameSession.EditorCraft = SaveStorage.DeepClone(Design);
            if (GameSession.Save != null) GameSession.Save.editorCraft = SaveStorage.DeepClone(Design);
        }

        // ------------------------------------------------------------------ undo / redo

        private void PushUndo()
        {
            _undo.Add(SaveStorage.ToJson(Design));
            if (_undo.Count > 80) _undo.RemoveAt(0);
            _redo.Clear();
        }

        public void Undo()
        {
            if (_undo.Count == 0) return;
            CancelHold();
            _redo.Add(SaveStorage.ToJson(Design));
            RestoreSnapshot(_undo[_undo.Count - 1]);
            _undo.RemoveAt(_undo.Count - 1);
            Message?.Invoke("Undo");
        }

        public void Redo()
        {
            if (_redo.Count == 0) return;
            CancelHold();
            _undo.Add(SaveStorage.ToJson(Design));
            RestoreSnapshot(_redo[_redo.Count - 1]);
            _redo.RemoveAt(_redo.Count - 1);
            Message?.Invoke("Redo");
        }

        private void RestoreSnapshot(string json)
        {
            Design = SaveStorage.FromJson<CraftDesign>(json);
            Asm = new CraftAssembler(Design, Db);
            var copy = SaveStorage.DeepClone(Design);
            new CraftAssembler(copy, Db).AutoStage();
            AutoStaging = true;
            for (int i = 0; i < Design.parts.Count; i++)
                if (copy.parts[i].stage != Design.parts[i].stage) { AutoStaging = false; break; }
            Rebuild();
        }

        // ------------------------------------------------------------------ staging

        /// <summary>Called after the part list changed: refresh staging, rebuild the scene.</summary>
        private void PartsChanged()
        {
            if (AutoStaging) Asm.AutoStage();
            else
            {
                // New stageable parts go into a new first-firing stage (KSP behaviour).
                int max = -1;
                foreach (var p in Design.parts) max = Math.Max(max, p.stage);
                for (int i = 0; i < Design.parts.Count; i++)
                {
                    var def = Db.Get(Design.parts[i].partId);
                    if (def == null) continue;
                    if (!def.IsStageable) Design.parts[i].stage = -1;
                    else if (Design.parts[i].stage < 0) Design.parts[i].stage = max + 1;
                }
                CompactStages();
            }
            Rebuild();
        }

        public void ResetStaging()
        {
            PushUndo();
            AutoStaging = true;
            Asm.AutoStage();
            StoreSessionCraft();
            DesignChanged?.Invoke();
            Message?.Invoke("Staging reset to the automatic order");
        }

        /// <summary>Moves a part (with its symmetry counterparts) to an existing stage.</summary>
        public void MoveToStage(int partIndex, int stage)
        {
            if (partIndex < 0 || partIndex >= Design.parts.Count) return;
            PushUndo();
            AutoStaging = false;
            foreach (int i in SymmetryGroup(partIndex)) Design.parts[i].stage = stage;
            CompactStages();
            StoreSessionCraft();
            DesignChanged?.Invoke();
        }

        /// <summary>
        /// Moves a part into a new stage that fires right after stage <paramref name="afterStage"/>
        /// (inverse numbering: the new stage gets number afterStage and older stages below shift up).
        /// Pass StageCount to create a new first-firing stage.
        /// </summary>
        public void MoveToNewStage(int partIndex, int afterStage)
        {
            if (partIndex < 0 || partIndex >= Design.parts.Count) return;
            PushUndo();
            AutoStaging = false;
            var group = SymmetryGroup(partIndex);
            foreach (var p in Design.parts) if (p.stage >= afterStage) p.stage++;
            foreach (int i in group) Design.parts[i].stage = afterStage;
            CompactStages();
            StoreSessionCraft();
            DesignChanged?.Invoke();
        }

        public void CompactStages()
        {
            var model = Asm.ToStageModel();
            for (int i = 0; i < model.Count; i++) model[i].Stage = Design.parts[i].stage;
            StagingPlanner.Compact(model);
            for (int i = 0; i < model.Count; i++) Design.parts[i].stage = model[i].Stage;
        }

        public int StageCount
        {
            get
            {
                int max = -1;
                foreach (var p in Design.parts) max = Math.Max(max, p.stage);
                return max + 1;
            }
        }

        public List<int> SymmetryGroup(int index) => SymmetryGroup(Design.parts, index);

        private static List<int> SymmetryGroup(List<PartNodeRecord> parts, int index)
        {
            var list = new List<int> { index };
            int g = parts[index].symmetryGroup;
            if (g < 0) return list;
            for (int i = 0; i < parts.Count; i++)
                if (i != index && parts[i].symmetryGroup == g) list.Add(i);
            return list;
        }

        // ------------------------------------------------------------------ per-part settings

        public double GetSetting(int index, string key, double fallback)
        {
            var s = Design.parts[index].settings;
            return s != null && s.TryGetValue(key, out double v) ? v : fallback;
        }

        public void SetSetting(int index, string key, double value)
        {
            if (index < 0 || index >= Design.parts.Count) return;
            PushUndo();
            foreach (int i in SymmetryGroup(index))
            {
                var r = Design.parts[i];
                if (r.settings == null) r.settings = new Dictionary<string, double>();
                r.settings[key] = value;
            }
            StoreSessionCraft();
            DesignChanged?.Invoke();
        }

        // ------------------------------------------------------------------ scene objects

        public void Rebuild()
        {
            foreach (var go in PartObjects) if (go != null) Destroy(go);
            PartObjects.Clear();
            for (int i = 0; i < Design.parts.Count; i++)
            {
                var pr = Design.parts[i];
                var def = Db.Get(pr.partId);
                if (def == null) { PartObjects.Add(null); continue; }
                var go = PartModelFactory.Build(def, Layers.EditorParts);
                go.transform.SetParent(CraftRoot, false);
                go.transform.localPosition = CraftAssembler.V(pr.pos);
                go.transform.localRotation = CraftAssembler.Q(pr.rot);
                SetLayerRecursive(go, Layers.EditorParts);
                go.AddComponent<EditorPartTag>().Index = i;
                PrepareModel(go, def);
                PartObjects.Add(go);
            }
            // Stand the craft on the floor of the building.
            float lowest = Design.parts.Count > 0 ? LaunchService.DesignLowestPoint(Design, Db) : 0;
            CraftRoot.position = new Vector3(0, FloorHeight - lowest + 0.25f, 0);
            RebuildDetached();
            Physics.SyncTransforms();
            _highlightDirty = true;
            StoreSessionCraft();
            DesignChanged?.Invoke();
        }

        /// <summary>Puts animated rigs into their launch configuration (legs stowed).</summary>
        private static void PrepareModel(GameObject go, PartDefinition def)
        {
            if (def.landingLeg != null)
            {
                var pivot = go.transform.Find("Model/LegPivot");
                if (pivot != null)
                    pivot.localRotation = def.landingLeg.startDeployed
                        ? Quaternion.AngleAxis(-def.landingLeg.splayDeg, Vector3.right)
                        : Quaternion.AngleAxis(-172f, Vector3.right);
            }
        }

        public static void SetLayerRecursive(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform t in go.transform) SetLayerRecursive(t.gameObject, layer);
        }

        public Bounds CraftBounds()
        {
            var b = new Bounds(CraftRoot != null ? CraftRoot.position : Vector3.zero, Vector3.one * 2);
            bool first = true;
            foreach (var go in PartObjects)
            {
                if (go == null) continue;
                foreach (var r in go.GetComponentsInChildren<Renderer>())
                {
                    if (first) { b = r.bounds; first = false; }
                    else b.Encapsulate(r.bounds);
                }
            }
            return b;
        }

        public void FrameCraft(bool instant = false)
        {
            if (CameraRig == null) return;
            // An empty floor gets a comfortable overview of the build platform.
            var b = Design.parts.Count > 0 ? CraftBounds() : new Bounds(new Vector3(0, 3.5f, 0), new Vector3(6, 7, 6));
            CameraRig.Frame(b, instant);
        }

        // ------------------------------------------------------------------ holding

        public void PickFromPalette(PartDefinition def)
        {
            CancelHold();
            _held = new List<PartNodeRecord>
            {
                new PartNodeRecord { partId = def.id, parent = -1, pos = CraftAssembler.A(Vector3.zero), rot = CraftAssembler.A(Quaternion.identity) },
            };
            _heldFromCraft = false;
            _heldRotation = Quaternion.identity;
            BuildGhosts(1);
        }

        /// <summary>Detaches a placed part (with its subtree and its symmetry counterparts) and holds it.</summary>
        public void PickUp(int index)
        {
            if (index < 0 || index >= Design.parts.Count) return;
            if (index == 0)
            {
                PickUpWholeCraft();
                return;
            }
            PushUndo();
            var group = SymmetryGroup(index);
            _held = ExtractSubtree(index, out _heldRotation);
            _heldFromCraft = true;
            if (group.Count > 1) Symmetry = group.Count;
            group.Sort((a, b) => b.CompareTo(a)); // remove the highest indices first so the others stay valid
            foreach (int g in group) Asm.RemoveSubtree(g);
            PartsChanged();
            BuildGhosts(Math.Max(1, group.Count));
        }

        /// <summary>Holds a copy of a placed part's subtree (Alt+click).</summary>
        public void CopyPart(int index)
        {
            if (index < 0 || index >= Design.parts.Count) return;
            CancelHold();
            _held = ExtractSubtree(index, out _heldRotation);
            foreach (var r in _held) { r.uid = 0; r.symmetryGroup = -1; r.stage = -1; }
            _heldFromCraft = false;
            BuildGhosts(1);
            Message?.Invoke("Copied " + (Db.Get(_held[0].partId)?.title ?? "part"));
        }

        /// <summary>Copies the subtree of a craft part with poses relative to that part.</summary>
        private List<PartNodeRecord> ExtractSubtree(int index, out Quaternion rootRot) => ExtractSubtree(Design.parts, index, out rootRot);

        /// <summary>Copies the subtree of a part of a part list with poses relative to that part.</summary>
        private static List<PartNodeRecord> ExtractSubtree(List<PartNodeRecord> parts, int index, out Quaternion rootRot)
        {
            var root = parts[index];
            Vector3 rootPos = CraftAssembler.V(root.pos);
            rootRot = CraftAssembler.Q(root.rot);
            var sub = new List<int>();
            CollectSubtree(parts, index, sub);
            var map = new Dictionary<int, int>();
            var held = new List<PartNodeRecord>();
            Quaternion inv = Quaternion.Inverse(rootRot);
            foreach (int i in sub)
            {
                var r = parts[i];
                var c = SaveStorage.DeepClone(r);
                map[i] = held.Count;
                c.pos = CraftAssembler.A(inv * (CraftAssembler.V(r.pos) - rootPos));
                c.rot = CraftAssembler.A(inv * CraftAssembler.Q(r.rot));
                held.Add(c);
            }
            foreach (var c in held) c.parent = c.parent >= 0 && map.TryGetValue(c.parent, out int np) ? np : -1;
            held[0].parent = -1;
            held[0].parentNode = null;
            return held;
        }

        private static void CollectSubtree(List<PartNodeRecord> parts, int index, List<int> list)
        {
            list.Add(index);
            for (int i = 0; i < parts.Count; i++)
                if (parts[i].parent == index) CollectSubtree(parts, i, list);
        }

        /// <summary>Removes a placed part, its subtree and its symmetry counterparts.</summary>
        public void DeletePart(int index)
        {
            if (index < 0 || index >= Design.parts.Count) return;
            if (index == 0)
            {
                Message?.Invoke("The root part can't be deleted: use New to start over.");
                return;
            }
            PushUndo();
            var group = SymmetryGroup(index);
            group.Sort((a, b) => b.CompareTo(a));
            foreach (int g in group) Asm.RemoveSubtree(g);
            PartsChanged();
            Message?.Invoke("Part deleted (Ctrl+Z to undo)");
        }

        public void CancelHold()
        {
            _held = null;
            _heldWholeCraft = false;
            foreach (var g in _ghosts) foreach (var go in g) if (go != null) Destroy(go);
            _ghosts.Clear();
            HideNodeMarkers();
        }

        /// <summary>Discards the held part (dropping it on the parts list, Delete or Esc).</summary>
        public void DiscardHeld()
        {
            if (_held == null) return;
            bool fromCraft = _heldFromCraft, whole = _heldWholeCraft;
            CancelHold();
            Rebuild();
            Message?.Invoke(whole ? "Craft deleted (Ctrl+Z to undo)" : fromCraft ? "Parts removed (Ctrl+Z to undo)" : "Part discarded");
        }

        private void BuildGhosts(int copies)
        {
            foreach (var g in _ghosts) foreach (var go in g) if (go != null) Destroy(go);
            _ghosts.Clear();
            EnsureGhostCopies(copies);
        }

        private void EnsureGhostCopies(int copies)
        {
            if (_held == null) return;
            while (_ghosts.Count < copies)
            {
                var list = new List<GameObject>();
                foreach (var r in _held)
                {
                    var def = Db.Get(r.partId);
                    var go = PartModelFactory.Build(def, Layers.Ghost);
                    SetLayerRecursive(go, Layers.Ghost);
                    foreach (var col in go.GetComponentsInChildren<Collider>()) col.enabled = false;
                    PrepareModel(go, def);
                    list.Add(go);
                }
                _ghosts.Add(list);
            }
            for (int c = 0; c < _ghosts.Count; c++)
                foreach (var go in _ghosts[c])
                    if (go != null && go.activeSelf != (c < copies)) go.SetActive(c < copies);
        }

        public void CycleSymmetry(int dir)
        {
            int i = Array.IndexOf(SymmetryModes, Symmetry);
            if (i < 0) i = 0;
            i = (i + dir + SymmetryModes.Length) % SymmetryModes.Length;
            Symmetry = SymmetryModes[i];
            DesignChanged?.Invoke();
        }

        public void ToggleAngleSnap()
        {
            AngleSnap = !AngleSnap;
            DesignChanged?.Invoke();
        }

        public void ToggleMarkers()
        {
            ShowMarkers = !ShowMarkers;
            DesignChanged?.Invoke();
        }
    }
}
