using System.Collections.Generic;
using TAP.Construction;
using TAP.Core;
using TAP.Parts;
using TAP.Persistence;
using UnityEngine;

namespace TAP.Game
{
    /// <summary>
    /// Parts set aside in the building (KSP's detached parts): groups dropped in empty space stay where they were
    /// dropped, greyed out and not part of the craft (not launched, not in the readouts or staging). They can be picked
    /// up again, built on, and attached to the craft. Picking up the root part takes the whole craft along, which can be
    /// put back or attached to a set-aside group (that group then becomes part of the craft, its root the new root).
    /// </summary>
    public sealed partial class AssemblyEditor
    {
        /// <summary>A connected group of parts: the craft (Id 0) or set-aside group k (Id k + 1).</summary>
        private struct Asmb
        {
            public int Id;
            public List<PartNodeRecord> Parts;
            /// <summary>World pose of the group's root frame (the craft frame is not rotated).</summary>
            public Vector3 Origin;
            public Quaternion Rotation;
        }

        private Asmb Craft => new Asmb { Id = 0, Parts = Design.parts, Origin = CraftRoot.position, Rotation = Quaternion.identity };

        private Asmb Detached(int k)
        {
            var d = Design.detached[k];
            return new Asmb { Id = k + 1, Parts = d.parts, Origin = CraftAssembler.V(d.pos), Rotation = CraftAssembler.Q(d.rot) };
        }

        private Asmb GetAsmb(int id) => id <= 0 ? Craft : Detached(id - 1);

        private IEnumerable<Asmb> AllAssemblies()
        {
            yield return Craft;
            for (int k = 0; k < Design.detached.Count; k++) yield return Detached(k);
        }

        public int DetachedPartCount
        {
            get
            {
                int n = 0;
                foreach (var d in Design.detached) n += d.parts.Count;
                return n;
            }
        }

        // ------------------------------------------------------------------ scene objects

        private readonly List<GameObject> _detachedRoots = new List<GameObject>();
        /// <summary>Renderers per set-aside group and part.</summary>
        private readonly List<List<Renderer[]>> _detachedRenderers = new List<List<Renderer[]>>();
        private MaterialPropertyBlock _greyBlock, _greyHoverBlock;
        private int _detachedHoverKey = int.MinValue;
        private static readonly Color DetachedGrey = new Color(0.66f, 0.69f, 0.74f, 1f);
        private static readonly Color DetachedGlow = new Color(0.05f, 0.06f, 0.08f);

        private void RebuildDetached()
        {
            foreach (var go in _detachedRoots) if (go != null) Destroy(go);
            _detachedRoots.Clear();
            _detachedRenderers.Clear();
            for (int k = 0; k < Design.detached.Count; k++)
            {
                var a = Detached(k);
                var root = new GameObject($"SetAside {k}");
                root.transform.SetPositionAndRotation(a.Origin, a.Rotation);
                var rends = new List<Renderer[]>();
                for (int j = 0; j < a.Parts.Count; j++)
                {
                    var pr = a.Parts[j];
                    var def = Db.Get(pr.partId);
                    if (def == null) { rends.Add(new Renderer[0]); continue; }
                    var go = PartModelFactory.Build(def, Layers.EditorParts);
                    go.transform.SetParent(root.transform, false);
                    go.transform.localPosition = CraftAssembler.V(pr.pos);
                    go.transform.localRotation = CraftAssembler.Q(pr.rot);
                    SetLayerRecursive(go, Layers.EditorParts);
                    var tag = go.AddComponent<EditorPartTag>();
                    tag.Index = j;
                    tag.Assembly = k + 1;
                    PrepareModel(go, def);
                    rends.Add(go.GetComponentsInChildren<Renderer>());
                }
                _detachedRoots.Add(root);
                _detachedRenderers.Add(rends);
            }
            _detachedHoverKey = int.MinValue;
            UpdateDetachedHighlight();
        }

