"""Builds tasks.csv from the plan files and checks them.

Run from anywhere:  python product/plan/build_index.py
- Every task heading looks like:   ### FND-01 · Title
- followed by a line like:         **Milestone** M1 · **Claude** 4 d · **You** 1 d · **Needs** FND-15, D-01
Prints totals per milestone and lane, and every broken reference.
"""
import csv
import os
import re
import sys
from collections import OrderedDict

HERE = os.path.dirname(os.path.abspath(__file__))
ORDER = ["M1", "EA1", "EA2", "EA3", "EA4", "EA5", "R1", "X1", "X2"]
HEAD = re.compile(r"^### ([A-Z]{2,3}-\d+[a-z]?) · (.+)$")
META = re.compile(r"^\*\*Milestone\*\* (.+?) · \*\*Claude\*\* (.+?) · \*\*You\*\* (.+?) · \*\*Needs\*\* (.+)$")


def days(text):
    text = text.strip()
    if text in ("—", "-", ""):
        return 0.0, False
    if text.upper().startswith("TBD"):
        return 0.0, True
    m = re.match(r"^([\d.]+) d$", text)
    if not m:
        raise ValueError(f"bad estimate '{text}'")
    return float(m.group(1)), False


def main():
    tasks = []
    for fn in sorted(os.listdir(HERE)):
        if not re.match(r"^\d\d-.*\.md$", fn):
            continue
        lines = open(os.path.join(HERE, fn), encoding="utf-8").read().splitlines()
        for i, line in enumerate(lines):
            h = HEAD.match(line)
            if not h:
                continue
            m = META.match(lines[i + 1]) if i + 1 < len(lines) else None
            if not m:
                sys.exit(f"{fn}: task {h.group(1)} has no metadata line")
            milestone = m.group(1).split("→")[0].strip()
            claude, tbd1 = days(m.group(2))
            you, tbd2 = days(m.group(3))
            needs = [n.strip() for n in m.group(4).split(",") if n.strip() not in ("—", "")]
            tasks.append(OrderedDict(id=h.group(1), title=h.group(2), file=fn, milestone=milestone,
                                     milestone_span=m.group(1), claude_days=claude, you_days=you,
                                     estimate_tbd=tbd1 or tbd2, needs=" ".join(needs)))

    ids = {t["id"] for t in tasks}
    dup = [t["id"] for t in tasks if [u["id"] for u in tasks].count(t["id"]) > 1]
    problems = [f"duplicate id {d}" for d in sorted(set(dup))]
    for t in tasks:
        for n in t["needs"].split():
            if re.match(r"^D-\d+$", n) or n in ORDER or n.lower() in ("every", "r1", "task"):
                continue
            if n not in ids:
                problems.append(f"{t['id']} needs unknown task {n}")
        if t["milestone"] not in ORDER:
            problems.append(f"{t['id']} has unknown milestone {t['milestone']}")

    with open(os.path.join(HERE, "tasks.csv"), "w", newline="", encoding="utf-8") as f:
        w = csv.DictWriter(f, fieldnames=list(tasks[0].keys()))
        w.writeheader()
        for t in sorted(tasks, key=lambda t: (ORDER.index(t["milestone"]) if t["milestone"] in ORDER else 99, t["id"])):
            w.writerow(t)

    print(f"{len(tasks)} tasks -> tasks.csv")
    print(f"{'milestone':<10}{'tasks':>6}{'Claude d':>10}{'You d':>8}")
    total_c = total_y = 0.0
    # A task spanning milestones ("EA1 → R1") is spread evenly over them.
    share = {}
    for t in tasks:
        parts = [p.strip() for p in t["milestone_span"].split("→")]
        span = ORDER[ORDER.index(parts[0]):ORDER.index(parts[-1]) + 1] if len(parts) == 2 else [parts[0]]
        for ms in span:
            share.setdefault(ms, []).append((t, 1.0 / len(span)))
    for ms in ORDER:
        sel = share.get(ms, [])
        if not sel:
            continue
        c = sum(t["claude_days"] * f for t, f in sel)
        y = sum(t["you_days"] * f for t, f in sel)
        total_c += c
        total_y += y
        tbd = sum(1 for t, f in sel if t["estimate_tbd"])
        print(f"{ms:<10}{len(sel):>6}{c:>10.1f}{y:>8.1f}" + (f"   ({tbd} TBD)" if tbd else ""))
    print(f"{'total':<10}{len(tasks):>6}{total_c:>10.1f}{total_y:>8.1f}   (tasks spanning milestones counted in each)")
    by_ws = OrderedDict()
    for t in tasks:
        ws = t["id"].split("-")[0]
        c, y, n = by_ws.get(ws, (0.0, 0.0, 0))
        by_ws[ws] = (c + t["claude_days"], y + t["you_days"], n + 1)
    print("per workstream: " + ", ".join(f"{k} {n} ({c:g}/{y:g})" for k, (c, y, n) in by_ws.items()))
    if problems:
        print("PROBLEMS:")
        for p in problems:
            print("  " + p)
        sys.exit(1)


if __name__ == "__main__":
    main()
