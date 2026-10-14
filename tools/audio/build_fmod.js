// Run with fmodstudiocl -script tools/audio/build_fmod.js Art/Audio/FMOD/FirstLight.fspro.
// Seeds an empty project once; subsequent sound design belongs in the saved Studio project.
var root = studio.project.filePath.replace(/\\/g, "/").replace(/\/[^/]+$/, "");
var audioRoot = root + "/../FirstLight/";
if (studio.project.model.Event.findInstances().length !== 0) {
    throw new Error("This project already has events. Build its saved banks normally; do not overwrite authored events.");
}
var ws = studio.project.workspace;
ws.platforms.forEach(function(platform) {
    if (!platform.encodingSettings) {
        var encoding = studio.project.create("EncodingSetting");
        encoding.platform = platform;
        encoding.encodable = platform;
        encoding.encodingFormat = 3;
        encoding.quality = 55;
    }
});
var bank = studio.project.create("Bank");
bank.name = "Flight";
bank.folder = ws.masterBankFolder;
var master = ws.mixer.masterBus;
var limiter = master.effectChain.addEffect("LimiterEffect");
limiter.ceiling = -1;
limiter.release = 100;
function bus(name, db) {
    var result = studio.project.create("MixerGroup");
    result.name = name;
    result.output = master;
    result.volume = db;
    return result;
}
var musicBus = bus("Music", 0);
var effectsBus = bus("Effects", -6);
var folders = {};
function event(folderName, name, spatial, output) {
    if (!folders[folderName]) {
        var folder = studio.project.create("EventFolder");
        folder.name = folderName;
        folder.folder = ws.masterEventFolder;
        folders[folderName] = folder;
    }
    var result = ws.addEvent(name, spatial);
    result.folder = folders[folderName];
    result.mixerInput.output = output;
    result.relationships.banks.add(bank);
    result.automatableProperties.maxVoices = spatial ? 4 : 1;
    result.automatableProperties.minimumDistance = 10;
    result.automatableProperties.maximumDistance = 1200;
    return result;
}
function parameter(e, name, initial) {
    var proxy = e.addGameParameter({name:name, type:studio.project.parameterType.User, min:0, max:1});
    proxy.preset.initialValue = initial;
    return proxy.preset;
}
function volumeCurve(object, p, points) {
    var curve = object.addAutomator("volume").addAutomationCurve(p);
    points.forEach(function(point) { curve.addAutomationPoint(point[0], point[1]); });
}
function linearGain(object, p) {
    var points = [[0, -80], [.001, -60], [.01, -40]];
    for (var i = 1; i <= 40; i++) points.push([i / 40, 20 * Math.log(i / 40) / Math.LN10]);
    volumeCurve(object, p, points);
}
function sound(e, name, file, length, streaming) {
    var asset = studio.project.importAudioFile(audioRoot + file);
    if (!asset) throw new Error("Missing source audio: " + file);
    asset.isStreaming = streaming;
    var track = e.addGroupTrack(name);
    var instrument = track.addSound(e.timeline, "SingleSound", 0, length);
    instrument.audioFile = asset;
    return track;
}
function loop(e, length) {
    e.markerTracks[0].addRegion(0, length, "Loop", studio.project.regionLoopMode.Looping);
}
var music = event("Music", "Flight", false, musicBus);
music.masterTrack.mixerGroup.volume = 20 * Math.log(2.3610964839058837) / Math.LN10;
var tempo = studio.project.create("TempoMarker");
tempo.tempo = 72;
tempo.position = 0;
tempo.timeline = music.timeline;
tempo.markerTrack = music.markerTracks[0];
["Pad", "Strings", "Cello", "Horns", "Celeste", "Pizzicato", "Pulse"].forEach(function(name) {
    var track = sound(music, name, "Stems/" + name + ".wav", 106.6666666667, true);
    linearGain(track.mixerGroup, parameter(music, name, 0));
});
loop(music, 106.6666666667);

var engine = event("Vessel", "Engine", false, effectsBus);
var low = sound(engine, "Structure and low roar", "SFX/Engine_Low_Loop.wav", 12, false);
var high = sound(engine, "External roar", "SFX/Engine_High_Loop.wav", 12, false);
var air = parameter(engine, "AirPresence", 1);
linearGain(engine.masterTrack.mixerGroup, parameter(engine, "EngineOutput", 0));
volumeCurve(low.mixerGroup, air, [[0, -16], [.2, -7], [1, -3]]);
linearGain(high.mixerGroup, air);
engine.masterTrack.mixerGroup.volume = -7;
loop(engine, 12);

[
    ["Wind", "Atmosphere_Wind_Loop", true],
    ["Reentry", "Reentry_Plasma_Loop", true],
    ["Ignition", "Engine_Ignition", false, 4],
    ["Shutdown", "Engine_Shutdown", false, 2],
    ["RCS", "RCS_Burst", false, .4],
    ["Stage01", "Stage_Separation_01", false, 3],
    ["Stage02", "Stage_Separation_02", false, 3],
    ["Stage03", "Stage_Separation_03", false, 3],
    ["Touchdown", "Touchdown", false, 2],
    ["Explosion", "Explosion", false, 5],
    ["Parachute", "Parachute_Deploy", false, 3]
].forEach(function(spec) {
    var e = event("Vessel", spec[0], !spec[2], effectsBus);
    sound(e, spec[0], "SFX/" + spec[1] + ".wav", spec[2] ? 12 : spec[3], false);
    e.masterTrack.mixerGroup.volume = -7;
    if (spec[2]) loop(e, 12);
    else e.automatableProperties.triggerCooldown = .08;
});
if (!studio.project.save()) throw new Error("FMOD project save failed");
if (!studio.project.build({platforms:"Desktop"})) throw new Error("FMOD bank build failed");
studio.project.exportGUIDs();
console.log("First Light: authored and built flight music and vessel effects.");
