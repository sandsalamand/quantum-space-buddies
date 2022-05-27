﻿using HarmonyLib;
using QSB.EchoesOfTheEye.LightSensorSync.WorldObjects;
using QSB.Patches;
using QSB.Player;
using QSB.Tools.FlashlightTool;
using QSB.Tools.ProbeTool;
using QSB.WorldSync;
using UnityEngine;

namespace QSB.EchoesOfTheEye.LightSensorSync.Patches;

[HarmonyPatch(typeof(SingleLightSensor))]
internal class LightSensorPatches : QSBPatch
{
	public override QSBPatchTypes Type => QSBPatchTypes.OnClientConnect;

	[HarmonyPrefix]
	[HarmonyPatch(nameof(SingleLightSensor.ManagedFixedUpdate))]
	private static bool ManagedFixedUpdate(SingleLightSensor __instance)
	{
		if (!QSBWorldSync.AllObjectsReady)
		{
			return true;
		}

		if (!__instance.HasWorldObject())
		{
			return true;
		}

		var qsbLightSensor = __instance.GetWorldObject<QSBLightSensor>();
		
		if (__instance._fixedUpdateFrameDelayCount > 0)
		{
			__instance._fixedUpdateFrameDelayCount--;
			return false;
		}

		var locallyIlluminated = qsbLightSensor.LocallyIlluminated;
		__instance.UpdateIllumination();
		if (!locallyIlluminated && qsbLightSensor.LocallyIlluminated)
		{
			qsbLightSensor.OnDetectLocalLight?.Invoke();
			// __instance.OnDetectLight.Invoke();
		}
		else if (locallyIlluminated && !__instance._illuminated)
		{
			qsbLightSensor.OnDetectLocalDarkness?.Invoke();
			// __instance.OnDetectDarkness.Invoke();
		}

		return false;
	}

	[HarmonyPrefix]
	[HarmonyPatch(nameof(SingleLightSensor.UpdateIllumination))]
	private static bool UpdateIllumination(SingleLightSensor __instance)
	{
		if (!QSBWorldSync.AllObjectsReady)
		{
			return true;
		}

		if (!__instance.HasWorldObject())
		{
			return true;
		}

		var qsbLightSensor = __instance.GetWorldObject<QSBLightSensor>();

		qsbLightSensor.LocallyIlluminated = false;
		__instance._illuminatingDreamLanternList?.Clear();
		if (__instance._lightSources == null || __instance._lightSources.Count == 0)
		{
			return false;
		}

		var vector = __instance.transform.TransformPoint(__instance._localSensorOffset);
		var sensorWorldDir = Vector3.zero;
		if (__instance._directionalSensor)
		{
			sensorWorldDir = __instance.transform.TransformDirection(__instance._localDirection).normalized;
		}

		for (var i = 0; i < __instance._lightSources.Count; i++)
		{
			if ((__instance._lightSourceMask & __instance._lightSources[i].GetLightSourceType()) == __instance._lightSources[i].GetLightSourceType() && __instance._lightSources[i].CheckIlluminationAtPoint(vector, __instance._sensorRadius, __instance._maxDistance))
			{
				var lightSourceType = __instance._lightSources[i].GetLightSourceType();
				switch (lightSourceType)
				{
					case LightSourceType.UNDEFINED:
						{
							var owlight = __instance._lightSources[i] as OWLight2;
							var occludableLight = owlight.GetLight().shadows != LightShadows.None && owlight.GetLight().shadowStrength > 0.5f;
							if (owlight.CheckIlluminationAtPoint(vector, __instance._sensorRadius, __instance._maxDistance)
								&& !__instance.CheckOcclusion(owlight.transform.position, vector, sensorWorldDir, occludableLight))
							{
								qsbLightSensor.LocallyIlluminated = true;
							}

							break;
						}
					case LightSourceType.FLASHLIGHT:
						{
							var position = Locator.GetPlayerCamera().transform.position;
							var to = __instance.transform.position - position;
							if (Vector3.Angle(Locator.GetPlayerCamera().transform.forward, to) <= __instance._maxSpotHalfAngle
								&& !__instance.CheckOcclusion(position, vector, sensorWorldDir))
							{
								qsbLightSensor.LocallyIlluminated = true;
							}

							break;
						}
					case LightSourceType.PROBE:
						{
							var probe = Locator.GetProbe();
							if (probe != null
								&& probe.IsLaunched()
								&& !probe.IsRetrieving()
								&& probe.CheckIlluminationAtPoint(vector, __instance._sensorRadius, __instance._maxDistance)
								&& !__instance.CheckOcclusion(probe.GetLightSourcePosition(), vector, sensorWorldDir))
							{
								qsbLightSensor.LocallyIlluminated = true;
							}

							break;
						}
					case LightSourceType.DREAM_LANTERN:
						{
							var dreamLanternController = __instance._lightSources[i] as DreamLanternController;
							if (dreamLanternController.IsLit()
								&& dreamLanternController.IsFocused(__instance._lanternFocusThreshold)
								&& dreamLanternController.CheckIlluminationAtPoint(vector, __instance._sensorRadius, __instance._maxDistance)
								&& !__instance.CheckOcclusion(dreamLanternController.GetLightPosition(), vector, sensorWorldDir))
							{
								__instance._illuminatingDreamLanternList.Add(dreamLanternController);
								qsbLightSensor.LocallyIlluminated = true;
							}

							break;
						}
					case LightSourceType.SIMPLE_LANTERN:
						foreach (var owlight in __instance._lightSources[i].GetLights())
						{
							var occludableLight = owlight.GetLight().shadows != LightShadows.None && owlight.GetLight().shadowStrength > 0.5f;
							var maxDistance = Mathf.Min(__instance._maxSimpleLanternDistance, __instance._maxDistance);
							if (owlight.CheckIlluminationAtPoint(vector, __instance._sensorRadius, maxDistance)
								&& !__instance.CheckOcclusion(owlight.transform.position, vector, sensorWorldDir, occludableLight))
							{
								qsbLightSensor.LocallyIlluminated = true;
								break;
							}
						}

						break;
					default:
						if (lightSourceType == LightSourceType.VOLUME_ONLY)
						{
							qsbLightSensor.LocallyIlluminated = true;
						}

						break;
				}
			}
		}

		return false;
	}

