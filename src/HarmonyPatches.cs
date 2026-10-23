using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

using FlourModLoader;

// dont load default map in loader
[HarmonyPatch(typeof(Loader), "Start")]
public static class LoaderStartPatch {

	[HarmonyPrefix]
	public static bool Prefix(Loader __instance) {
		var t = Traverse.Create(__instance);

		t.Field("safeToClick").SetValue(false);
		t.Field("hideLogo").SetValue(PlayerPrefs.GetInt("HideLogo") == 1);
		if (!PlayerPrefs.HasKey("Trackpad") || Application.isEditor) {
			t.Field("shouldShowMouseQuery").SetValue(true);
		} else {
			t.Field("shouldShowMouseQuery").SetValue(false);
		}
		t.Field("progress").SetValue(1f); // original: 0f
		t.Field("lerpProgress").SetValue(1f); // original: 0f
		t.Field("loadFinished").SetValue(true); // original: false
		t.Field("menuItemClicked").SetValue(-1);

		t.Field("canContinue").SetValue(false);
		if (PlayerPrefs.GetString("SaveGame0").Length > 0 || PlayerPrefs.GetString("SaveGame1").Length > 0) {
			t.Field("canContinue").SetValue(true);
		}
		TextMeshProUGUI[] componentsInChildren = __instance.menu.GetComponentsInChildren<TextMeshProUGUI>();
		for (int i = 0; i < componentsInChildren.Length; i++) {
			Color color = componentsInChildren[i].color;
			color.a = 0f;
			componentsInChildren[i].color = color;
		}
		__instance.continueBar.color = new Color(0f, 0f, 0f, 0f);
		__instance.newgameBar.color = new Color(0f, 0f, 0f, 0f);
		__instance.scrapeParticles.gameObject.SetActive(value: true);
		__instance.fog.SetActive(value: true);
		t.Field("introAnimDone").SetValue(false);
		__instance.humble.gameObject.SetActive(value: true);
		__instance.titleMask.sizeDelta = new Vector2(0f, 216f);
		if (t.Field("hideLogo").GetValue<bool>()) {
			__instance.fader.color = new Color(0f, 0f, 0f, 1f);
			__instance.humble.gameObject.SetActive(value: false);
			if (t.Field("shouldShowMouseQuery").GetValue<bool>()) {
				__instance.mouseQuery.SetActive(value: true);
				__instance.languageBar.SetActive(value: true);
				__instance.languageBar.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 826.7f);
			}
			__instance.StartCoroutine("FadeInTitle");
		} else {
			__instance.StartCoroutine("FadeInHumble");
			PlayerPrefs.SetInt("HideLogo", 1);
			PlayerPrefs.Save();
		}
		Cursor.lockState = CursorLockMode.None;
		Cursor.visible = true;

		return false;
	}
}
// load sync instead of async for new game button
[HarmonyPatch(typeof(Loader), "DoStart")]
public static class LoaderDoStartPatch {
	[HarmonyPrefix]
	public static bool Prefix(Loader __instance) {
		__instance.variants.WarmUp();
		SceneManager.LoadScene("Mian");

		return false;
	}
};
// ctrl r in maps
[HarmonyPatch(typeof(PlayerControl), "Update")]
public static class PlayerControlUpdatePatch {
	private static readonly AccessTools.FieldRef<PlayerControl, int> numWinsRef = AccessTools.FieldRefAccess<PlayerControl, int>("numWins");

	[HarmonyPrefix]
	public static bool Prefix(PlayerControl __instance) {
		if (numWinsRef(__instance) > 0 && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKey(KeyCode.R)) {
			PlayerPrefs.DeleteKey("NumSaves");
			PlayerPrefs.DeleteKey("SaveGame0");
			PlayerPrefs.DeleteKey("SaveGame1");
			PlayerPrefs.Save();
			if (LevelLoader.LevelLoaderPlugin.Instance.isCustomMap) {
				LevelLoader.LevelLoaderPlugin.Instance.ReloadScene();
			} else {
				SceneManager.LoadScene("Mian");
			}
		}
		__instance.fakeCursor.position = new Vector3(__instance.fakeCursor.position.x, __instance.fakeCursor.position.y, __instance.tip.position.z - 1f);

		return false;
	}
};
// auto discover colorsets
[HarmonyPatch(typeof(FogControl), "Start")]
public static class FogControlStartPatch {
	private static readonly AccessTools.FieldRef<FogControl, PostProcessVolume> postRef = AccessTools.FieldRefAccess<FogControl, PostProcessVolume>("post");
	private static readonly AccessTools.FieldRef<FogControl, ColorGrading> settingsRef = AccessTools.FieldRefAccess<FogControl, ColorGrading>("settings");

