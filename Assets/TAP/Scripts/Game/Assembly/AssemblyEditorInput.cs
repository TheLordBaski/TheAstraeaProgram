using System;
using System.Collections.Generic;
using TAP.Construction;
using TAP.Core;
using TAP.Parts;
using TAP.Persistence;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TAP.Game
{
    /// <summary>Mouse/keyboard interaction and attachment solving of the assembly editor.</summary>
    public sealed partial class AssemblyEditor
    {
        public enum AttachKind { None, Root, Stack, Surface }

        /// <summary>Where the held part would go if it were placed now.</summary>
        public struct Placement
        {
            public AttachKind Kind;
            public bool Valid;
            public string Reason;
            /// <summary>Target group: 0 = the craft, k + 1 = detached group k.</summary>
            public int Asm;
            public int Parent;
            public string ParentNode, AttachNode;
        }

        private struct CopyPose
        {
            public int Parent;
            public Vector3 Pos;
            public Quaternion Rot;
        }

        public Placement Current;
        public int HoveredPart = -1;
        /// <summary>Detached part under the pointer: group id (k + 1) and index, or -1.</summary>
        public int HoveredDetachedAsm = -1, HoveredDetachedPart = -1;
        /// <summary>Parts highlighted from the UI (e.g. hovering a stage icon).</summary>
        public readonly HashSet<int> UiHighlighted = new HashSet<int>();

        private readonly List<CopyPose> _copies = new List<CopyPose>();
        private bool _highlightDirty = true;
        private readonly Dictionary<int, Color> _appliedGlow = new Dictionary<int, Color>();
        private MaterialPropertyBlock _mpb;
        private Vector2 _rmbDownPos;
        private bool _rmbTracking;
        private readonly List<Transform> _nodeMarkers = new List<Transform>();
        private readonly List<Renderer> _nodeMarkerRenderers = new List<Renderer>();
        private Material _nodeMat, _nodeTargetMat;
        private Vector3 _targetNodeWorld;
        private const float SnapRadiusPx = 46f; // at 1080p

        private static readonly Color HoverGlow = new Color(0.10f, 0.20f, 0.32f);
        private static readonly Color StageGlow = new Color(0.32f, 0.26f, 0.04f);
        private static readonly Color ValidGlow = new Color(0.04f, 0.30f, 0.08f);
        private static readonly Color InvalidGlow = new Color(0.40f, 0.05f, 0.03f);

        private void Update()
        {
            if (Cam == null || CraftRoot == null) return;
            var mouse = Mouse.current;
            var kb = Keyboard.current;
            if (mouse == null) return;
            bool overUi = PointerOverUi != null && PointerOverUi();
            if (kb != null && !UiState.KeyboardCaptured) HandleKeys(kb);

            Vector2 mp = mouse.position.ReadValue();
            Ray ray = Cam.ScreenPointToRay(mp);
            HoveredDetachedAsm = HoveredDetachedPart = -1;
            if (_held != null)
            {
                HoveredPart = -1;
                UpdatePlacement(ray, mp);
                UpdateGhosts();
                UpdateNodeMarkers();
                if (mouse.leftButton.wasPressedThisFrame && !overUi) CommitHeld();
            }
            else
            {
                HoveredPart = -1;
                if (!overUi && RaycastAny(ray, out int asm, out int index, out _))
                {
                    if (asm == 0) HoveredPart = index;
                    else { HoveredDetachedAsm = asm; HoveredDetachedPart = index; }
                }
                if (mouse.leftButton.wasPressedThisFrame && !overUi)
                {
                    bool copy = kb != null && (kb.leftAltKey.isPressed || kb.rightAltKey.isPressed);
                    if (HoveredPart >= 0)
                    {
                        if (copy) CopyPart(HoveredPart);
                        else PickUp(HoveredPart);
                    }
                    else if (HoveredDetachedAsm > 0)
                    {
                        if (copy) CopyDetached(HoveredDetachedAsm - 1, HoveredDetachedPart);
                        else PickUpDetached(HoveredDetachedAsm - 1, HoveredDetachedPart);
                    }
                }
            }
            // Right click (without dragging the camera) opens the part menu.
            if (mouse.rightButton.wasPressedThisFrame) { _rmbTracking = !overUi; _rmbDownPos = mp; }
            if (mouse.rightButton.wasReleasedThisFrame && _rmbTracking)
            {
                _rmbTracking = false;
                if ((mp - _rmbDownPos).magnitude < 6f && _held == null && HoveredPart >= 0) PartContextRequested?.Invoke(HoveredPart);
            }
            UpdateHighlights();
            UpdateDetachedHighlight();
        }

        /// <summary>
        /// Places the held part as if the player clicked at a screen position (automation and tests).
        /// Returns true if the part was attached.
        /// </summary>
        public bool PlaceHeldAt(Vector2 screenPos)
        {
            if (_held == null || Cam == null) return false;
            int before = Design.parts.Count;
            UpdatePlacement(Cam.ScreenPointToRay(screenPos), screenPos);
            if (!Current.Valid) return false;
            CommitHeld();
            return Design.parts.Count > before;
        }

        /// <summary>Screen position of a free stack node of a placed part (automation and tests).</summary>
        public bool NodeScreenPosition(int index, string node, out Vector2 screen)
        {
            screen = default;
            var def = Db.Get(Design.parts[index].partId);
            var n = def?.FindNode(node);
            if (n == null) return false;
            Vector3 w = CraftRoot.position + CraftAssembler.V(Design.parts[index].pos) + CraftAssembler.Q(Design.parts[index].rot) * n.Position;
            Vector3 s = Cam.WorldToScreenPoint(w);
            screen = new Vector2(s.x, s.y);
            return s.z > 0;
        }

        // ------------------------------------------------------------------ keys

        private void HandleKeys(Keyboard kb)
        {
            bool ctrl = kb.leftCtrlKey.isPressed || kb.rightCtrlKey.isPressed;
            bool shift = kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed;
            if (ctrl)
            {
                if (kb.zKey.wasPressedThisFrame) { if (shift) Redo(); else Undo(); }
                else if (kb.yKey.wasPressedThisFrame) Redo();
                else if (kb.sKey.wasPressedThisFrame) Save();
                return;
            }
            if (kb.xKey.wasPressedThisFrame) CycleSymmetry(shift ? -1 : 1);
            if (kb.cKey.wasPressedThisFrame) ToggleAngleSnap();
            if (kb.fKey.wasPressedThisFrame || kb.homeKey.wasPressedThisFrame) FrameCraft();
            if (kb.vKey.wasPressedThisFrame) ToggleMarkers();

            if (_held != null)
            {
                float step = shift ? 5f : (AngleSnap ? 90f : 15f);
                Vector3 camRight = SnapAxis(Cam.transform.right);
                Vector3 camFwd = SnapAxis(Cam.transform.forward);
                if (kb.wKey.wasPressedThisFrame) RotateHeld(camRight, step);
                if (kb.sKey.wasPressedThisFrame) RotateHeld(camRight, -step);
                if (kb.aKey.wasPressedThisFrame) RotateHeld(Vector3.up, step);
                if (kb.dKey.wasPressedThisFrame) RotateHeld(Vector3.up, -step);
                if (kb.qKey.wasPressedThisFrame) RotateHeld(camFwd, step);
                if (kb.eKey.wasPressedThisFrame) RotateHeld(camFwd, -step);
                if (kb.spaceKey.wasPressedThisFrame) _heldRotation = Quaternion.identity;
                if (kb.escapeKey.wasPressedThisFrame) PutBackHeld();
                else if (kb.deleteKey.wasPressedThisFrame || kb.backspaceKey.wasPressedThisFrame) DiscardHeld();
            }
            else if (kb.deleteKey.wasPressedThisFrame || kb.backspaceKey.wasPressedThisFrame)
            {
                if (HoveredPart >= 0) DeletePart(HoveredPart);
                else if (HoveredDetachedAsm > 0) DeleteDetached(HoveredDetachedAsm - 1, HoveredDetachedPart);
            }
        }

        /// <summary>Nearest horizontal world axis to a direction (so key rotations stay on the grid).</summary>
        private static Vector3 SnapAxis(Vector3 v)
        {
            v.y = 0;
            if (v.sqrMagnitude < 1e-6f) return Vector3.right;
            return Mathf.Abs(v.x) >= Mathf.Abs(v.z) ? new Vector3(Mathf.Sign(v.x), 0, 0) : new Vector3(0, 0, Mathf.Sign(v.z));
        }

        private void RotateHeld(Vector3 axis, float deg)
        {
            _heldRotation = Quaternion.AngleAxis(deg, axis) * _heldRotation;
            // Keep the quaternion clean after many 90 degree steps.
            Vector3 e = _heldRotation.eulerAngles;
            if (Mathf.Abs(deg) >= 15f) _heldRotation = Quaternion.Euler(Mathf.Round(e.x / 5f) * 5f, Mathf.Round(e.y / 5f) * 5f, Mathf.Round(e.z / 5f) * 5f);
        }

        // ------------------------------------------------------------------ picking

        /// <summary>Index of the craft part under the ray (-1 if none, or if a detached part is in front).</summary>
        public int RaycastPart(Ray ray, out RaycastHit hit)
        {
            if (RaycastAny(ray, out int asm, out int index, out hit) && asm == 0) return index;
            return -1;
        }

        /// <summary>Part under the ray: group (0 = craft, k + 1 = detached group k) and index in that group.</summary>
        public bool RaycastAny(Ray ray, out int asm, out int index, out RaycastHit hit)
        {
            asm = -1;
            index = -1;
            if (!Physics.Raycast(ray, out hit, 500f, 1 << Layers.EditorParts, QueryTriggerInteraction.Ignore)) return false;
            var tag = hit.collider.GetComponentInParent<EditorPartTag>();
            if (tag == null || tag.Assembly < 0 || tag.Assembly > Design.detached.Count) return false;
            if (tag.Index >= GetAsmb(tag.Assembly).Parts.Count) return false;
            asm = tag.Assembly;
            index = tag.Index;
            return true;
        }

        public bool NodeOccupied(int index, string node) => NodeOccupied(Design.parts, index, node);

        private static bool NodeOccupied(List<PartNodeRecord> parts, int index, string node)
        {
            var r = parts[index];
            if (r.parent >= 0 && r.attachNode == node) return true;
            for (int i = 0; i < parts.Count; i++)
                if (parts[i].parent == index && parts[i].parentNode == node) return true;
            return false;
        }

        private List<AttachNodeDefinition> FreeHeldNodes(PartDefinition rootDef)
        {
            var list = new List<AttachNodeDefinition>();
            foreach (var n in rootDef.nodes)
            {
                bool used = false;
                for (int j = 1; j < _held.Count; j++)
                    if (_held[j].parent == 0 && _held[j].parentNode == n.id) { used = true; break; }
                if (!used) list.Add(n);
            }
            return list;
        }

        // ------------------------------------------------------------------ placement solving

        private void UpdatePlacement(Ray ray, Vector2 mouse)
        {
            _copies.Clear();
            Current = new Placement { Kind = AttachKind.None, Parent = -1 };
            var rootDef = Db.Get(_held[0].partId);
            if (rootDef == null) return;

            if (Design.parts.Count == 0)
            {
                // No craft: the held parts attach to a detached group if pointed at one, else they become the craft,
                // standing upright on the floor.
                if (Design.detached.Count > 0 && (TryStackSnap(mouse, rootDef) || TrySurfaceAttach(ray, rootDef))) return;
                Current = new Placement { Kind = AttachKind.Root, Valid = true, Parent = -1 };
                _copies.Clear();
                _copies.Add(new CopyPose { Parent = -1, Pos = Vector3.up * (rootDef.height * 0.5f), Rot = Quaternion.identity });
                return;
            }
            if (TryStackSnap(mouse, rootDef)) return;
            if (TrySurfaceAttach(ray, rootDef)) return;

            // Free floating in front of the craft: a click sets it aside.
            Vector3 p = FloatPoint(ray);
            _copies.Add(new CopyPose { Parent = -1, Pos = p - CraftRoot.position, Rot = _heldRotation });
            Current.Reason = rootDef.surfaceAttach.allowed
                ? "Move over a free node (green) or a part surface to attach"
                : "Move over a free attachment node (green) to attach";
        }

        private Vector3 FloatPoint(Ray ray)
        {
            Vector3 n = Cam.transform.forward;
            n.y = 0;
            if (n.sqrMagnitude < 1e-4f) n = Vector3.forward;
            var plane = new Plane(-n.normalized, CraftRoot.position);
            if (plane.Raycast(ray, out float t) && t > 0.5f && t < 400f) return ray.GetPoint(t);
            return ray.GetPoint(15f);
        }

        private bool TryStackSnap(Vector2 mouse, PartDefinition rootDef)
        {
            if (rootDef.nodes.Count == 0) return false;
            var freeHeld = FreeHeldNodes(rootDef);
            if (freeHeld.Count == 0) return false;
            float best = SnapRadiusPx * Mathf.Max(0.5f, Screen.height / 1080f);
            int bestAsm = -1, bestPart = -1;
            AttachNodeDefinition bestNode = null;
            foreach (var a in AllAssemblies())
            {
                for (int i = 0; i < a.Parts.Count; i++)
                {
                    var def = Db.Get(a.Parts[i].partId);
                    if (def == null) continue;
                    Vector3 pp = CraftAssembler.V(a.Parts[i].pos);
                    Quaternion pr = CraftAssembler.Q(a.Parts[i].rot);
                    foreach (var n in def.nodes)
                    {
                        if (NodeOccupied(a.Parts, i, n.id)) continue;
                        Vector3 s = Cam.WorldToScreenPoint(a.Origin + a.Rotation * (pp + pr * n.Position));
                        if (s.z <= 0) continue;
                        float d = Vector2.Distance(new Vector2(s.x, s.y), mouse);
                        if (d < best) { best = d; bestAsm = a.Id; bestPart = i; bestNode = n; }
                    }
                }
            }
            if (bestPart < 0) return false;

            var target = GetAsmb(bestAsm);
            var tr = target.Parts[bestPart];
            Vector3 tPos = CraftAssembler.V(tr.pos);
            Quaternion tRot = CraftAssembler.Q(tr.rot);
            // The held part is oriented in the target group's frame; the child's node must point back at the parent.
            Quaternion heldLocal = Quaternion.Inverse(target.Rotation) * _heldRotation;
            Vector3 targetDir = -(tRot * bestNode.Direction);
            AttachNodeDefinition hn = null;
            float bestDot = -2f;
            foreach (var n in freeHeld)
            {
                float dot = Vector3.Dot(heldLocal * n.Direction, targetDir);
                if (dot > bestDot + 1e-3f) { bestDot = dot; hn = n; }
            }
            Quaternion rot = Quaternion.FromToRotation(heldLocal * hn.Direction, targetDir) * heldLocal;
            Vector3 pos = tPos + tRot * bestNode.Position - rot * hn.Position;

            Current.Kind = AttachKind.Stack;
            Current.Valid = true;
            Current.Asm = bestAsm;
            Current.Parent = bestPart;
            Current.ParentNode = bestNode.id;
            Current.AttachNode = hn.id;
            _targetNodeWorld = target.Origin + target.Rotation * (tPos + tRot * bestNode.Position);
            AddSymmetricCopies(target, bestPart, pos, rot, bestNode.id);
            return true;
        }

        private bool TrySurfaceAttach(Ray ray, PartDefinition rootDef)
        {
            if (!RaycastAny(ray, out int asm, out int hitIndex, out RaycastHit hit)) return false;
            var target = GetAsmb(asm);
            Quaternion invA = Quaternion.Inverse(target.Rotation);
            Vector3 hitLocal = invA * (hit.point - target.Origin);
            Vector3 normalLocal = invA * hit.normal;
            var pr = target.Parts[hitIndex];
            var pdef = Db.Get(pr.partId);
            Current.Kind = AttachKind.Surface;
            Current.Asm = asm;
            Current.Parent = hitIndex;
            if (!rootDef.surfaceAttach.allowed || pdef == null || !pdef.surfaceAttach.onto)
            {
                Current.Valid = false;
                Current.Reason = !rootDef.surfaceAttach.allowed
                    ? $"{rootDef.title} attaches only to stack nodes (the green spheres)"
                    : $"Nothing can be attached to the surface of the {pdef?.title}";
                _copies.Add(new CopyPose { Parent = -1, Pos = hitLocal + normalLocal * Mathf.Max(0.3f, rootDef.diameter * 0.5f), Rot = invA * _heldRotation });
                return true;
            }

            Vector3 pPos = CraftAssembler.V(pr.pos);
            Quaternion pRot = CraftAssembler.Q(pr.rot);
            Quaternion invP = Quaternion.Inverse(pRot);
            Vector3 lp = invP * (hitLocal - pPos);
            Vector3 ln = (invP * normalLocal).normalized;
            float horiz = new Vector2(ln.x, ln.z).magnitude;
            if (horiz > 0.3f)
            {
                // Side surface: use the exact lathe profile of the parent and optionally snap the angle.
                float az = Mathf.Atan2(lp.x, lp.z);
                if (AngleSnap) az = Mathf.Round(az * Mathf.Rad2Deg / 15f) * 15f * Mathf.Deg2Rad;
                Vector3 dirH = new Vector3(Mathf.Sin(az), 0, Mathf.Cos(az));
                if (SurfaceProfile(pdef, lp.y, out float radius, out float slope))
                {
                    lp = dirH * radius + Vector3.up * lp.y;
                    ln = (dirH + Vector3.up * slope).normalized;
                }
                else if (AngleSnap)
                {
                    float r = new Vector2(lp.x, lp.z).magnitude;
                    lp = dirH * r + Vector3.up * lp.y;
                    ln = (dirH * horiz + Vector3.up * ln.y).normalized;
                }
            }
            CraftAssembler.SurfacePose(pPos, pRot, lp, ln, rootDef, 0, out _, out Quaternion aligned);
            Quaternion rot = aligned * (invA * _heldRotation);
            Vector3 pos = pPos + pRot * lp - rot * rootDef.surfaceAttach.Position;
            Current.Valid = true;
            Current.ParentNode = "srf";
            Current.AttachNode = "srf";
            AddSymmetricCopies(target, hitIndex, pos, rot, null);
            return true;
        }

        /// <summary>
        /// Radius and outward slope (dr/dy, negated) of a part's side at part-local height y for lathe-shaped parts.
        /// Returns false for irregular shapes (the collider hit is used instead).
        /// </summary>
        private static bool SurfaceProfile(PartDefinition def, float y, out float radius, out float slope)
        {
            radius = 0; slope = 0;
            var m = def.model;
            if (m == null) return false;
            float h = m.height > 0 ? m.height : def.height;
            float y0 = -h / 2, y1 = h / 2;
            y = Mathf.Clamp(y, y0, y1);
            switch (m.type)
            {
                case "tank":
                case "srb":
                case "probe":
                case "decoupler":
                case "disc":
                case "dockingport":
                case "heatshield":
                    radius = m.diameter * 0.5f;
                    return true;
                case "adapter":
                {
                    float rb = m.BottomD * 0.5f, rt = m.TopD * 0.5f;
                    float a = y0 + 0.08f, b = y1 - 0.08f;
                    if (y <= a) { radius = rb; return true; }
                    if (y >= b) { radius = rt; return true; }
                    radius = Mathf.Lerp(rb, rt, (y - a) / (b - a));
                    slope = (rb - rt) / (b - a);
                    return true;
                }
                case "capsule":
                {
                    float rb = m.BottomD * 0.5f, rt = m.TopD * 0.5f;
                    float yb = y1 - h * 0.1f;
                    if (y >= yb) { radius = rt * 0.94f; return true; }
                    radius = Mathf.Lerp(rb, rt, (y - y0) / (yb - y0));
                    slope = (rb - rt) / (yb - y0);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Adds the pose for the target parent plus its symmetry copies: onto every symmetry counterpart of the
        /// parent (mirrored by their relative transform), or N-way radial copies around the parent's axis.
        /// Poses are in the target group's frame.
        /// </summary>
        private void AddSymmetricCopies(Asmb target, int parent, Vector3 pos, Quaternion rot, string stackNode)
        {
            var parts = target.Parts;
            var pr = parts[parent];
            Vector3 pPos = CraftAssembler.V(pr.pos);
            Quaternion pRot = CraftAssembler.Q(pr.rot);
            Quaternion invP = Quaternion.Inverse(pRot);
            var group = SymmetryGroup(parts, parent);
            if (group.Count > 1)
            {
                foreach (int c in group)
                {
                    if (stackNode != null && NodeOccupied(parts, c, stackNode))
                    {
                        Current.Valid = false;
                        Current.Reason = "The same node on a symmetry counterpart is already occupied";
                    }
                    var cr = parts[c];
                    Quaternion m = CraftAssembler.Q(cr.rot) * invP;
                    _copies.Add(new CopyPose { Parent = c, Pos = CraftAssembler.V(cr.pos) + m * (pos - pPos), Rot = m * rot });
                }
                return;
            }
            int n = stackNode == null ? Math.Max(1, Symmetry) : 1;
            for (int k = 0; k < n; k++)
            {
                Quaternion rk = pRot * Quaternion.AngleAxis(360f * k / n, Vector3.up) * invP;
                _copies.Add(new CopyPose { Parent = parent, Pos = pPos + rk * (pos - pPos), Rot = rk * rot });
            }
        }

        // ------------------------------------------------------------------ committing

        private void CommitHeld()
        {
            if (_held == null) return;
            if (!Current.Valid || _copies.Count == 0)
            {
                if (Current.Kind == AttachKind.None) SetAsideHeld();
                else Message?.Invoke(Current.Reason ?? "Can't attach here");
                return;
            }
            if (!_heldFromCraft) PushUndo();
            bool root = Current.Kind == AttachKind.Root;
            var target = root ? Craft : GetAsmb(Current.Asm);
            var parts = target.Parts;
            bool intoCraft = target.Id == 0;
            int copies = _copies.Count;
            int groupBase = NextSymmetryGroup(parts);
            // Single placements keep the held parts' own symmetry groups, renumbered so they can't clash with the
            // groups of the part list they join (a group set aside could reuse a number the craft has).
            var remap = new Dictionary<int, int>();
            int nextGroup = groupBase;
            int uid = NextUid();
            for (int c = 0; c < copies; c++)
            {
                var cp = _copies[c];
                if (root) { cp.Pos = Vector3.zero; cp.Rot = Quaternion.identity; }
                int baseIndex = parts.Count;
                for (int j = 0; j < _held.Count; j++)
                {
                    var src = _held[j];
                    var r = SaveStorage.DeepClone(src);
                    r.uid = uid++;
                    r.pos = CraftAssembler.A(cp.Pos + cp.Rot * CraftAssembler.V(src.pos));
                    r.rot = CraftAssembler.A(cp.Rot * CraftAssembler.Q(src.rot));
                    if (j == 0)
                    {
                        r.parent = cp.Parent;
                        r.parentNode = root ? null : Current.ParentNode;
                        r.attachNode = root ? null : Current.AttachNode;
                    }
                    else r.parent = baseIndex + src.parent;
                    // Symmetry: the j-th parts of all copies form a group.
                    if (copies > 1) r.symmetryGroup = groupBase + j;
                    else if (j == 0) r.symmetryGroup = -1;
                    else if (src.symmetryGroup >= 0)
                    {
                        if (!remap.TryGetValue(src.symmetryGroup, out int g)) { g = nextGroup++; remap[src.symmetryGroup] = g; }
                        r.symmetryGroup = g;
                    }
                    if (!_heldFromCraft || !intoCraft) r.stage = -1;
                    parts.Add(r);
                }
            }
            if (root) parts[0].parent = -1;
            // The whole craft attached onto a set-aside group: that group becomes the craft (its root is the new root).
            if (_heldWholeCraft && Design.parts.Count == 0 && target.Id > 0)
            {
                var det = Design.detached[target.Id - 1];
                Design.detached.RemoveAt(target.Id - 1);
                Design.parts.AddRange(det.parts);
                Design.parts[0].parent = -1;
                Design.parts[0].parentNode = null;
                Design.parts[0].attachNode = null;
                Message?.Invoke("Craft attached: the group it was attached to is now part of the craft");
            }
            var placedDef = Db.Get(_held[0].partId);
            _heldFromCraft = false;
            _heldWholeCraft = false;
            CancelHold();
            Asm = new CraftAssembler(Design, Db);
            PartsChanged();
            if (copies > 1) Message?.Invoke($"Placed {copies}× {placedDef?.title}");
        }

        private int NextUid()
        {
            int max = 0;
            foreach (var p in Design.parts) max = Math.Max(max, p.uid);
            foreach (var d in Design.detached) foreach (var p in d.parts) max = Math.Max(max, p.uid);
            return max + 1;
        }

        private static int NextSymmetryGroup(List<PartNodeRecord> parts)
        {
            int max = 0;
            foreach (var p in parts) max = Math.Max(max, p.symmetryGroup + 1);
            return max;
        }

        // ------------------------------------------------------------------ visuals

        private void UpdateGhosts()
        {
            EnsureGhostCopies(Math.Max(1, _copies.Count));
            // Poses are in the target group's frame; floating and root placements use the craft frame.
            var frame = Current.Kind == AttachKind.Stack || Current.Kind == AttachKind.Surface ? GetAsmb(Current.Asm) : Craft;
            Color glow = Current.Valid ? ValidGlow : InvalidGlow;
            if (_mpb == null) _mpb = new MaterialPropertyBlock();
            _mpb.SetColor("_EmissionColor", glow);
            for (int c = 0; c < _ghosts.Count && c < _copies.Count; c++)
            {
                var cp = _copies[c];
                for (int j = 0; j < _held.Count && j < _ghosts[c].Count; j++)
                {
                    var go = _ghosts[c][j];
                    if (go == null) continue;
                    go.transform.SetPositionAndRotation(
                        frame.Origin + frame.Rotation * (cp.Pos + cp.Rot * CraftAssembler.V(_held[j].pos)),
                        frame.Rotation * cp.Rot * CraftAssembler.Q(_held[j].rot));
                    foreach (var r in go.GetComponentsInChildren<Renderer>()) r.SetPropertyBlock(_mpb);
                }
            }
        }

        private void UpdateHighlights()
        {
            if (_mpb == null) _mpb = new MaterialPropertyBlock();
            // Desired glow per part.
            var want = new Dictionary<int, Color>();
            if (HoveredPart >= 0 && HoveredPart < Design.parts.Count)
                foreach (int i in SymmetryGroup(HoveredPart)) want[i] = HoverGlow;
            foreach (int i in UiHighlighted) if (i < Design.parts.Count) want[i] = StageGlow;
            if (_held != null && Current.Kind != AttachKind.None && Current.Asm == 0 && Current.Parent >= 0 && Current.Valid)
                want[Current.Parent] = ValidGlow * 0.6f;

            bool changed = _highlightDirty || want.Count != _appliedGlow.Count;
            if (!changed)
                foreach (var kv in want)
                    if (!_appliedGlow.TryGetValue(kv.Key, out var c) || c != kv.Value) { changed = true; break; }
            if (!changed) return;
            _highlightDirty = false;
            for (int i = 0; i < PartObjects.Count; i++)
            {
                var go = PartObjects[i];
                if (go == null) continue;
                bool on = want.TryGetValue(i, out Color g);
                if (on) _mpb.SetColor("_EmissionColor", g);
                foreach (var r in go.GetComponentsInChildren<Renderer>()) r.SetPropertyBlock(on ? _mpb : null);
            }
            _appliedGlow.Clear();
            foreach (var kv in want) _appliedGlow[kv.Key] = kv.Value;
        }

        public void RefreshHighlights() => _highlightDirty = true;

        private void UpdateNodeMarkers()
        {
            var rootDef = Db.Get(_held[0].partId);
            bool show = rootDef != null && rootDef.nodes.Count > 0 && FreeHeldNodes(rootDef).Count > 0;
            int used = 0;
            if (show)
            {
                if (_nodeMat == null)
                {
                    _nodeMat = EditorVisuals.OverlayMaterial(new Color(0.3f, 1f, 0.45f, 0.55f), true);
                    _nodeTargetMat = EditorVisuals.OverlayMaterial(new Color(0.55f, 1f, 0.6f, 0.95f), true);
                }
                foreach (var a in AllAssemblies())
                {
                    for (int i = 0; i < a.Parts.Count; i++)
                    {
                        var def = Db.Get(a.Parts[i].partId);
                        if (def == null) continue;
                        Vector3 pp = CraftAssembler.V(a.Parts[i].pos);
                        Quaternion pr = CraftAssembler.Q(a.Parts[i].rot);
                        foreach (var n in def.nodes)
                        {
                            if (NodeOccupied(a.Parts, i, n.id)) continue;
                            Vector3 w = a.Origin + a.Rotation * (pp + pr * n.Position);
                            if (used >= _nodeMarkers.Count)
                            {
                                var s = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                                Destroy(s.GetComponent<Collider>());
                                s.name = "NodeMarker";
                                s.layer = Layers.Ghost;
                                var rend = s.GetComponent<Renderer>();
                                rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                                _nodeMarkers.Add(s.transform);
                                _nodeMarkerRenderers.Add(rend);
                            }
                            var t = _nodeMarkers[used];
                            bool target = Current.Kind == AttachKind.Stack && (w - _targetNodeWorld).sqrMagnitude < 1e-4f;
                            float size = n.size == 0 ? 0.22f : n.size == 1 ? 0.32f : 0.46f;
                            t.position = w;
                            t.localScale = Vector3.one * (target ? size * 1.35f : size);
                            _nodeMarkerRenderers[used].sharedMaterial = target ? _nodeTargetMat : _nodeMat;
                            if (!t.gameObject.activeSelf) t.gameObject.SetActive(true);
                            used++;
                        }
                    }
                }
            }
            for (int i = used; i < _nodeMarkers.Count; i++)
                if (_nodeMarkers[i].gameObject.activeSelf) _nodeMarkers[i].gameObject.SetActive(false);
        }

        private void HideNodeMarkers()
        {
            foreach (var t in _nodeMarkers) if (t != null) t.gameObject.SetActive(false);
        }

        /// <summary>Status line for the UI describing what a click would do now.</summary>
        public string PlacementHint
        {
            get
            {
                if (_held == null) return null;
                var def = Db.Get(_held[0].partId);
                string name = _heldWholeCraft ? "craft" : def?.title ?? "part";
                string where = Current.Asm > 0 ? " (set-aside group)" : "";
                switch (Current.Kind)
                {
                    case AttachKind.Root:
                        return _heldWholeCraft ? "Click to put the craft back · or attach it to a free node of a set-aside (grey) group"
                                               : $"Click to place the {name} as the root part";
                    case AttachKind.Stack:
                        return Current.Valid ? $"Click to attach {name} to the {Current.ParentNode} node of the {PartTitle(Current.Asm, Current.Parent)}{where}" : Current.Reason;
                    case AttachKind.Surface:
                        return Current.Valid
                            ? $"Click to attach {name} to the {PartTitle(Current.Asm, Current.Parent)}{where}" + (_copies.Count > 1 ? $" ({_copies.Count}× symmetry)" : "")
                            : Current.Reason;
                    default:
                        return (Current.Reason ?? "") + " · click empty space to set it aside (grey, not part of the craft) · drop it on the parts list to delete it";
                }
            }
        }

        private string PartTitle(int asm, int index)
        {
            var parts = asm >= 0 && asm <= Design.detached.Count ? GetAsmb(asm).Parts : null;
            if (parts == null || index < 0 || index >= parts.Count) return "?";
            return Db.Get(parts[index].partId)?.title ?? parts[index].partId;
        }
    }
}
