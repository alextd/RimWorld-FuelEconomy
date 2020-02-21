using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Verse;
using RimWorld;
using UnityEngine;


namespace Fuel_Economy
{
	[DefOf]
	public static class SmallPodDefOf
	{
		public static ThingDef TransportPodSmall;
	}

	static class LaunchableMethods
	{
		public static float smallPodEfficiency = 5;
		public static float FuelNeededToLaunchAtDist(float dist, CompLaunchable launchable)
		{
			float fuelForPod = MassFactor(launchable) * dist;
			if (launchable.parent.def == SmallPodDefOf.TransportPodSmall)
				fuelForPod /= smallPodEfficiency;
			Log.Message($"Needs {fuelForPod}");
			return fuelForPod;
		}

		static int vanillaMax = 66; //150 / 2.25
		public static int MaxLaunchDistanceAtFuelLevel(float fuelLevel, CompLaunchable launchable)
		{
			float distance = fuelLevel / MassFactor(launchable);
			if (launchable.parent.def == SmallPodDefOf.TransportPodSmall)
				distance *= smallPodEfficiency;
			if (!Settings.Get().pastVanillaMaxRange && distance > vanillaMax)
				distance = vanillaMax;
			Log.Message($"Can do {distance}");
			return Mathf.FloorToInt(distance);
		}

		// emptyPercent of 50% means a pod weighs 150kg, so a full pod weighs 300kg and an empty pod costs 50% fuel, and can go 2x as far
		// 0% means it's weightless, can send empty pod with 0% fuel
		// 100% means essentially infinite weight, all fuel is used to launch pod and the content doesn't matter, and that's vanilla so why do that
		const float FuelPerTile = 2.25f;
		public static float MassFactor(CompLaunchable launchable)
		{
			float emptyPercent = Settings.Get().emptyPercent;
			return ((FuelPerTile * emptyPercent) + (FuelPerTile - FuelPerTile * emptyPercent) * PercentFull(launchable));
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
					mass += t.GetStatValue(StatDefOf.Mass) * t.stackCount;
				}
				max += transporter.Props.massCapacity;
			}
			return mass / max;
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
				if (i.opcode == OpCodes.Call && i.operand.Equals(FuelNeededToLaunchAtDistInfo))
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
	
	[HarmonyPatch(typeof(CompLaunchable), "MaxLaunchDistance", MethodType.Getter)]
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
				if (i.opcode == OpCodes.Call && i.operand.Equals(MaxLaunchDistanceAtFuelLevel))
				{
					i.operand = MaxLaunchDistanceAtFuelLevelPatch;
					yield return new CodeInstruction(OpCodes.Ldarg_0); //this
				}
				yield return i;
			}
		}
	}
}