	[HarmonyPrefix]
	public static bool Prefix(FogControl __instance) {
		var t = Traverse.Create(__instance);

		t.Field("topColor").SetValue(Color.white);
		t.Field("bottomColor").SetValue(Color.white);
		t.Field("midColor").SetValue(Color.white);
		t.Field("contrast").SetValue(1f);
		t.Field("exposure").SetValue(0f);
		t.Field("saturation").SetValue(1f);
		postRef(__instance) = __instance.GetComponent<PostProcessVolume>();
		settingsRef(__instance) = postRef(__instance).profile.GetSetting<ColorGrading>();

		// auto find colorsets
		List<ColorSet> newColorSets = UnityEngine.Object.FindObjectsOfType<ColorSet>().ToList();
		foreach (ColorSet colorSet in __instance.colorSets) { // add fogcontrol colorsets to list
			if (!newColorSets.Contains(colorSet)) {
				newColorSets.Add(colorSet);
			}
		}
		__instance.colorSets = newColorSets.ToArray();

		return false;
	}
}

// check if in game for pause menu
[HarmonyPatch(typeof(SettingsManager), "Update")]
public static class SettingsManagerPausePatch {
	// access private fields
	private static readonly AccessTools.FieldRef<SettingsManager, RestartOnContact> underwaterDetectorRef = AccessTools.FieldRefAccess<SettingsManager, RestartOnContact>("underwaterDetector");
	private static readonly AccessTools.FieldRef<SettingsManager, GameObject> narratorRef				 = AccessTools.FieldRefAccess<SettingsManager, GameObject>("narrator");

	[HarmonyPrefix]
	public static bool Prefix(SettingsManager __instance) {
		// if escape and timer is above 1.5 and not in water and in game then toggle menu
		if (Input.GetKeyDown(KeyCode.Escape) &&
			GOIConstants.timer > 1.5f &&
			(underwaterDetectorRef(__instance) == null || (underwaterDetectorRef(__instance) != null && !underwaterDetectorRef(__instance).resetting)) &&
			GOIConstants.gameState == GameState.InGame) {
			__instance.Invoke("ToggleMenu", 0f);
		}

		return false;
	}
};
// check if water isnt null when starting camera
[HarmonyPatch(typeof(CameraControl), "Start")]
public static class CameraControlStartPatch {
	// access private fields
	private static readonly AccessTools.FieldRef<CameraControl, Camera> mainCamRef	   = AccessTools.FieldRefAccess<CameraControl, Camera>("mainCam");
	private static readonly AccessTools.FieldRef<CameraControl, Vector3> velRef		  = AccessTools.FieldRefAccess<CameraControl, Vector3>("vel");
	private static readonly AccessTools.FieldRef<CameraControl, Vector3> lookaheadPosRef = AccessTools.FieldRefAccess<CameraControl, Vector3>("lookaheadPos");
	private static readonly AccessTools.FieldRef<CameraControl, GameObject> waterRef	 = AccessTools.FieldRefAccess<CameraControl, GameObject>("water");
	private static readonly AccessTools.FieldRef<CameraControl, GameObject> playerRef	= AccessTools.FieldRefAccess<CameraControl, GameObject>("player");
	private static readonly AccessTools.FieldRef<CameraControl, Vector3> targetRef	   = AccessTools.FieldRefAccess<CameraControl, Vector3>("target");
	private static readonly AccessTools.FieldRef<CameraControl, Vector3> oldTargetRef	= AccessTools.FieldRefAccess<CameraControl, Vector3>("oldTarget");

