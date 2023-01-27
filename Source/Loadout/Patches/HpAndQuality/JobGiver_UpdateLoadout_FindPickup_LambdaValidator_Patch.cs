using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using Verse;

namespace CombatExtended.ExtendedLoadout;

[HarmonyPatch]
public class JobGiver_UpdateLoadout_FindPickup_LambdaValidator_Patch
{
    private static bool Prepare()
    {
        return ExtendedLoadoutMod.Instance.useHpAndQualityInLoadouts;
    }

    [UsedImplicitly]
    public static MethodBase TargetMethod()
    {
        return AccessTools.Method(AccessTools.Inner(typeof(JobGiver_UpdateLoadout), "<>c__DisplayClass8_0"), "<FindPickup>b__3");
    }

    [HarmonyTranspiler]
    [UsedImplicitly]
    public static IEnumerable<CodeInstruction> FindPickup_Validator_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilGen)
    {
        List<CodeInstruction> list = instructions.ToList();
        /*
          IL_000e:  ldarg.1
          IL_000f:  ldarg.0
          IL_0010:  ldfld      class ['Assembly-CSharp']Verse.Pawn CombatExtended.JobGiver_UpdateLoadout/'<>c__DisplayClass6_0'::pawn
          IL_0015:  call       bool ['Assembly-CSharp']RimWorld.ForbidUtility::IsForbidden(class ['Assembly-CSharp']Verse.Thing, class ['Assembly-CSharp']Verse.Pawn)
          IL_001a:  brtrue.s   IL_0043
         */
        MethodInfo isForbidden = AccessTools.Method(typeof(ForbidUtility), "IsForbidden", new Type[2]
        {
            typeof(Thing),
            typeof(Pawn)
        });
        FieldInfo operand = AccessTools.Field(AccessTools.Inner(typeof(JobGiver_UpdateLoadout), "<>c__DisplayClass8_0"), "pawn");
        int num = list.FindIndex((CodeInstruction ci) => ci.Calls(isForbidden));
        if (num == -1)
        {
            Log.Error("Can't find IsForbidden in ce findPickup_validator");
            return list;
        }
        num += 2;
        list.InsertRange(num, new CodeInstruction[5]
        {
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Ldfld, operand),
            new CodeInstruction(OpCodes.Ldarg_1),
            new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(JobGiver_UpdateLoadout_FindPickup_LambdaValidator_Patch), "AllowEquip")),
            new CodeInstruction(OpCodes.Brfalse_S, list[num - 1].operand)
        });
        return list;
    }

    public static bool AllowEquip(Pawn p, Thing t)
    {
        if (!t.def.IsWeapon)
        {
            return true;
        }
        Loadout loadout = p.GetLoadout();
        if (loadout == null)
        {
            return true;
        }
        if (loadout is Loadout_Multi loadout_Multi)
        {
            loadout = loadout_Multi.FindLoadoutWithThingDef(t.def);
            if (loadout == null)
            {
                return true;
            }
        }
        return loadout.Extended().Allows(t);
    }
}
