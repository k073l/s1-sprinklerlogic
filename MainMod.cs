using System.Collections;
using MelonLoader;
using BigSprinklerLogic.Helpers;
using HarmonyLib;
using S1API.Items;
using S1API.Shops;
using UnityEngine;
#if MONO
using ScheduleOne.ObjectScripts;
using ScheduleOne.Tiles;
using System.Collections;
#else
using Il2CppScheduleOne.ObjectScripts;
using Il2CppScheduleOne.Tiles;
using Il2CppSystem.Collections.Generic;
#endif

[assembly: MelonInfo(
    typeof(BigSprinklerLogic.BigSprinklerLogic),
    BigSprinklerLogic.BuildInfo.Name,
    BigSprinklerLogic.BuildInfo.Version,
    BigSprinklerLogic.BuildInfo.Author
)]
[assembly: MelonColor(1, 11, 57, 84)]
[assembly: MelonGame("TVGS", "Schedule I")]

namespace BigSprinklerLogic;

public static class BuildInfo
{
    public const string Name = "BigSprinklerLogic";
    public const string Description = "Makes the big sprinkler work.";
    public const string Author = "k073l";
    public const string Version = "1.0.0";
}

public class BigSprinklerLogic : MelonMod
{
    private static MelonLogger.Instance Logger;
    private bool _shopsReady;

    public override void OnInitializeMelon()
    {
        Logger = LoggerInstance;
        Logger.Msg("BigSprinklerLogic initialized");
    }

    public override void OnSceneWasLoaded(int buildIndex, string sceneName)
    {
        switch (sceneName)
        {
            case "Menu":
                _shopsReady = false;
                break;
            case "Main":
                MelonCoroutines.Start(AddDelayed());
                break;
        }
    }

    private IEnumerator AddDelayed()
    {
        yield return new WaitForSeconds(2f);

        if (_shopsReady) yield break;

        var item = ItemManager.GetItemDefinition("bigsprinkler");
        if (item == null)
        {
            Logger.Error("Could not find bigsprinkler item definition!");
            yield break;
        }

        var addedCount = ShopManager.AddToCompatibleShops(item);
        Logger.Msg($"Added bigsprinkler to {addedCount} shops.");
        _shopsReady = true;
    }
}

[HarmonyPatch(typeof(Sprinkler), "GetPots")]
public class SprinklerPatches
{
    [HarmonyWrapSafe]
    public static bool Prefix(
        Sprinkler __instance,
#if MONO
        ref System.Collections.Generic.List<Pot> __result
#else
        ref Il2CppSystem.Collections.Generic.List<Pot> __result
#endif
    )
    {
        var go = __instance.gameObject;
        var arrow = go.transform.Find("Arrow");
        if (arrow != null)
        {
            MelonDebug.Msg("Normal Sprinkler called");
            return true;
        }

        MelonDebug.Msg("Arrow not found, applying big sprinkler logic");

        var origin = new Coordinate(__instance._originCoordinate);

        var offsets = new System.Collections.Generic.List<Coordinate>();

        const int minX = -1;
        const int maxX = 2;
        const int minY = -1;
        const int maxY = 2;
        for (var x = minX; x <= maxX; x++)
        {
            offsets.Add(new Coordinate(x, minY)); // bottom row
            offsets.Add(new Coordinate(x, maxY)); // top row
        }

        for (var y = minY + 1; y <= maxY - 1; y++)
        {
            offsets.Add(new Coordinate(minX, y)); // left column
            offsets.Add(new Coordinate(maxX, y)); // right column
        }

        // rotate offsets
        var coords = offsets
            .Select(offset => origin + Coordinate.RotateCoordinates(offset, __instance._rotation))
            .ToList();

        var pots = new System.Collections.Generic.HashSet<Pot>();

        foreach (var c in coords)
        {
            var tile = __instance.OwnerGrid.GetTile(c);
            if (tile == null) continue;

            foreach (var occupant in tile.BuildableOccupants)
                if (Utils.Is<Pot>(occupant, out var pot))
                    pots.Add(pot);
        }
#if MONO
        __result = pots.ToList();
#else
        __result = pots.ToIl2CppList();
#endif

        return false;
    }
}