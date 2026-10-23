using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

using FlourModLoader;

[HarmonyPatch]
public static class CustomHitSoundPatch {
	public static readonly Type customHitSoundType = AccessTools.TypeByName("CustomHitSound");

	public static FieldInfo customHitSoundHardHitsField = customHitSoundType.GetField("hardHits", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
	public static FieldInfo customHitSoundHitsField = customHitSoundType.GetField("hits", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
	public static FieldInfo customHitSoundScrapesField = customHitSoundType.GetField("scrapes", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
	public static MethodInfo customHitSoundGetHitMethod = customHitSoundType.GetMethod("GetHit", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

	[HarmonyPatch]
	public static class GetHardHitPatch {
		public static MethodBase TargetMethod() => AccessTools.Method(customHitSoundType, "GetHardHit");

		[HarmonyPrefix]
		public static bool Prefix(object __instance, ref AudioClip __result) {
			var t = __instance.GetType();

			List<AudioClip> hardHits = customHitSoundHardHitsField?.GetValue(__instance) as List<AudioClip>;

			if (hardHits.Count > 0) { // return hard hit
				__result = hardHits[UnityEngine.Random.Range(0, hardHits.Count)];
			} else { // if theres no hard hits return hit
				__result = (AudioClip)customHitSoundGetHitMethod.Invoke(__instance, null);
			}
			return false;
		}
	};
	[HarmonyPatch]
	public static class GetHitPatch {
		public static MethodBase TargetMethod() => AccessTools.Method(customHitSoundType, "GetHit");

		[HarmonyPrefix]
		public static bool Prefix(object __instance, ref AudioClip __result) {
			var t = __instance.GetType();

			List<AudioClip> hits = customHitSoundHitsField?.GetValue(__instance) as List<AudioClip>;

			if (hits.Count > 0) { // return hit
				__result = hits[UnityEngine.Random.Range(0, hits.Count)];
			} else { // if theres no hard hits return null
				__result = null;
			}
			return false;
		}
	}
	[HarmonyPatch]
	public static class GetScrapePatch {
		public static MethodBase TargetMethod() => AccessTools.Method(customHitSoundType, "GetScrape");

		[HarmonyPrefix]
		public static bool Prefix(object __instance, ref AudioClip __result) {
			var t = __instance.GetType();

			List<AudioClip> scrapes = customHitSoundScrapesField?.GetValue(__instance) as List<AudioClip>;

			if (scrapes.Count > 0) { // return scrape
				__result = scrapes[UnityEngine.Random.Range(0, scrapes.Count)];
			} else { // if theres no scrapes return null
				__result = null;
			}
			return false;
		}
	};
}

[HarmonyPatch]
public static class CustomHitSoundProviderPatch {
	private static readonly Type customHitSoundProviderType = AccessTools.TypeByName("CustomHitSoundProvider");
	private static readonly Type customHitSoundType = AccessTools.TypeByName("CustomHitSound");

	private static FieldInfo hitSoundsField = customHitSoundProviderType.GetField("hitSounds", BindingFlags.Instance | BindingFlags.Public);
	private static FieldInfo hitDictField = customHitSoundProviderType.GetField("hitDict", BindingFlags.Instance | BindingFlags.Public);

	private static FieldInfo customHitSoundNameField = customHitSoundType.GetField("name");
	private static MethodInfo customHitSoundGetHardHitMethod = customHitSoundType.GetMethod("GetHardHit");
	private static MethodInfo customHitSoundGetHitMethod = customHitSoundType.GetMethod("GetHit");
	private static MethodInfo customHitSoundGetScrapeMethod = customHitSoundType.GetMethod("GetScrape");

	[HarmonyPatch]
	public static class AwakePatch {
		static MethodBase TargetMethod() => AccessTools.Method(customHitSoundProviderType, "Awake");

		[HarmonyPrefix]
		static bool Prefix(object __instance) {
			var hitSounds = hitSoundsField?.GetValue(__instance) as Array;
			var hitDict = hitDictField?.GetValue(__instance) as System.Collections.IDictionary;

			// initialize hitdict
			if (hitDict == null) {
				var dictionaryType = typeof(Dictionary<,>).MakeGenericType(typeof(string), customHitSoundType);
				hitDict = Activator.CreateInstance(dictionaryType) as System.Collections.IDictionary;
			}

			hitDict.Clear();

			foreach (var customHitSound in hitSounds) {
				if (customHitSound == null) continue;

				string name = customHitSoundNameField.GetValue(customHitSound) as string;
				hitDict[name] = customHitSound;
			}

			hitDictField?.SetValue(__instance, hitDict);

			return false;
		}
	}
	[HarmonyPatch]
	public static class GetHitPatch {
		static MethodBase TargetMethod() => AccessTools.Method(customHitSoundProviderType, "GetHit", new[] { typeof(string) });

		[HarmonyPrefix]
		public static bool Prefix(object __instance, string mat, ref AudioClip __result) {
			var hitDict = hitDictField?.GetValue(__instance) as System.Collections.IDictionary;

			if (hitDict != null && hitDict.Contains(mat)) {
				var customHitSound = hitDict[mat];
				__result = (AudioClip)customHitSoundGetHitMethod.Invoke(customHitSound, null);
			} else {
				__result = null;
			}

			return false;
		}
	}
	[HarmonyPatch]
	public static class GetHardHitPatch {
		static MethodBase TargetMethod() => AccessTools.Method(customHitSoundProviderType, "GetHardHit", new[] { typeof(string) });


		static bool Prefix(object __instance, string mat, ref AudioClip __result) {
			var hitDict = hitDictField?.GetValue(__instance) as System.Collections.IDictionary;

			if (hitDict != null && hitDict.Contains(mat)) {
				var customHitSound = hitDict[mat];
				__result = (AudioClip)customHitSoundGetHardHitMethod.Invoke(customHitSound, null);
			} else {
				__result = null;
			}

			return false;
		}
	}
	[HarmonyPatch]
	public static class GetScrapePatch {
		static MethodBase TargetMethod() => AccessTools.Method(customHitSoundProviderType, "GetScrape", new[] { typeof(string) });

		static bool Prefix(object __instance, string mat, ref AudioClip __result) {
			var hitDict = hitDictField?.GetValue(__instance) as System.Collections.IDictionary;

			if (hitDict != null && hitDict.Contains(mat)) {
				var customHitSound = hitDict[mat];
				__result = (AudioClip)customHitSoundGetScrapeMethod.Invoke(customHitSound, null);
			} else {
				__result = null;
			}

			return false;
		}
	}
}

[HarmonyPatch]
public static class CustomTriggerPatch {
	private static readonly Type T_CustomTrigger = AccessTools.TypeByName("CustomTrigger");

	private static readonly Type T_Trigger = AccessTools.Inner(T_CustomTrigger, "Trigger");
	private static readonly Type T_DetectMode = AccessTools.Inner(T_CustomTrigger, "DetectMode");

	// get enums as ints
	private static readonly int V_Trigger_Reset = (int)Enum.Parse(T_Trigger, "Reset");
	private static readonly int V_Trigger_Teleport = (int)Enum.Parse(T_Trigger, "Teleport");
	private static readonly int V_Trigger_Finish = (int)Enum.Parse(T_Trigger, "Finish");
	private static readonly int V_Trigger_Playsound = (int)Enum.Parse(T_Trigger, "Playsound");
	private static readonly int V_Trigger_Animation = (int)Enum.Parse(T_Trigger, "Animation");
	private static readonly int V_Trigger_Event = (int)Enum.Parse(T_Trigger, "Event");
	private static readonly int V_Trigger_SwitchScene = (int)Enum.Parse(T_Trigger, "SwitchScene");

	private static readonly int V_DetectMode_PlayerOnly = (int)Enum.Parse(T_DetectMode, "PlayerOnly");
	private static readonly int V_DetectMode_PropsOnly = (int)Enum.Parse(T_DetectMode, "PropsOnly");
	private static readonly int V_DetectMode_SpecificProp = (int)Enum.Parse(T_DetectMode, "SpecificProp");

	private static readonly FieldInfo F_TriggerType = AccessTools.Field(T_CustomTrigger, "TriggerType");
	private static readonly FieldInfo F_Mode = AccessTools.Field(T_CustomTrigger, "Mode");
	private static readonly FieldInfo F_Destination = AccessTools.Field(T_CustomTrigger, "Destination");
	private static readonly FieldInfo F_source = AccessTools.Field(T_CustomTrigger, "source");
	private static readonly FieldInfo F_Sound = AccessTools.Field(T_CustomTrigger, "Sound");
	private static readonly FieldInfo F_TriggerEvent = AccessTools.Field(T_CustomTrigger, "TriggerEvent");
	private static readonly FieldInfo F_animation = AccessTools.Field(T_CustomTrigger, "animation");
	private static readonly FieldInfo F_anim = AccessTools.Field(T_CustomTrigger, "anim");
	private static readonly FieldInfo F_Detect = AccessTools.Field(T_CustomTrigger, "Detect");
	private static readonly FieldInfo F_TargetProp = AccessTools.Field(T_CustomTrigger, "TargetProp");

	private static MonoBehaviour AsMonoBehaviour(object instance) => (MonoBehaviour)instance;

	[HarmonyPatch]
	public static class ResetPatch {
		static MethodBase TargetMethod() => AccessTools.Method(T_CustomTrigger, "Reset");

		[HarmonyPrefix]
		static bool Prefix(object __instance) {
			F_Destination.SetValue(__instance, AsMonoBehaviour(__instance).transform.position);

			return false;
		}
	}

	[HarmonyPatch]
	public static class OnDrawGizmosPatch {
		static MethodBase TargetMethod() => AccessTools.Method(T_CustomTrigger, "OnDrawGizmos");

		[HarmonyPrefix]
		static bool Prefix(object __instance) {
			int triggerTypeVal = (int)F_TriggerType.GetValue(__instance);

			if (triggerTypeVal == V_Trigger_Teleport) {
				Gizmos.color = Color.red;

				Vector3 destination = (Vector3)F_Destination.GetValue(__instance);
				Gizmos.DrawSphere(destination, 0.25f);
			}

			return false;
		}
	}

	[HarmonyPatch]
	public static class StartPatch {
		static MethodBase TargetMethod() => AccessTools.Method(T_CustomTrigger, "Start");

		[HarmonyPrefix]
		static bool Prefix(object __instance) {
			int triggerTypeVal = (int)F_TriggerType.GetValue(__instance);

			if (triggerTypeVal == V_Trigger_Playsound) {
				GameObject go = AsMonoBehaviour(__instance).gameObject;

				AudioSource audioSrc = go.GetComponent<AudioSource>();

				if (audioSrc == null) {
					audioSrc = (AudioSource)go.AddComponent(typeof(AudioSource));
				}

				F_source.SetValue(__instance, audioSrc);
			}

			return false;
		}
	}

	[HarmonyPatch]
	public static class OnTriggerEnter2DPatch {
		static MethodBase TargetMethod() => AccessTools.Method(T_CustomTrigger, "OnTriggerEnter2D", new Type[] { typeof(Collider2D) });

		[HarmonyPrefix]
		static bool Prefix(object __instance, Collider2D col) {
			int detectVal = (int)F_Detect.GetValue(__instance);
			GameObject[] targetProp = (GameObject[])F_TargetProp.GetValue(__instance);

			if ((col.gameObject.layer != 8 && detectVal == V_DetectMode_PlayerOnly) ||
				(col.gameObject.layer == 8 && detectVal == V_DetectMode_PropsOnly) ||
				(detectVal == V_DetectMode_SpecificProp && !targetProp.Contains(col.gameObject))) {
				return false;
				}

				int triggerTypeVal = (int)F_TriggerType.GetValue(__instance);

			if (triggerTypeVal == V_Trigger_Reset) {
				LevelLoader.LevelLoaderPlugin.Instance.ReloadScene();
			} else if (triggerTypeVal == V_Trigger_Teleport) {
				Vector3 destination = (Vector3)F_Destination.GetValue(__instance);
				bool keepMomentum = (bool)F_Mode.GetValue(__instance);

				LevelLoader.LevelLoaderPlugin.TeleportPlayer(new Vector2(destination.x, destination.y), keepMomentum);
			} else if (triggerTypeVal == V_Trigger_Finish) {
				MonoBehaviour.print("Finished");
				PlayerPrefs.SetFloat("LastTime", GOIConstants.timer);
				PlayerPrefs.DeleteKey("NumSaves");
				PlayerPrefs.DeleteKey("SaveGame0");
				PlayerPrefs.DeleteKey("SaveGame1");
				PlayerPrefs.DeleteKey("targetScene");
				int @int = PlayerPrefs.GetInt("NumWins");
				@int++;
				PlayerPrefs.SetInt("NumWins", @int);
				Debug.Log("Game Done, deleting saves");
				PlayerPrefs.Save();
				SceneManager.LoadScene("Reward Loader");
			} else if (triggerTypeVal == V_Trigger_Playsound) {
				AudioSource audioSrc = (AudioSource)F_source.GetValue(__instance);
				AudioClip sound = (AudioClip)F_Sound.GetValue(__instance);
				bool mode = (bool)F_Mode.GetValue(__instance);

				if (!audioSrc.isPlaying) {
					audioSrc.PlayOneShot(sound);
				} if (mode) {
					Collider2D[] components = AsMonoBehaviour(__instance).GetComponents<Collider2D>();
					for (int i = 0; i < components.Length; i++) {
						components[i].enabled = false;
					}
				}
			}
			else if (triggerTypeVal == V_Trigger_Animation) {
				Animation animComponent = (Animation)F_animation.GetValue(__instance);
				string animName = (string)F_anim.GetValue(__instance);

				if (!string.IsNullOrEmpty(animName)) {
					animComponent.Play(animName);
				} else {
					animComponent.Play();
				}
			} else if (triggerTypeVal == V_Trigger_Event) {
				UnityEvent triggerEvent = (UnityEvent)F_TriggerEvent.GetValue(__instance);
				triggerEvent.Invoke();
			} else if (triggerTypeVal == V_Trigger_SwitchScene) {
				string sceneName = (string)F_anim.GetValue(__instance);

				LevelLoader.LevelLoaderPlugin.Instance.LoadScene(sceneName);
			}

			return false;
		}
	}
}

[HarmonyPatch]
public static class GravityControlLitePatch {
	private static readonly Type T_GravityControlLite = AccessTools.TypeByName("GravityControlLite");
	private static readonly Type T_Attractors = AccessTools.Inner(T_GravityControlLite, "Attractors");

	private static readonly FieldInfo F_gvec = AccessTools.Field(T_GravityControlLite, "gvec");
	private static readonly FieldInfo F_gravityWells = AccessTools.Field(T_GravityControlLite, "gravityWells");
	private static readonly FieldInfo F_gravmod = AccessTools.Field(T_GravityControlLite, "gravmod");

	// Fields for the nested Attractors class
	private static readonly FieldInfo F_Attractors_gravityWell = AccessTools.Field(T_Attractors, "gravityWell");
	private static readonly FieldInfo F_Attractors_gravityModifier = AccessTools.Field(T_Attractors, "gravityModifier");


	[HarmonyPatch]
	public static class OnTriggerStay2DPatch {
		static MethodBase TargetMethod() => AccessTools.Method(T_GravityControlLite, "OnTriggerStay2D", new Type[] { typeof(Collider2D) });

		[HarmonyPrefix]
		static bool Prefix(object __instance, Collider2D coll) {
			if (coll.attachedRigidbody == null) return false;

			Array gravityWells = (Array)F_gravityWells.GetValue(__instance);

			if (gravityWells == null) {
				Debug.LogWarning("gravityWells is null");
				return false;
			}
			foreach (object attractors in gravityWells) {
				Transform gravityWell = (Transform)F_Attractors_gravityWell.GetValue(attractors);

				float gravityModifier = (float)F_Attractors_gravityModifier.GetValue(attractors);

				Vector2 gvec = new Vector2(gravityWell.position.x, gravityWell.position.y) - coll.attachedRigidbody.position;

				F_gvec.SetValue(__instance, gvec);

				coll.attachedRigidbody.AddForce(2500f / gvec.sqrMagnitude * gvec.normalized * (1f + gravityModifier));
			}

			return false;
		}
	}

	[HarmonyPatch]
	public static class OnTriggerEnter2DPatch {
		static MethodBase TargetMethod() => AccessTools.Method(T_GravityControlLite, "OnTriggerEnter2D", new Type[] { typeof(Collider2D) });

		[HarmonyPrefix]
		static bool Prefix(object __instance, Collider2D coll) {
			if (coll.attachedRigidbody != null) {
				GOIConstants.ModifierSetEnabled<Vector2>("gravity", "zerograv", true);

				F_gravmod.SetValue(null, 1f);
			}

			return false;
		}
	}

	[HarmonyPatch]
	public static class OnTriggerExit2DPatch {
		static MethodBase TargetMethod() => AccessTools.Method(T_GravityControlLite, "OnTriggerExit2D", new Type[] { typeof(Collider2D) });

		[HarmonyPrefix]
		static bool Prefix(object __instance, Collider2D coll) {
			if (coll.attachedRigidbody != null) {
				GOIConstants.ModifierSetEnabled<Vector2>("gravity", "zerograv", false);

				F_gravmod.SetValue(null, 0f);
			}

			return false;
		}
	}
}
