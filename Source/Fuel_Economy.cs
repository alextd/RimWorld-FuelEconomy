using System.Reflection;
using System.Linq;
using Verse;
using UnityEngine;
using HarmonyLib;
using RimWorld;

namespace Fuel_Economy
{
	public class Mod : Verse.Mod
	{
		public static Settings settings;
		public Mod(ModContentPack content) : base(content)
		{
			// initialize settings
			settings = GetSettings<Settings>();
#if DEBUG
			Harmony.DEBUG = true;
#endif
			Harmony harmony = new Harmony("Uuugggg.rimworld.Fuel_Economy.main");

			harmony.PatchAll();
		}

		public override void DoSettingsWindowContents(Rect inRect)
		{
			base.DoSettingsWindowContents(inRect);
			settings.DoWindowContents(inRect);
		}

		public override string SettingsCategory()
		{
			return "TD.FuelEconomy".Translate();
		}
	}
}