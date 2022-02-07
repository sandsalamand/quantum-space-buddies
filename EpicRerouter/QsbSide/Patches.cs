﻿using HarmonyLib;

namespace EpicRerouter.QsbSide
{
	[HarmonyPatch(typeof(ProcessSide.EpicPlatformManager))]
	public static class Patches
	{
		[HarmonyPrefix]
		[HarmonyPatch("instance", MethodType.Getter)]
		private static bool GetInstance()
		{
			Interop.Log("instance get called");
			return false;
		}

		[HarmonyPrefix]
		[HarmonyPatch("instance", MethodType.Setter)]
		private static bool SetInstance()
		{
			Interop.Log("instance set called");
			return false;
		}

		[HarmonyPrefix]
		[HarmonyPatch("platformInterface", MethodType.Getter)]
		private static bool GetPlatformInterface()
		{
			Interop.Log("platformInterface get called");
			return false;
		}
	}
}