	[HarmonyPrefix]
	public static bool Prefix(CameraControl __instance) {
		// traversal for primitives
		var t = Traverse.Create(__instance);

		// modify private fields
		mainCamRef(__instance) = Camera.main;
		velRef(__instance) = Vector3.zero;
		lookaheadPosRef(__instance) = Vector3.zero;

		GameObject water = waterRef(__instance);
		if (water != null) {
			float waterLevel = water.GetComponent<MeshRenderer>().bounds.max.y;
			t.Field("waterLevel").SetValue(waterLevel);
		}

		t.Field("fadeInTimerRef").SetValue(0);
		t.Field("fadeOutTimerRef").SetValue(0);

		GameObject player = playerRef(__instance);
		Vector3 target = player.transform.position + new Vector3(0f, 0f, -20f);
		targetRef(__instance) = target;
		oldTargetRef(__instance) = target;
		t.Field("alpha").SetValue(1f);

		__instance.Invoke("FadeIn", 0f);

		return false;
	}
}
// check if spline is null when teleporting camera
[HarmonyPatch(typeof(CameraControl), "Teleport")]
public static class CameraControlTeleportPatch {

	[HarmonyPrefix]
	public static bool Prefix(CameraControl __instance, Vector3 newPosition) {
		// traversal for primitives
		var t = Traverse.Create(__instance);

		__instance.gameObject.transform.position = newPosition + new Vector3(0f, 0f, -20f);
		if (__instance.spline != null) {
			float lastTF = __instance.spline.GetNearestPointTF(newPosition);
			t.Field("lastTF").SetValue(lastTF);
			Vector3 vector = __instance.spline.Interpolate(lastTF + 0.01f);
			Vector3 vector2 = __instance.spline.Interpolate(lastTF + 0.02f);
			Vector3 vector3 = __instance.spline.Interpolate(lastTF + 0.03f);
			t.Field("lookAheadPos").SetValue(0.3333f * (vector + vector2 + vector3));
		}
		t.Field("vel").SetValue(Vector3.zero);

		return false;
	}
}
// check if saviour is null when playing sad sound
[HarmonyPatch(typeof(PlayerSounds), "PlaySadSound")]
public static class PlaySadSoundSaviourPatch {
	// access private fields
	private static readonly AccessTools.FieldRef<PlayerSounds, float> lastSoundTimerRef	 = AccessTools.FieldRefAccess<PlayerSounds, float>("lastSoundTimer");
	private static readonly AccessTools.FieldRef<PlayerSounds, int>  lastSoundIndexRef	  = AccessTools.FieldRefAccess<PlayerSounds, int>("lastSoundIndex");
	private static readonly AccessTools.FieldRef<PlayerSounds, AudioSource>  audioSourceRef = AccessTools.FieldRefAccess<PlayerSounds, AudioSource>("audioSource");
	private static readonly AccessTools.FieldRef<PlayerSounds, AudioClip[]>  sadSoundsRef   = AccessTools.FieldRefAccess<PlayerSounds, AudioClip[]>("sadSounds");
	private static readonly AccessTools.FieldRef<PlayerSounds, float>  mouthTimerRef		= AccessTools.FieldRefAccess<PlayerSounds, float>("mouthTimer");
	private static readonly AccessTools.FieldRef<PlayerSounds, Saviour>  saviourRef		 = AccessTools.FieldRefAccess<PlayerSounds, Saviour>("saviour");

	private static MethodInfo saviourSaveMethod = AccessTools.Method(typeof(Saviour), "SaveGameNow");

	[HarmonyPrefix]
	public static bool Prefix(PlayerSounds __instance, float volume = 1f) {

		if (!(lastSoundTimerRef(__instance) > 0f)) {
			AudioClip[] sadSounds = sadSoundsRef(__instance);
			if (sadSounds == null || sadSounds.Length == 0)
				return false;

			int index = UnityEngine.Random.Range(0, sadSounds.Length);
			lastSoundIndexRef(__instance) = index;

			AudioSource source = audioSourceRef(__instance);
			if (source != null)
				source.PlayOneShot(sadSounds[index], volume);

			lastSoundTimerRef(__instance) = sadSounds[index].length * 2.5f;
			mouthTimerRef(__instance) = sadSounds[index].length;

			Saviour saviour = saviourRef(__instance);
			if (saviour != null) { // saviour is null in maps
				saviourSaveMethod.Invoke(saviour, new object[] { false });
			}
		}

		return false;
	}
};


[HarmonyPatch]
public static class HammerCollisionsPatch {
	private static readonly AccessTools.FieldRef<HammerCollisions, AudioSource> audioSourceRef = AccessTools.FieldRefAccess<HammerCollisions, AudioSource>("audioSource");