        /// <summary>Greys out set-aside parts; the subtree under the pointer (what a click picks up) glows.</summary>
        private void UpdateDetachedHighlight()
        {
            int key = HoveredDetachedAsm > 0 && _held == null ? HoveredDetachedAsm * 100000 + HoveredDetachedPart : -1;
            if (key == _detachedHoverKey) return;
            _detachedHoverKey = key;
            if (_greyBlock == null)
            {
                _greyBlock = new MaterialPropertyBlock();
                _greyBlock.SetColor("_BaseColor", DetachedGrey);
                _greyBlock.SetColor("_EmissionColor", DetachedGlow);
                _greyHoverBlock = new MaterialPropertyBlock();
                _greyHoverBlock.SetColor("_BaseColor", DetachedGrey);
                _greyHoverBlock.SetColor("_EmissionColor", HoverGlow);
            }
            for (int k = 0; k < _detachedRenderers.Count; k++)
            {
                var hovered = new HashSet<int>();
                if (key >= 0 && HoveredDetachedAsm == k + 1 && k < Design.detached.Count)
                {
                    var sub = new List<int>();
                    CollectSubtree(Design.detached[k].parts, HoveredDetachedPart, sub);
                    hovered.UnionWith(sub);
                }
                var rends = _detachedRenderers[k];
                for (int j = 0; j < rends.Count; j++)
                    foreach (var r in rends[j])
                        if (r != null) r.SetPropertyBlock(hovered.Contains(j) ? _greyHoverBlock : _greyBlock);
            }
        }

        // ------------------------------------------------------------------ picking up / setting aside

        /// <summary>Picks up the root part: the whole craft is held (to put back or attach to a set-aside group).</summary>
        private void PickUpWholeCraft()
        {
            if (Design.parts.Count == 0) return;
            PushUndo();
            _held = ExtractSubtree(0, out _);
            _heldRotation = Quaternion.identity;
            _heldFromCraft = true;
            _heldWholeCraft = true;
            Design.parts.Clear();
            Asm = new CraftAssembler(Design, Db);
            PartsChanged();
            BuildGhosts(1);
            Message?.Invoke("Holding the whole craft: click to put it back, or attach it to a free node of a set-aside (grey) group");
        }

        /// <summary>Picks up a part of a set-aside group together with the parts attached below it.</summary>
        public void PickUpDetached(int k, int j)
        {
            if (k < 0 || k >= Design.detached.Count) return;
            var a = Detached(k);
            if (j < 0 || j >= a.Parts.Count) return;
            PushUndo();
            _held = ExtractSubtree(a.Parts, j, out Quaternion localRot);
            _heldRotation = a.Rotation * localRot;
            _heldFromCraft = true;
            RemoveFromDetached(k, j);
            Rebuild();
            BuildGhosts(1);
        }

        /// <summary>Holds a copy of a set-aside part's subtree (Alt+click).</summary>
        public void CopyDetached(int k, int j)
        {
            if (k < 0 || k >= Design.detached.Count) return;
            var a = Detached(k);
            if (j < 0 || j >= a.Parts.Count) return;
            CancelHold();
            _held = ExtractSubtree(a.Parts, j, out Quaternion localRot);
            foreach (var r in _held) { r.uid = 0; r.symmetryGroup = -1; r.stage = -1; }
            _heldRotation = a.Rotation * localRot;
            _heldFromCraft = false;
            BuildGhosts(1);
            Message?.Invoke("Copied " + (Db.Get(_held[0].partId)?.title ?? "part"));
        }

        /// <summary>Deletes a set-aside part and the parts attached below it (the whole group for its root).</summary>
        public void DeleteDetached(int k, int j)
        {
            if (k < 0 || k >= Design.detached.Count || j < 0 || j >= Design.detached[k].parts.Count) return;
            PushUndo();
            RemoveFromDetached(k, j);
            Rebuild();
            Message?.Invoke("Part deleted (Ctrl+Z to undo)");
        }

        private void RemoveFromDetached(int k, int j)
        {
            if (j == 0) Design.detached.RemoveAt(k);
            else new CraftAssembler(new CraftDesign { parts = Design.detached[k].parts }, Db).RemoveSubtree(j);
        }

        /// <summary>Leaves the held parts where they float: a new set-aside group, greyed out, not part of the craft.</summary>
        private void SetAsideHeld()
        {
            if (_held == null || _copies.Count == 0) return;
            if (!_heldFromCraft) PushUndo();
            var cp = _copies[0];
            var det = new DetachedAssembly
            {
                pos = CraftAssembler.A(Craft.Origin + cp.Pos),
                rot = CraftAssembler.A(cp.Rot),
            };
            int uid = NextUid();
            foreach (var src in _held)
            {
                var r = SaveStorage.DeepClone(src);
                r.uid = uid++;
                r.stage = -1;
                det.parts.Add(r);
            }
            det.parts[0].parent = -1;
            det.parts[0].parentNode = null;
            det.parts[0].attachNode = null;
            Design.detached.Add(det);
            int n = det.parts.Count;
            _heldFromCraft = false;
            CancelHold();
            Rebuild();
            Message?.Invoke(n == 1 ? "Part set aside (grey): it is not part of the craft until you attach it again"
                                   : $"{n} parts set aside (grey): they are not part of the craft until you attach them again");
        }

