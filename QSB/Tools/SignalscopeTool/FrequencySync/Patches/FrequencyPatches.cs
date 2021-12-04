﻿using HarmonyLib;
using QSB.Events;
using QSB.Patches;

namespace QSB.Tools.SignalscopeTool.FrequencySync.Patches
{
	[HarmonyPatch]
	public class FrequencyPatches : QSBPatch
	{
		public override QSBPatchTypes Type => QSBPatchTypes.OnClientConnect;

		[HarmonyPostfix]
		[HarmonyPatch(typeof(AudioSignal), nameof(AudioSignal.IdentifyFrequency))]
		public static void IdentifyFrequencyEvent(SignalFrequency ____frequency)
			=> QSBEventManager.FireEvent(EventNames.QSBIdentifyFrequency, ____frequency);

		[HarmonyPostfix]
		[HarmonyPatch(typeof(AudioSignal), nameof(AudioSignal.IdentifySignal))]
		public static void IdentifySignalEvent(SignalName ____name)
			=> QSBEventManager.FireEvent(EventNames.QSBIdentifySignal, ____name);
	}
}