using System;
using System.Collections.Generic;
using UnityEngine;

namespace LetterboxCamera
{
	[Serializable]
	public class ForceCameraRatio : MonoBehaviour
	{
		public void FindAllCamerasInScene()
		{
		}

		public void CalculateAndSetAllCameraRatios()
		{
		}

		public void SetCameraAnchor(Camera _camera, Vector2 _anchor)
		{
		}

		public CameraRatio[] GetCameras()
		{
			return this.cameras.ToArray();
		}

		public Vector2 ratio = new Vector2(16f, 9f);

		public bool forceRatioOnAwake = true;

		public bool listenForWindowChanges = true;

		public bool createCameraForLetterBoxRendering = true;

		public bool findCamerasAutomatically = true;

		public Color letterBoxCameraColor = new Color(0f, 0f, 0f, 1f);

		public List<CameraRatio> cameras;

		public Camera letterBoxCamera;
	}
}