        /// <summary>
        /// Re-roots the craft on a part (KSP's re-root tool): parent links along the path to the old root are reversed
        /// and all poses re-expressed relative to the new root. Only through stack connections, and not for parts
        /// placed in symmetry (a surface-attached or mirrored part can't carry the craft).
        /// </summary>
        public bool MakeRoot(int index)
        {
            if (index <= 0 || index >= Design.parts.Count) return false;
            var nr = Design.parts[index];
            if (nr.symmetryGroup >= 0 && SymmetryGroup(index).Count > 1)
            {
                Message?.Invoke("A part placed in symmetry can't become the root");
                return false;
            }
            var path = new List<int>(); // new root, its parent, ..., old root
            for (int i = index; i >= 0; i = Design.parts[i].parent)
            {
                path.Add(i);
                if (path.Count > Design.parts.Count) return false; // broken tree
            }
            for (int k = 0; k < path.Count - 1; k++)
            {
                var r = Design.parts[path[k]];
                if (r.attachNode == "srf" || r.parentNode == "srf")
                {
                    Message?.Invoke("Only parts connected to the root through stack nodes can become the root");
                    return false;
                }
            }
            PushUndo();
            CancelHold();
            // Reverse the links from the old root down to the new one (node names swap sides, as in flight re-rooting).
            for (int k = path.Count - 1; k > 0; k--)
            {
                var parent = Design.parts[path[k]];
                var child = Design.parts[path[k - 1]];
                parent.parent = path[k - 1];
                parent.parentNode = child.attachNode;
                parent.attachNode = child.parentNode;
            }
            nr.parent = -1;
            nr.parentNode = null;
            nr.attachNode = null;
            // Poses relative to the new root, which moves to the origin unrotated.
            Vector3 p0 = CraftAssembler.V(nr.pos);
            Quaternion inv = Quaternion.Inverse(CraftAssembler.Q(nr.rot));
            foreach (var r in Design.parts)
            {
                r.pos = CraftAssembler.A(inv * (CraftAssembler.V(r.pos) - p0));
                r.rot = CraftAssembler.A(inv * CraftAssembler.Q(r.rot));
            }
            // The root must be index 0: move it to the front and renumber the parent links.
            var order = new List<int> { index };
            for (int i = 0; i < Design.parts.Count; i++) if (i != index) order.Add(i);
            var map = new Dictionary<int, int>();
            for (int i = 0; i < order.Count; i++) map[order[i]] = i;
            var reordered = new List<PartNodeRecord>();
            foreach (int i in order) reordered.Add(Design.parts[i]);
            foreach (var r in reordered) if (r.parent >= 0) r.parent = map[r.parent];
            Design.parts.Clear();
            Design.parts.AddRange(reordered);
            Asm = new CraftAssembler(Design, Db);
            PartsChanged();
            Message?.Invoke($"{Db.Get(nr.partId)?.title ?? "Part"} is now the root part");
            return true;
        }

        /// <summary>Drops the held parts as if the player clicked at a screen position, wherever that is (automation).</summary>
        public void DropHeldAt(Vector2 screenPos)
        {
            if (_held == null || Cam == null) return;
            UpdatePlacement(Cam.ScreenPointToRay(screenPos), screenPos);
            CommitHeld();
        }

        /// <summary>Screen position of a stack node of a part in a group (0 = craft, k + 1 = set-aside group k).</summary>
        public bool NodeScreenPosition(int asm, int index, string node, out Vector2 screen)
        {
            screen = default;
            if (asm < 0 || asm > Design.detached.Count) return false;
            var a = GetAsmb(asm);
            if (index < 0 || index >= a.Parts.Count) return false;
            var n = Db.Get(a.Parts[index].partId)?.FindNode(node);
            if (n == null) return false;
            Vector3 w = a.Origin + a.Rotation * (CraftAssembler.V(a.Parts[index].pos) + CraftAssembler.Q(a.Parts[index].rot) * n.Position);
            Vector3 s = Cam.WorldToScreenPoint(w);
            screen = new Vector2(s.x, s.y);
            return s.z > 0;
        }

        /// <summary>Esc while holding: parts picked up go back where they were; a new part from the list is dropped.</summary>
        public void PutBackHeld()
        {
            if (_held == null) return;
            if (_heldFromCraft && CanUndo)
            {
                Undo(); // the snapshot taken when the parts were picked up
                if (_redo.Count > 0) _redo.RemoveAt(_redo.Count - 1);
                Message?.Invoke("Put back");
            }
            else DiscardHeld();
        }
    }
}
