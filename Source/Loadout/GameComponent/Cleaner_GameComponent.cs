
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;
using HarmonyLib;
using Verse.Profile;
using System.Diagnostics;

namespace CombatExtended.ExtendedLoadout;

[AttributeUsage(AttributeTargets.Method)]
public class ClearDataOnNewGame : Attribute {}

[HarmonyPatch(typeof(MemoryUtility), "ClearAllMapsAndWorld")]
public static class Harmony_ClearAllMapsAndWorld
{
    public static void Prefix()
    {
        DbgLog.Wrn($"ExtendedLoadout clearing initiated.");
        if (_clearDataMethods == null)
            _clearDataMethods = GetClearingMethods();
        _clearDataMethods?.ForEach(x => x.Invoke(null, null));
    }

    private static List<MethodInfo>? GetClearingMethods()
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        var allTypesInAsm = Assembly.GetExecutingAssembly().GetTypes();
        var methods = allTypesInAsm
            .SelectMany(x => x.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            .Where(x => x.HasAttribute<ClearDataOnNewGame>())
            .ToList();
        stopwatch.Stop();
        Log.Message($"It took {stopwatch.ElapsedMilliseconds} ms to find all types and using selectmany to find methods has ClearDataOnNewGameAttribute");
        stopwatch.Start();
        var allTypesInAsmAlt = GenTypes.allTypesCached;
        var methodsAlt = GenTypes.allTypesCached.SelectMany(x => x.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            .Where(x => x.HasAttribute<ClearDataOnNewGame>())
            .ToList();
        stopwatch.Stop();
        Log.Message($"It took {stopwatch.ElapsedMilliseconds} ms to find all methods using GenTypes");
        Log.Message($"The two results {(methods == methodsAlt ? "are" : "aren't")} identical.");
        DbgLog.Wrn($"CollectClearingMethods: {String.Join("; ", methods.Select(x => $"{x.DeclaringType.Name}:{x.Name}"))}");
        return methods;
    }

    private static List<MethodInfo>? _clearDataMethods;
}