	private static bool UpdateIllumination_OLD(SingleLightSensor __instance)
	{
		if (!QSBWorldSync.AllObjectsReady)
		{
			return true;
		}

		if (!__instance.HasWorldObject())
		{
			return true;
		}

		var qsbLightSensor = __instance.GetWorldObject<QSBLightSensor>();
		var locallyIlluminated = qsbLightSensor.LocallyIlluminated;
		qsbLightSensor.LocallyIlluminated = false;

		__instance._illuminated = false;
		__instance._illuminatingDreamLanternList?.Clear();

		if (__instance._lightSources == null || __instance._lightSources.Count == 0)
		{
			if (locallyIlluminated)
			{
				qsbLightSensor.OnDetectLocalDarkness?.Invoke();
			}

			return false;
		}

		var vector = __instance.transform.TransformPoint(__instance._localSensorOffset);
		var sensorWorldDir = Vector3.zero;
		if (__instance._directionalSensor)
		{
			sensorWorldDir = __instance.transform.TransformDirection(__instance._localDirection).normalized;
		}

		foreach (var lightSource in __instance._lightSources)
		{
			if ((__instance._lightSourceMask & lightSource.GetLightSourceType()) == lightSource.GetLightSourceType() &&
				lightSource.CheckIlluminationAtPoint(vector, __instance._sensorRadius, __instance._maxDistance))
			{
				var lightSourceType = lightSource.GetLightSourceType();
				switch (lightSourceType)
				{
					case LightSourceType.UNDEFINED:
						{
							var owlight = lightSource as OWLight2;
							var occludableLight = owlight.GetLight().shadows != LightShadows.None && owlight.GetLight().shadowStrength > 0.5f;
							if (owlight.CheckIlluminationAtPoint(vector, __instance._sensorRadius, __instance._maxDistance)
								&& !__instance.CheckOcclusion(owlight.transform.position, vector, sensorWorldDir, occludableLight))
							{
								__instance._illuminated = true;
								qsbLightSensor.LocallyIlluminated = true;
							}

							break;
						}
					case LightSourceType.FLASHLIGHT:
						{
							if (lightSource is Flashlight)
							{
								var position = Locator.GetPlayerCamera().transform.position;
								var to = __instance.transform.position - position;
								if (Vector3.Angle(Locator.GetPlayerCamera().transform.forward, to) <= __instance._maxSpotHalfAngle
									&& !__instance.CheckOcclusion(position, vector, sensorWorldDir))
								{
									__instance._illuminated = true;
									qsbLightSensor.LocallyIlluminated = true;
								}
							}
							else if (lightSource is QSBFlashlight qsbFlashlight)
							{
								var playerCamera = qsbFlashlight.Player.Camera;

								var position = playerCamera.transform.position;
								var to = __instance.transform.position - position;
								if (Vector3.Angle(playerCamera.transform.forward, to) <= __instance._maxSpotHalfAngle
									&& !__instance.CheckOcclusion(position, vector, sensorWorldDir))
								{
									__instance._illuminated = true;
								}
							}

							break;
						}
					case LightSourceType.PROBE:
						{
							if (lightSource is SurveyorProbe probe)
							{
								if (probe != null
									&& probe.IsLaunched()
									&& !probe.IsRetrieving()
									&& probe.CheckIlluminationAtPoint(vector, __instance._sensorRadius, __instance._maxDistance)
									&& !__instance.CheckOcclusion(probe.GetLightSourcePosition(), vector, sensorWorldDir))
								{
									__instance._illuminated = true;
									qsbLightSensor.LocallyIlluminated = true;
								}
							}
							else if (lightSource is QSBProbe qsbProbe)
							{
								if (qsbProbe != null
									&& qsbProbe.IsLaunched()
									&& !qsbProbe.IsRetrieving()
									&& qsbProbe.CheckIlluminationAtPoint(vector, __instance._sensorRadius, __instance._maxDistance)
									&& !__instance.CheckOcclusion(qsbProbe.GetLightSourcePosition(), vector, sensorWorldDir))
								{
									__instance._illuminated = true;
								}
							}

							break;
						}
					case LightSourceType.FLASHLIGHT | LightSourceType.PROBE:
					case LightSourceType.FLASHLIGHT | LightSourceType.DREAM_LANTERN:
					case LightSourceType.PROBE | LightSourceType.DREAM_LANTERN:
					case LightSourceType.FLASHLIGHT | LightSourceType.PROBE | LightSourceType.DREAM_LANTERN:
						break;
					case LightSourceType.DREAM_LANTERN:
						{
							var dreamLanternController = lightSource as DreamLanternController;
							if (dreamLanternController.IsLit()
								&& dreamLanternController.IsFocused(__instance._lanternFocusThreshold)
								&& dreamLanternController.CheckIlluminationAtPoint(vector, __instance._sensorRadius, __instance._maxDistance)
								&& !__instance.CheckOcclusion(dreamLanternController.GetLightPosition(), vector, sensorWorldDir))
							{
								__instance._illuminatingDreamLanternList.Add(dreamLanternController);
								__instance._illuminated = true;
								var dreamLanternItem = dreamLanternController.GetComponent<DreamLanternItem>();
								qsbLightSensor.LocallyIlluminated = QSBPlayerManager.LocalPlayer.HeldItem?.AttachedObject == dreamLanternItem;
							}

							break;
						}
					case LightSourceType.SIMPLE_LANTERN:
						foreach (var owlight in lightSource.GetLights())
						{
							var occludableLight = owlight.GetLight().shadows != LightShadows.None && owlight.GetLight().shadowStrength > 0.5f;
							var maxDistance = Mathf.Min(__instance._maxSimpleLanternDistance, __instance._maxDistance);
							if (owlight.CheckIlluminationAtPoint(vector, __instance._sensorRadius, maxDistance)
								&& !__instance.CheckOcclusion(owlight.transform.position, vector, sensorWorldDir, occludableLight))
							{
								__instance._illuminated = true;
								var simpleLanternItem = (SimpleLanternItem)lightSource;
								qsbLightSensor.LocallyIlluminated = QSBPlayerManager.LocalPlayer.HeldItem?.AttachedObject == simpleLanternItem;
								break;
							}
						}

						break;
					default:
						if (lightSourceType == LightSourceType.VOLUME_ONLY)
						{
							__instance._illuminated = true;
							qsbLightSensor.LocallyIlluminated = true;
						}

						break;
				}
			}
		}

		if (qsbLightSensor.LocallyIlluminated && !locallyIlluminated)
		{
			qsbLightSensor.OnDetectLocalLight?.Invoke();
		}
		else if (!qsbLightSensor.LocallyIlluminated && locallyIlluminated)
		{
			qsbLightSensor.OnDetectLocalDarkness?.Invoke();
		}

		return false;
	}
}
