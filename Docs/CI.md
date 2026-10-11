# Builds, tests and CI

## Players

| Target | Menu | Command line | Output |
|---|---|---|---|
| Windows x64 (Mono) | *TAP → Build Windows Player* | `Unity -batchmode -quit -executeMethod TAP.EditorTools.BuildScript.BuildWindowsBatch` | `Build/Windows/TAP.exe` |
| Linux x64 (Mono) | *TAP → Build Linux Player* | `… BuildScript.BuildLinuxBatch` | `Build/Linux/TAP.x86_64` |

Both use the Mono scripting backend, because plugin mods are loaded as assemblies (D-11). The Linux build needs the editor module
*Linux Build Support (Mono)*: `unity install-modules -e 6000.5.7f1 -m linux-mono` (56 MB download, 206 MB on disk).

The version is `git describe --tags --always --dirty` (for example `v0.1.0-14-ga5e1d8a`) stamped into the player as
`Application.version`. The main menu's footer shows it and the log starts with it:

```
[TAP] The Astraea Program v0.1.0-14-ga5e1d8a · WindowsPlayer · Unity 6000.5.7f1 · Windows 11 … · NVIDIA GeForce …
```

Tag a release (`git tag v0.1.0`) to give builds a clean number; without tags the version is the short commit.

## Mission autotests

`TAP.exe -batchmode -autotest <mission> -report <file.md> -logFile <file.log>` flies one mission with the scripted pilot and writes
a report whose second line is `Result: PASSED` or `Result: FAILED`. Missions: `flow` (the scene loop through the real screens),
`orbit`, `suborbital`, `persistence`, `docking`, `failures`, `escape`, `lunar` (about an hour), and `locale` (the text audit,
see [LOCALIZATION.md](LOCALIZATION.md)). **Always pass `-batchmode`**: without it a window opens and keys pressed into it fly the rocket.

`tools/ci/run-missions.ps1` (Windows) and `tools/ci/run-missions.sh` (Linux) run a list of them one after another and print a
table with the result of each, in the job summary too when they run in GitHub Actions. The exit code is 1 when a mission did not pass.

```
./tools/ci/run-missions.ps1 -Exe Build/Windows/TAP.exe -Out results -Missions "orbit lunar"
TAP_ARGS=-nographics tools/ci/run-missions.sh Build/Linux/TAP.x86_64 results orbit lunar
```

## GitHub Actions

`.github/workflows/ci.yml`, on every push to `main` and on pull requests (GameCI):

1. **EditMode tests** with a test report as a check and as an artifact. The two Celestial Body Lab tests that open an editor
   window are in the category `NeedsGraphics` and are left out (no display on the runner).
2. **Players**, Windows and Linux, as artifacts (`TAP-StandaloneWindows64`, `TAP-StandaloneLinux64`), built by
   `BuildScript.CiBuild`, which stamps the version.

It needs three repository secrets (*Settings → Secrets and variables → Actions*): `UNITY_LICENSE`, `UNITY_EMAIL`,
`UNITY_PASSWORD`. For a personal licence, follow GameCI's activation steps
(<https://game.ci/docs/github/activation>) and paste the resulting `.ulf` file as `UNITY_LICENSE`. The checkout uses Git LFS
and the full history (for `git describe`); LFS bandwidth counts against the account.

`.github/workflows/nightly.yml`, every night at 01:00 and by hand (*Run workflow*, with an optional list of missions): builds
the Windows player and runs every mission on a **self-hosted runner** (the developer's PC):

1. *Settings → Actions → Runners → New self-hosted runner*, Windows, and run `config.cmd` with the extra labels `windows,tap`.
2. Install it as a service, or start `run.cmd` before the night.
3. The Unity editor named in `ProjectSettings/ProjectVersion.txt` must be installed (found under `D:\Programs\Unity\<version>` or
   `C:\Program Files\Unity\Hub\Editor\<version>`; or set the machine variable `UNITY_PATH` to its `Unity.exe`). No other editor
   may have the project open while the job builds.

The runner keeps its workspace, so only the first night imports the whole project.

## A real Linux machine

The first run on Linux (Ubuntu LTS with the Steam runtime) is checked by hand:

1. Build with *TAP → Build Linux Player* (or take the CI artifact) and copy `Build/Linux` to the machine.
2. `chmod +x TAP.x86_64`, then `TAP_ARGS=-nographics tools/ci/run-missions.sh TAP.x86_64 results orbit lunar`. Without a GPU or
   display use `-nographics`, or put `xvfb-run -a` in front.
3. Saves and settings are in `~/.config/unity3d/Astraea Works/The Astraea Program/TAP`.

The terrain heights and biomes are bit-for-bit the same on Linux (FND-02's fingerprint check ran on Ubuntu 24.04).
