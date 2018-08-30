using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using Harmony;
using Verse;
using RimWorld;
using UnityEngine;


namespace Fuel_Economy
{
	static class LaunchableMethods
	{
		public static float FuelNeededToLaunchAtDist(float dist, CompLaunchable launchable)
		{
			return FuelNeededToLaunchAtDist(dist, PercentFull(launchable));
		}
		public static float FuelNeededToLaunchAtDist(float dist, float percentFull)
		{
			float ret = (MassFactor(percentFull) * dist);
			Log.Message("Needs " + ret);
			return ret;
		}

		public static int MaxLaunchDistanceAtFuelLevel(float fuelLevel, CompLaunchable launchable)
		{
			return MaxLaunchDistanceAtFuelLevel(fuelLevel, PercentFull(launchable));
		}
		static int vanillaMax = 66; //150 / 2.25
		public static int MaxLaunchDistanceAtFuelLevel(float fuelLevel, float percentFull)
		{
			int ret = Mathf.FloorToInt(fuelLevel / MassFactor(percentFull));
			if (!Settings.Get().pastVanillaMaxRange && ret > vanillaMax)
				ret = vanillaMax;
			Log.Message("Can do " + ret);
			return ret;
		}

		// emptyPercent of 50% means a pod weighs 150kg, so a full pod weighs 300kg and an empty pod costs 50% fuel, and can go 2x as far
		// 0% means it's weightless, can send empty pod with 0% fuel
		// 100% means essentially infinite weight, all fuel is used to launch pod and the content doesn't matter, and that's vanilla so why do that
		public static float MassFactor(float percentFull)
		{
			float emptyPercent = Settings.Get().emptyPercent;
			return ((2.25f * emptyPercent) + (2.25f - 2.25f*emptyPercent) * percentFull);
		}

		public static float PercentFull(CompLaunchable launchable)
		{
			float mass = 0;
			float max = 0;
			List<CompTransporter> transporters = launchable.TransportersInGroup;
			foreach (CompTransporter transporter in transporters)
			{
				foreach (Thing t in transporter.GetDirectlyHeldThings())
				{
					Log.Message("thing " + t + " mass is " + t.GetStatValue(StatDefOf.Mass));
					mass += t.GetStatValue(StatDefOf.Mass);
				}
				Log.Message("transporter " + transporter + " massCap is " + transporter.Props.massCapacity);
				max += transporter.Props.massCapacity;
			}
			Log.Message("Max is " + max);
			return mass / max;
		}
	}

	[HarmonyPatch(typeof(CompLaunchable), "MaxLaunchDistanceAtFuelLevel")]
	public static class MaxLaunchDistanceAtFuelLevelPatch
	{
		public static void Postfix(float fuelLevel, ref int __result)
		{
			__result = LaunchableMethods.MaxLaunchDistanceAtFuelLevel(fuelLevel, 0f);
		}
	}

	[HarmonyPatch(typeof(CompLaunchable), "TryLaunch")]
	//private void TryLaunch(GlobalTargetInfo target, PawnsArriveMode arriveMode, bool attackOnArrival)
	public static class TryLaunchPatch
	{
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> codeInstructions)
		{
			MethodInfo FuelNeededToLaunchAtDistInfo = AccessTools.Method(typeof(CompLaunchable), "FuelNeededToLaunchAtDist");
			MethodInfo FuelNeededToLaunchAtDistInfoPatch = AccessTools.Method(typeof(LaunchableMethods), "FuelNeededToLaunchAtDist",
				new Type[] { typeof(float), typeof(CompLaunchable) });

			foreach (CodeInstruction i in codeInstructions)
			{
				if (i.opcode == OpCodes.Call && i.operand == FuelNeededToLaunchAtDistInfo)
				{
					i.operand = FuelNeededToLaunchAtDistInfoPatch;
					yield return new CodeInstruction(OpCodes.Ldarg_0); //this
				}
				yield return i;
			}
		}
	}

	[HarmonyPatch(typeof(CompLaunchable), "ChoseWorldTarget")]
	//private void TryLaunch(GlobalTargetInfo target, PawnsArriveMode arriveMode, bool attackOnArrival)
	static class ChoseWorldTargetPatch
	{
		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> codeInstructions)
		{
			return TryLaunchPatch.Transpiler(codeInstructions);
		}
	}
	
	[HarmonyPatch(typeof(CompLaunchable))]
	[HarmonyPatch("MaxLaunchDistance", PropertyMethod.Getter)]
	public static class MaxLaunchDistance_Patch
	{
		//private void TryLaunch(GlobalTargetInfo target, PawnsArriveMode arriveMode, bool attackOnArrival)
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> codeInstructions)
		{
			MethodInfo MaxLaunchDistanceAtFuelLevel = AccessTools.Method(typeof(CompLaunchable), "MaxLaunchDistanceAtFuelLevel");
			MethodInfo MaxLaunchDistanceAtFuelLevelPatch = AccessTools.Method(typeof(LaunchableMethods), "MaxLaunchDistanceAtFuelLevel",
				new Type[] { typeof(float), typeof(CompLaunchable) });

			foreach (CodeInstruction i in codeInstructions)
			{
				if (i.opcode == OpCodes.Call && i.operand == MaxLaunchDistanceAtFuelLevel)
				{
					i.operand = MaxLaunchDistanceAtFuelLevelPatch;
					yield return new CodeInstruction(OpCodes.Ldarg_0); //this
				}
				yield return i;
			}
		}
	}
	
	[HarmonyPatch(typeof(CompLaunchable))]
	[HarmonyPatch("MaxLaunchDistanceEverPossible", PropertyMethod.Getter)]
	static class MaxLaunchDistanceEverPossible_Patch
	{
		//private void TryLaunch(GlobalTargetInfo target, PawnsArriveMode arriveMode, bool attackOnArrival)
		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> codeInstructions)
		{
			return MaxLaunchDistance_Patch.Transpiler(codeInstructions);
		}
	}
}