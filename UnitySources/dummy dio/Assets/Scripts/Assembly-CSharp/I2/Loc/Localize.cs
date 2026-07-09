using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace I2.Loc
{
	[AddComponentMenu("I2/Localization/I2 Localize")]
	public class Localize : MonoBehaviour
	{
		public enum TermModification
		{
			DontModify,
			ToUpper,
			ToLower,
			ToUpperFirst,
			ToTitle
		}

		public string Term
		{
			get;
			set;
		}

		public string SecondaryTerm
		{
			get;
			set;
		}


		public bool HasCallback()
		{
			return true;
		}

		public void OnLocalize(bool Force = false)
		{
		}

		public bool FindTarget()
		{
			return true;
		}

		public void GetFinalTerms(out string primaryTerm, out string secondaryTerm)
		{
			primaryTerm = "";
			secondaryTerm = "";
		}

		public string GetMainTargetsText()
		{
			return "";
		}

		public void SetFinalTerms(string Main, string Secondary, out string primaryTerm, out string secondaryTerm, bool RemoveNonASCII)
		{
			primaryTerm = "";
			secondaryTerm = "";
		}

		public void SetTerm(string primary)
		{
		}

		public void SetTerm(string primary, string secondary)
		{
		}

		public void UpdateAssetDictionary()
		{
		}


		public T FindTranslatedObject<T>(string value) where T : global::UnityEngine.Object
		{
			return default(T);
		}

		public bool HasTranslatedObject(global::UnityEngine.Object Obj)
		{
			return true;
		}

		public void AddTranslatedObject(global::UnityEngine.Object Obj)
		{
		}

		public void SetGlobalLanguage(string Language)
		{
		}

		public string mTerm = string.Empty;

		public string mTermSecondary = string.Empty;

		[NonSerialized]
		public string FinalTerm;

		[NonSerialized]
		public string FinalSecondaryTerm;

		public Localize.TermModification PrimaryTermModifier;

		public Localize.TermModification SecondaryTermModifier;

		public string TermPrefix;

		public string TermSuffix;

		public bool LocalizeOnAwake = true;

		private string LastLocalizedLanguage;

		public bool IgnoreRTL;

		public int MaxCharactersInRTL;

		public bool IgnoreNumbersInRTL = true;

		public bool CorrectAlignmentForRTL = true;

		public bool AddSpacesToJoinedLanguages;

		public bool AllowLocalizedParameters = true;

		public List<global::UnityEngine.Object> TranslatedObjects = new List<global::UnityEngine.Object>();

		[NonSerialized]
		public Dictionary<string, global::UnityEngine.Object> mAssetDictionary = new Dictionary<string, global::UnityEngine.Object>(StringComparer.Ordinal);

		public UnityEvent LocalizeEvent = new UnityEvent();

		public static string MainTranslation;

		public static string SecondaryTranslation;

		public static string CallBackTerm;

		public static string CallBackSecondaryTerm;

		public static Localize CurrentLocalizeComponent;

		public bool AlwaysForceLocalize;

		[SerializeField]
		public EventCallback LocalizeCallBack = new EventCallback();

		public bool mGUI_ShowReferences;

		public bool mGUI_ShowTems = true;

		public bool mGUI_ShowCallback;

		public ILocalizeTarget mLocalizeTarget;

		public string mLocalizeTargetName;
	}
}
