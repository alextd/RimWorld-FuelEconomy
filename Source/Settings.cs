using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;

namespace Fuel_Economy
{
	class Settings : ModSettings
	{
		public bool pastVanillaMaxRange = false;
		public float emptyPercent = 0.5f;

		public static Settings Get()
		{
			return LoadedModManager.GetMod<Fuel_Economy.Mod>().GetSettings<Settings>();
		}

		public void DoWindowContents(Rect wrect)
		{
			var options = new Listing_Standard();
			options.Begin(wrect);

			options.Label("TD.FuelSettingsDesc".Translate());
			options.CheckboxLabeled("TD.SettingExtendRange".Translate(), ref pastVanillaMaxRange);

			options.Gap();
			options.Label("TD.SettingEmptyPercent".Translate() + $" ({emptyPercent:P})");
			emptyPercent = options.Slider(emptyPercent, 0, 1);
			options.Label("TD.EmptyPercentDesc".Translate());
			
			options.End();
		}
		
		public override void ExposeData()
		{
			Scribe_Values.Look(ref pastVanillaMaxRange, "vanillaMaxRange", false);
			Scribe_Values.Look(ref emptyPercent, "emptyPercent", 0.5f);
		}
	}
}