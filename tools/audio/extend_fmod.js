// Add missing alternative scores and foley without modifying existing authored events.
// Run: fmodstudiocl -script tools/audio/extend_fmod.js Art/Audio/FMOD/FirstLight.fspro
var root = studio.project.filePath.replace(/\\/g, "/").replace(/\/[^/]+$/, "");
var ws = studio.project.workspace;
function named(type, name) {
    var found = studio.project.model[type].findInstances().filter(function(x) { return x.name === name; });
    if (found.length !== 1) throw new Error("Expected one " + type + " named " + name);
    return found[0];
}
var folder = named("EventFolder", "Music");
var bus = named("MixerGroup", "Music");
var bank = named("Bank", "Flight");
var addedMusic = 0, addedFoley = 0;
function exists(name) {
    return studio.project.model.Event.findInstances().some(function(e) { return e.name === name; });
}
["FarHorizon", "NightDrift", "Starward", "BlueSilence", "GravityWell", "DawnPatrol", "AmberSkies",
 "ClockworkVoyage", "Homecoming"].forEach(function(suite) {
    if (exists(suite)) return;
    var e = ws.addEvent(suite, false);
    e.folder = folder;
    e.mixerInput.output = bus;
    e.relationships.banks.add(bank);
    e.automatableProperties.maxVoices = 1;
    // Each source stem already includes its suite's measured master gain.
    var tempo = studio.project.create("TempoMarker");
    tempo.tempo = 72;
    tempo.position = 0;
    tempo.timeline = e.timeline;
    tempo.markerTrack = e.markerTracks[0];
    ["Pad", "Strings", "Cello", "Horns", "Celeste", "Pizzicato", "Pulse"].forEach(function(name) {
        var asset = studio.project.importAudioFile(root + "/Assets/" + suite + "_" + name + ".wav");
        if (!asset) throw new Error("Missing " + suite + " " + name);
        asset.isStreaming = true;
        var track = e.addGroupTrack(name);
        var instrument = track.addSound(e.timeline, "SingleSound", 0, 106.6666666667);
        instrument.audioFile = asset;
        var p = e.addGameParameter(named("ParameterPreset", name));
        p.preset.initialValue = 0;
        var curve = track.mixerGroup.addAutomator("volume").addAutomationCurve(p.preset);
        curve.addAutomationPoint(0, -80);
        curve.addAutomationPoint(.001, -60);
        curve.addAutomationPoint(.01, -40);
        for (var i=1; i<=40; i++) curve.addAutomationPoint(i/40, 20*Math.log(i/40)/Math.LN10);
    });
    e.markerTracks[0].addRegion(0, 106.6666666667, "Loop", studio.project.regionLoopMode.Looping);
    addedMusic++;
});
var vesselFolder = named("EventFolder", "Vessel");
var effects = named("MixerGroup", "Effects");
var specs = [
    ["Dock_Latch_01", .9], ["Dock_Latch_02", .9],
    ["Undock_Release_01", .7], ["Undock_Release_02", .7],
    ["Leg_Servo_01", 1.6], ["Leg_Servo_02", 1.6],
    ["Hatch_Open", 1], ["Hatch_Close", 1],
    ["Step_Metal_01", .45], ["Step_Metal_02", .45], ["Step_Metal_03", .45],
    ["Step_Surface_01", .45], ["Step_Surface_02", .45], ["Step_Surface_03", .45],
    ["Jetpack_Loop", 4]
];
specs.forEach(function(spec) {
    if (exists(spec[0])) return;
    var e = ws.addEvent(spec[0], spec[0] !== "Jetpack_Loop");
    e.folder = vesselFolder;
    e.mixerInput.output = effects;
    e.relationships.banks.add(bank);
    e.automatableProperties.maxVoices = 3;
    e.automatableProperties.triggerCooldown = .08;
    e.automatableProperties.minimumDistance = 3;
    e.automatableProperties.maximumDistance = 250;
    e.masterTrack.mixerGroup.volume = -7;
    var asset = studio.project.importAudioFile(root + "/Assets/" + spec[0] + ".wav");
    if (!asset) throw new Error("Missing " + spec[0]);
    var instrument = e.addGroupTrack(spec[0]).addSound(e.timeline, "SingleSound", 0, spec[1]);
    instrument.audioFile = asset;
    if (spec[0] === "Jetpack_Loop")
        e.markerTracks[0].addRegion(0, spec[1], "Loop", studio.project.regionLoopMode.Looping);
    addedFoley++;
});
if (!studio.project.save()) throw new Error("FMOD project save failed");
if (!studio.project.build({platforms:"Desktop"})) throw new Error("FMOD bank build failed");
studio.project.exportGUIDs();
console.log("Added " + addedMusic + " scores and " + addedFoley + " foley events; existing events preserved.");
