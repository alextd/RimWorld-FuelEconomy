﻿using System;
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
		public static float smallPodEfficiency = 5;	//TODO DefExtensions to make this an xml value?
		public static float FuelNeededToLaunchAtDist(float dist, CompLaunchable launchable)
		{
			float fuelForPod = FuelPerTile(launchable) * dist;
			Log.Message($"At {dist}, {launchable.parent} needs {fuelForPod} fuel with {FuelPerTile(launchable)} Fuel per file");
			return fuelForPod;
		}

		static int vanillaMax = 66; //150 / 2.25
		public static int MaxLaunchDistanceAtFuelLevel(float fuelLevel, CompLaunchable launchable)
		{
			float distance = fuelLevel / FuelPerTile(launchable);

			if (!Mod.settings.pastVanillaMaxRange && distance > vanillaMax)
				distance = vanillaMax;

			Log.Message($"Can do {distance} for {distance* FuelPerTile(launchable)}/{fuelLevel} fuel with {FuelPerTile(launchable)} Fuel per file");
			return Mathf.FloorToInt(distance);
		}

		// emptyPercent of 50% means a pod weighs 150kg, so a full pod weighs 300kg and an empty pod costs 50% fuel, and can go 2x as far
		// 0% means it's weightless, can send empty pod with 0% fuel
		// 100% means essentially infinite weight, all fuel is used to launch pod and the content doesn't matter, and that's vanilla so why do that
		const float vanillaFuelPerTile = 2.25f;
		public static float FuelPerTile(CompLaunchable launchable)
		{
			float emptyPercent = Mod.settings.emptyPercent;
			float fuelPerTileBase = vanillaFuelPerTile * (emptyPercent + (1 - emptyPercent) * PercentFull(launchable));
			
			if (launchable.parent.def == SmallPodDefOf.TransportPodSmall)
				return fuelPerTileBase / smallPodEfficiency;  //TODO: DefExtension and just divide by that value
			return fuelPerTileBase;
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
				if (i.Calls(FuelNeededToLaunchAtDistInfo))
				{
					yield return new CodeInstruction(OpCodes.Ldarg_0); //this
					yield return new CodeInstruction(OpCodes.Call, FuelNeededToLaunchAtDistInfoPatch);
				}
				else
					yield return i;
			}
		}
	}

	[HarmonyPatch(typeof(CompLaunchable), "ChoseWorldTarget", new Type[] { typeof(RimWorld.Planet.GlobalTargetInfo)})]
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
			MethodInfo MaxLaunchDistanceAtFuelLevelInfo = AccessTools.Method(typeof(CompLaunchable), nameof(CompLaunchable.MaxLaunchDistanceAtFuelLevel));
			MethodInfo MaxLaunchDistanceAtFuelLevelPatch = AccessTools.Method(typeof(LaunchableMethods), nameof(LaunchableMethods.MaxLaunchDistanceAtFuelLevel),
				new Type[] { typeof(float), typeof(CompLaunchable) });

			foreach (CodeInstruction i in codeInstructions)
			{
				if (i.Calls(MaxLaunchDistanceAtFuelLevelInfo))
				{
					yield return new CodeInstruction(OpCodes.Ldarg_0); //this
					yield return new CodeInstruction(OpCodes.Call, MaxLaunchDistanceAtFuelLevelPatch);
				}
				else
					yield return i;
			}
		}
	}
}