	private static Type customHitSoundProviderType = AccessTools.TypeByName("CustomHitSoundProvider");
	private static Type customGroundColType = AccessTools.TypeByName("CustomGroundCol");

	private static MethodInfo playHitMethod = typeof(HammerCollisions).GetMethod("PlayHit", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
	private static MethodInfo playHardHitMethod = typeof(HammerCollisions).GetMethod("PlayHardHit", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
	private static MethodInfo playScrapeMethod = typeof(HammerCollisions).GetMethod("PlayScrape", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

	private static MethodInfo getHitMethod = customHitSoundProviderType.GetMethod("GetHit", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
	private static MethodInfo getHardHitMethod = customHitSoundProviderType.GetMethod("GetHardHit", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
	private static MethodInfo getScrapeMethod = customHitSoundProviderType.GetMethod("GetScrape", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);


	private static FieldInfo isSolidField = customGroundColType.GetField("isSolid", BindingFlags.Public | BindingFlags.Instance);
	private static FieldInfo groundColField = customGroundColType.GetField("groundCol", BindingFlags.Public | BindingFlags.Instance);
	private static FieldInfo materialField = customGroundColType.GetField("material", BindingFlags.Public | BindingFlags.Instance);

	public static class HammerCollisionsCustomFunctions {
		public static void PlayCustomHardHit(HammerCollisions __instance, string customMat, float volume = 1f) {
			var t = Traverse.Create(__instance);
			Type customHitSoundProviderType = AccessTools.TypeByName("CustomHitSoundProvider");

			if (t.Field("lastSoundTimer").GetValue<float>() < 0.1f) {
				return;
			}
			if (volume > 3f) {
				volume = 3f;
			}
			t.Field("lastSoundTimer").SetValue(0f);
			AudioClip audioClip = null;

			UnityEngine.Object customHitSoundProvider = UnityEngine.Object.FindObjectOfType(customHitSoundProviderType);

			if (customHitSoundProvider != null) {

				audioClip = (AudioClip)getHardHitMethod.Invoke(customHitSoundProvider, new object[] { customMat });
			}
			if (audioClip != null) {
				audioSourceRef(__instance).PlayOneShot(audioClip, volume);
			} else {
				playHardHitMethod.Invoke(__instance, new object[] { volume, GroundCol.SoundMaterial.rock });
			}
		}

		public static void PlayCustomHit(HammerCollisions __instance, string customMat, float volume = 1f) {
			var t = Traverse.Create(__instance);

			if (t.Field("lastSoundTimer").GetValue<float>() < 0.1f) {
				return;
			}
			if (volume > 3f) {
				volume = 3f;
			}
			t.Field("lastSoundTimer").SetValue(0f);
			AudioClip audioClip = null;

			UnityEngine.Object customHitSoundProvider = UnityEngine.Object.FindObjectOfType(customHitSoundProviderType);

			if (customHitSoundProvider != null) {
				audioClip = (AudioClip)getHitMethod.Invoke(customHitSoundProvider, new object[] { customMat });
			}
			if (audioClip != null) {
				audioSourceRef(__instance).PlayOneShot(audioClip, volume);
			} else {
				playHitMethod.Invoke(__instance, new object[] { volume, GroundCol.SoundMaterial.rock });
			}
		}
		public static void PlayCustomScrape(HammerCollisions __instance, string customMat, float volume = 1f) {
			var t = Traverse.Create(__instance);

			if (t.Field("lastSoundTimer").GetValue<float>() < 0.1f){
				return;
			}
			if (volume > 3f) {
				volume = 3f;
			}
			t.Field("lastSoundTimer").SetValue(0f);
			AudioClip audioClip = null;

			UnityEngine.Object customHitSoundProvider = UnityEngine.Object.FindObjectOfType(customHitSoundProviderType);

			if (customHitSoundProvider != null) {
				audioClip = (AudioClip)getScrapeMethod.Invoke(customHitSoundProvider, new object[] { customMat });
			}
			if (audioClip != null) {
				audioSourceRef(__instance).PlayOneShot(audioClip, volume);
			} else {
				playScrapeMethod.Invoke(__instance, new object[] { volume, GroundCol.SoundMaterial.rock });
			}
		}
	}
	[HarmonyPatch(typeof(HammerCollisions), "OnCollisionEnter2D")]
	public static class EnterPatch {
		private static readonly AccessTools.FieldRef<HammerCollisions, ParticleSystem.EmitParams> sparkParamsRef = AccessTools.FieldRefAccess<HammerCollisions, ParticleSystem.EmitParams>("sparkParams");
		private static readonly AccessTools.FieldRef<HammerCollisions, ParticleSystem.EmitParams> debrisParamsRef = AccessTools.FieldRefAccess<HammerCollisions, ParticleSystem.EmitParams>("debrisParams");
		private static readonly AccessTools.FieldRef<HammerCollisions, ParticleSystem.EmitParams> dustParamsRef = AccessTools.FieldRefAccess<HammerCollisions, ParticleSystem.EmitParams>("dustParams");
		private static readonly AccessTools.FieldRef<HammerCollisions, ContactPoint2D[]> contactsRef = AccessTools.FieldRefAccess<HammerCollisions, ContactPoint2D[]>("contacts");


		[HarmonyPrefix]
		public static bool Prefix(HammerCollisions __instance, Collision2D coll) {
			var t = Traverse.Create(__instance);

			if (__instance.collisionPoints == null) {
				return false;
			}
			int contactCount = coll.GetContacts(contactsRef(__instance));
			if (contactCount <= 0 || (coll.gameObject.layer != 10 && coll.gameObject.layer != 29)) {
				return false;
			}
			if (coll.relativeVelocity.magnitude > 14f) {
				__instance.player.SendMessage("HammerReturn");
			}
			if (__instance.collisionPoints.ContainsKey(coll.collider)) {
				return false;
			}
			__instance.collisionPoints[coll.collider] = contactsRef(__instance);
			GroundCol groundCol = coll.collider.GetComponent<GroundCol>();

			Component customGroundCol = coll.collider.GetComponent(customGroundColType);
			bool customGroundColIsSolid = false;;
			Color customGroundColGroundCol = new Color();
			string customGroundColMaterial = "";
			if (customGroundCol != null) {
				// is solid
				customGroundColIsSolid = (bool)isSolidField.GetValue(customGroundCol);
				// hit color
				customGroundColGroundCol = (Color)groundColField.GetValue(customGroundCol);
				// hit sound
				customGroundColMaterial = (string)materialField.GetValue(customGroundCol);
			}
			Color impactColor = new Color();
			bool hasImpactColor = true;
			if (customGroundCol != null) {
				impactColor = customGroundColGroundCol;
			} else if (groundCol != null){
				impactColor = groundCol.groundCol;
			} else {
				hasImpactColor = false;
			}

			__instance.impactBlur.Impact(new Vector2(contactsRef(__instance)[0].point.x, contactsRef(__instance)[0].point.y), coll.relativeVelocity.magnitude / 12f);

			bool isSolid = false;
			if ((groundCol != null && (groundCol.material == GroundCol.SoundMaterial.rock || groundCol.material == GroundCol.SoundMaterial.metal || groundCol.material == GroundCol.SoundMaterial.solidmetal)) || (customGroundCol != null && customGroundColIsSolid)) {
				isSolid = true;
			}
			if (coll.relativeVelocity.magnitude < t.Field("hardHitThreshold").GetValue<float>()) { // soft hit
				for (int i = 0; i < contactCount; i++) {
					debrisParamsRef(__instance).position = contactsRef(__instance)[i].point;
					if (hasImpactColor) {
						debrisParamsRef(__instance).startColor = impactColor;
					}
					float normalImpulse = contactsRef(__instance)[i].normalImpulse;
					float tangentImpulse = contactsRef(__instance)[i].tangentImpulse;
					Vector3 contactNormal = contactsRef(__instance)[i].normal;
					Vector3 contactNormal2D = new Vector3(contactNormal.y, 0f - contactNormal.x, 0f);
					for (int j = 0; j < (int)Mathf.Min((normalImpulse + tangentImpulse) * 0.1f, 20f); j++) {
						debrisParamsRef(__instance).velocity = contactNormal * normalImpulse * 0.2f + contactNormal2D * ((j % 2 == 0) ? 1f : (-1f)) * tangentImpulse * 0.1f + UnityEngine.Random.insideUnitSphere * 3.4f;
						__instance.debris.Emit(debrisParamsRef(__instance), 1);
					}
				} if (customGroundCol != null) {
					HammerCollisionsCustomFunctions.PlayCustomHit(__instance, customGroundColMaterial, coll.relativeVelocity.magnitude / t.Field("hardHitThreshold").GetValue<float>());
				} else if (groundCol != null) {
					playHitMethod.Invoke(__instance, new object[] { coll.relativeVelocity.magnitude / t.Field("hardHitThreshold").GetValue<float>(), groundCol.material });
				} else {
					playHitMethod.Invoke(__instance, new object[] { coll.relativeVelocity.magnitude / t.Field("hardHitThreshold").GetValue<float>(), GroundCol.SoundMaterial.rock});
				}
				return false;
			}
			if (isSolid) {
				for (int k = 0; k < contactCount; k++) {
					sparkParamsRef(__instance).position = contactsRef(__instance)[k].point;
					float normalImpulse2 = contactsRef(__instance)[k].normalImpulse;
					float tangentImpulse2 = contactsRef(__instance)[k].tangentImpulse;
					float value = normalImpulse2 + tangentImpulse2;
					Vector3 contactNormal = contactsRef(__instance)[k].normal;
					Vector3 contactNormal2D = new Vector3(contactNormal.y, 0f - contactNormal.x, 0f);
					for (int l = 0; l < (int)Mathf.Min((normalImpulse2 + tangentImpulse2) * 0.1f, 20f); l++) {
						sparkParamsRef(__instance).velocity = contactNormal * normalImpulse2 * 0.3f + contactNormal2D * ((l % 2 == 0) ? 1f : (-1f)) * tangentImpulse2 * 0.3f + UnityEngine.Random.insideUnitSphere * Mathf.Clamp(value, 1f, 60f) * 0.1f;
						__instance.sparks.Emit(sparkParamsRef(__instance), 1);
					}
				}
			} else {
				for (int m = 0; m < contactCount; m++) {
					debrisParamsRef(__instance).position = contactsRef(__instance)[m].point;
					if (hasImpactColor) {
						debrisParamsRef(__instance).startColor = impactColor;
					}
					float normalImpulse3 = contactsRef(__instance)[m].normalImpulse;
					float tangentImpulse3 = contactsRef(__instance)[m].tangentImpulse;
					float value2 = normalImpulse3 + tangentImpulse3;
					Vector3 contactNormal = contactsRef(__instance)[m].normal;
					Vector3 contactNormal2D = new Vector3(contactNormal.y, 0f - contactNormal.x, 0f);
					for (int n = 0; n < (int)Mathf.Min((normalImpulse3 + tangentImpulse3) * 0.1f, 20f); n++) {
						debrisParamsRef(__instance).velocity = contactNormal * normalImpulse3 * 0.1f + contactNormal2D * ((n % 2 == 0) ? 1f : (-1f)) * tangentImpulse3 * 0.1f + UnityEngine.Random.insideUnitSphere * Mathf.Clamp(value2, 1f, 60f) * 0.1f;
						__instance.debris.Emit(debrisParamsRef(__instance), 1);
					}
				}
			} if (customGroundCol != null) {
				HammerCollisionsCustomFunctions.PlayCustomHardHit(__instance, customGroundColMaterial, coll.relativeVelocity.magnitude / 4f);
			} else if (groundCol != null) {
				playHardHitMethod.Invoke(__instance, new object[] { coll.relativeVelocity.magnitude / 4f, groundCol.material });
			} else {
				playHardHitMethod.Invoke(__instance, new object[] { coll.relativeVelocity.magnitude / 4f, GroundCol.SoundMaterial.rock });
			}

			return false;
		}
	}

	[HarmonyPatch(typeof(HammerCollisions), "OnCollisionStay2D")]
	public class StayPatch {
		private static Type customGroundColType = AccessTools.TypeByName("CustomGroundCol");

		private static readonly AccessTools.FieldRef<HammerCollisions, AudioSource> audioSourceRef = AccessTools.FieldRefAccess<HammerCollisions, AudioSource>("audioSource");
		private static readonly AccessTools.FieldRef<HammerCollisions, ParticleSystem.EmitParams> sparkParamsRef = AccessTools.FieldRefAccess<HammerCollisions, ParticleSystem.EmitParams>("sparkParams");
		private static readonly AccessTools.FieldRef<HammerCollisions, ParticleSystem.EmitParams> debrisParamsRef = AccessTools.FieldRefAccess<HammerCollisions, ParticleSystem.EmitParams>("debrisParams");
		private static readonly AccessTools.FieldRef<HammerCollisions, ParticleSystem.EmitParams> dustParamsRef = AccessTools.FieldRefAccess<HammerCollisions, ParticleSystem.EmitParams>("dustParams");
		private static readonly AccessTools.FieldRef<HammerCollisions, ContactPoint2D[]> contactsRef = AccessTools.FieldRefAccess<HammerCollisions, ContactPoint2D[]>("contacts");
		private static readonly AccessTools.FieldRef<HammerCollisions, Rigidbody2D> tipRef = AccessTools.FieldRefAccess<HammerCollisions, Rigidbody2D>("tip");

		[HarmonyPrefix]
		public static bool Prefix(HammerCollisions __instance, Collision2D coll) {
			var t = Traverse.Create(__instance);

			if (__instance.collisionPoints == null || coll.gameObject.layer != LayerMask.NameToLayer("Terrain")) {
				return false;
			}
			int contactCount = coll.GetContacts(contactsRef(__instance));
			if (!__instance.collisionPoints.ContainsKey(coll.collider)) {
				return false;
			}
			bool isScrape = true;
			float minMagnitude = 0.3f;
			float maxTangentialness = 0f;
			for (int i = 0; i < contactCount; i++) {
				for (int j = 0; j < __instance.collisionPoints[coll.collider].Length; j++) {
					float magnitude = (__instance.collisionPoints[coll.collider][j].point - contactsRef(__instance)[i].point).magnitude;
					minMagnitude = Mathf.Min(minMagnitude, magnitude);
					maxTangentialness = Mathf.Max((1f + Mathf.Abs(contactsRef(__instance)[i].tangentImpulse)) / (1f + Mathf.Abs(contactsRef(__instance)[i].normalImpulse)), maxTangentialness);
					if (magnitude < t.Field("moveThreshold").GetValue<float>()) {
						isScrape = false;
						break;
					}
				}
			}
			if (tipRef(__instance).velocity.magnitude > 0.3f && maxTangentialness > 5f) { // if hammer is fast and
				isScrape = true;
			}
			if (isScrape && coll.rigidbody == null) {
				__instance.collisionPoints[coll.collider] = contactsRef(__instance);

				GroundCol groundCol = coll.collider.GetComponent<GroundCol>();
				// get custom ground col
				Component customGroundCol = coll.collider.GetComponent(customGroundColType);
				bool customGroundColIsSolid = false;
				Color customGroundColGroundCol = new Color();
				string customGroundColMaterial = "";
				if (customGroundCol != null) {
					// is solid
					customGroundColIsSolid = (bool)isSolidField.GetValue(customGroundCol);
					// hit color
					customGroundColGroundCol = (Color)groundColField.GetValue(customGroundCol);
					// hit sound
					customGroundColMaterial = (string)materialField.GetValue(customGroundCol);
				}

				if (customGroundCol != null) {
					dustParamsRef(__instance).startColor = customGroundColGroundCol;
				} else if (groundCol != null) {
					dustParamsRef(__instance).startColor = groundCol.groundCol;
				}

				for (int k = 0; k < contactsRef(__instance).Length; k++) {
					dustParamsRef(__instance).position = contactsRef(__instance)[k].point;
					Vector3 vector = 0.4f * new Vector3(contactsRef(__instance)[k].normal.y, 0f - contactsRef(__instance)[k].normal.x) * Vector2.Dot(new Vector2(contactsRef(__instance)[k].normal.y, 0f - contactsRef(__instance)[k].normal.x), coll.relativeVelocity);
					dustParamsRef(__instance).velocity = UnityEngine.Random.insideUnitSphere * 2f + vector;
					__instance.dust.Emit(dustParamsRef(__instance), (int)Mathf.Max(minMagnitude * 50f - 1f, 0f));
					if (!audioSourceRef(__instance).isPlaying) {
						if (customGroundCol != null) {
							HammerCollisionsCustomFunctions.PlayCustomScrape(__instance, customGroundColMaterial, 1f);
						} else if (groundCol != null) {
							playScrapeMethod.Invoke(__instance, new object[] { 1f, groundCol.material});
						} else {
							playScrapeMethod.Invoke(__instance, new object[] { 1f, GroundCol.SoundMaterial.rock });
						}
					}
				}
			}
			t.Field("slide").SetValue(isScrape);
			return false;
		}
	}
}
