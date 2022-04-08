using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using RimWorld;
using Verse;
using HarmonyLib;

namespace Fuel_Economy
{
	[HarmonyPatch(typeof(Dialog_LoadTransporters), "CheckForErrors")]
	class SmallPod
	{
		//private bool CheckForErrors(List<Pawn> pawns)
		public static void Postfix(ref bool __result, Dialog_LoadTransporters __instance, List<CompTransporter> ___transporters, List<TransferableOneWay> ___transferables)
		{
			if (!__result) return;

			if (!___transporters.All(t => t.parent.def == SmallPodDefOf.TransportPodSmall))
				return;

			float maxMass = ___transporters[0].Props.massCapacity;
			Log.Message($"maxMass is {maxMass}");

			Log.Message($"transferables are {___transferables}");
			if (___transferables.Any(t => t.CountToTransfer > 0 && t.AnyThing is Pawn))
			{
				Messages.Message("TD.SmallTransportPodItemsOnly".Translate(), MessageTypeDefOf.RejectInput);
				__result = false;
			}
			else if (___transferables.Any(t => t.CountToTransfer > 0 && t.AnyThing.GetStatValue(StatDefOf.Mass) > maxMass))
			{
				Messages.Message("TD.SmallTransportPodTooLarge".Translate(maxMass), MessageTypeDefOf.RejectInput);
				__result = false;
			}
		}
	}
}
