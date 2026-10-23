using System;
using System.Reflection;
using UnityEngine;

namespace LevelLoader {
	public class Saviour : MonoBehaviour
	{
		public MonoBehaviour pc;

		public MonoBehaviour cc;

		private bool willPlayAnimation;

		// Reflection
		private static readonly Type playerControlType;
		private static readonly Type cameraControlType;

		private static readonly FieldInfo playerControlLoadFinishedField;
		private static readonly FieldInfo cameraControlLoadFinishedField;

		private static readonly MethodInfo playerControlPlayOpeningAnimationMethod;

		static Saviour() {
			foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies()) {
				if (assembly.GetName().Name == "Assembly-CSharp") {
					playerControlType = assembly.GetType("PlayerControl");
					cameraControlType = assembly.GetType("CameraControl");
					break;
				}
			}
			playerControlLoadFinishedField = playerControlType.GetField("loadFinished", BindingFlags.Instance | BindingFlags.Public);
			cameraControlLoadFinishedField = cameraControlType.GetField("loadFinished", BindingFlags.Instance | BindingFlags.Public);

			playerControlPlayOpeningAnimationMethod = playerControlType.GetMethod("PlayOpeningAnimation", new Type[] {});
		}

		private void Start()
		{
			willPlayAnimation = true;
		}

		private void Update()
		{
			if (willPlayAnimation)
			{
				playerControlPlayOpeningAnimationMethod.Invoke(pc, new System.Object[] {});
				willPlayAnimation = false;
				playerControlLoadFinishedField.SetValue(pc, true);
				cameraControlLoadFinishedField.SetValue(cc, true);
			}
		}
	}
}
