using System;
using UnityEngine;

namespace LetterboxCamera
{
	[Serializable]
	public class CameraRatio
	{
		public enum CameraAnchor
		{
			Center,
			Top,
			Bottom,
			Left,
			Right,
			TopLeft,
			TopRight,
			BottomLeft,
			BottomRight
		}

		public CameraRatio(Camera _camera, Vector2 _anchor)
		{
			this.camera = _camera;
			this.vectorAnchor = _anchor;
			this.originViewPort = this.camera.rect;
		}

		public void ResetOriginViewport()
		{
			this.originViewPort = this.camera.rect;
			this.SetAnchorBasedOnEnum(this.anchor);
		}

		public void SetAnchorBasedOnEnum(CameraRatio.CameraAnchor _anchor)
		{
			switch (_anchor)
			{
				case CameraRatio.CameraAnchor.Center:
					this.vectorAnchor = new Vector2(0.5f, 0.5f);
					return;
				case CameraRatio.CameraAnchor.Top:
					this.vectorAnchor = new Vector2(0.5f, 1f);
					return;
				case CameraRatio.CameraAnchor.Bottom:
					this.vectorAnchor = new Vector2(0.5f, 0f);
					return;
				case CameraRatio.CameraAnchor.Left:
					this.vectorAnchor = new Vector2(0f, 0.5f);
					return;
				case CameraRatio.CameraAnchor.Right:
					this.vectorAnchor = new Vector2(1f, 0.5f);
					return;
				case CameraRatio.CameraAnchor.TopLeft:
					this.vectorAnchor = new Vector2(0f, 1f);
					return;
				case CameraRatio.CameraAnchor.TopRight:
					this.vectorAnchor = new Vector2(1f, 1f);
					return;
				case CameraRatio.CameraAnchor.BottomLeft:
					this.vectorAnchor = new Vector2(0f, 0f);
					return;
				case CameraRatio.CameraAnchor.BottomRight:
					this.vectorAnchor = new Vector2(1f, 0f);
					return;
				default:
					return;
			}
		}

		public void CalculateAndSetCameraRatio(float _width, float _height, bool _horizontalLetterbox)
		{
			Rect rect = default(Rect);
			if (_horizontalLetterbox)
			{
				rect.height = _height;
				rect.width = 1f;
			}
			else
			{
				rect.height = 1f;
				rect.width = _width;
			}
			Rect rect2 = default(Rect);
			Rect rect3 = default(Rect);
			rect2.width = this.originViewPort.width;
			rect2.height = this.originViewPort.width * (rect.height / rect.width);
			rect2.x = this.originViewPort.x;
			rect2.y = Mathf.Lerp(this.originViewPort.y, this.originViewPort.y + (this.originViewPort.height - rect2.height), this.vectorAnchor.y);
			rect3.width = this.originViewPort.height * (rect.width / rect.height);
			rect3.height = this.originViewPort.height;
			rect3.x = Mathf.Lerp(this.originViewPort.x, this.originViewPort.x + (this.originViewPort.width - rect3.width), this.vectorAnchor.x);
			rect3.y = this.originViewPort.y;
			if (rect2.height >= rect3.height && rect2.width >= rect3.width)
			{
				if (rect2.height <= this.originViewPort.height && rect2.width <= this.originViewPort.width)
				{
					this.camera.rect = rect2;
					return;
				}
				this.camera.rect = rect3;
				return;
			}
			else
			{
				if (rect3.height <= this.originViewPort.height && rect3.width <= this.originViewPort.width)
				{
					this.camera.rect = rect3;
					return;
				}
				this.camera.rect = rect2;
				return;
			}
		}

		[Tooltip("The Camera assigned to have an automatically calculated Viewport Ratio")]
		public Camera camera;

		[Tooltip("When a Camera Viewport is shrunk to fit a ratio, it will anchor the new Viewport Rectangle at the given point (relative to the original, unshrunk Viewport)")]
		public CameraRatio.CameraAnchor anchor;

		[HideInInspector]
		public Vector2 vectorAnchor;

		private Rect originViewPort;
	}
}
