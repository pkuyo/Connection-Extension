<h1>Connection Extension</h1>

A Rain World mod that added the functionality to specify exits for room connections, allowing for multiple connections between two rooms or self-connections within a single room.

<h2>Usage</h2>

`RoomName` : `TargetRoom`<`Target Exit Index`> , ...

For usage examples, please refer to the `CTET` (Connection Extension Sample Region) included with this mod.

<h2>Compatibility Instructions</h2>

Add `ExtendHelperImport` (can be found at [ExtendHelperImport](./Helpers/ExtensionImport.cs) ).

Call `typeof(ExtendHelperImport).ModInterop();` in your mod's `OnEnable` method. (Requires `using MonoMod.ModInterop` and a reference to `MonoMod.Utils.dll`.)

Replace `AbstractRoom.ExitIndex` with `AbstractRoom.ExitIndexWithEntrance`.

<b>Note:</b> These changes do <b>not</b> require adding this mod as a dependency.
