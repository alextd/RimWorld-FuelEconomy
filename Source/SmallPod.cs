using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Harmony;

namespace Fuel_Economy
{
	[HarmonyPatch(typeof(Dialog_LoadTransporters), "CheckForErrors")]
	class SmallPod
	{
		//private bool CheckForErrors(List<Pawn> pawns)
		public static void Postfix(ref bool __result, Dialog_LoadTransporters __instance)
		{
			if (!__result) return;

			List<CompTransporter> transporters = (List<CompTransporter>)AccessTools.Field(typeof(Dialog_LoadTransporters), "transporters").GetValue(__instance);

			if (!transporters.All(t => t.parent.def == SmallPodDefOf.TransportPodSmall))
				return;

			float maxMass = transporters[0].Props.massCapacity;
			Log.Message($"maxMass is {maxMass}");

			List <TransferableOneWay> transferables = (List<TransferableOneWay>)AccessTools.Field(typeof(Dialog_LoadTransporters), "transferables").GetValue(__instance);
			Log.Message($"transferables are {transferables}");
			if (transferables.Any(t => t.CountToTransfer > 0 && t.AnyThing is Pawn))
			{
				Messages.Message("TD.SmallTransportPodItemsOnly".Translate(), MessageTypeDefOf.RejectInput);
				__result = false;
			}
			else if (transferables.Any(t => t.CountToTransfer > 0 && t.AnyThing.GetStatValue(StatDefOf.Mass) > maxMass))
			{
				Messages.Message("TD.SmallTransportPodTooLarge".Translate(new object[] { maxMass }), MessageTypeDefOf.RejectInput);
				__result = false;
			}
		}
	}
}
