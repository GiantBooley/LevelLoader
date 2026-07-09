using System;
using System.Collections.Generic;
using UnityEngine;
using FluffyUnderware.DevTools;

namespace FluffyUnderware.Curvy
{
	public abstract class CurvySplineBase : DTVersionedMonoBehaviour
	{

		public virtual Vector3 InterpolateFast(float tf)
		{
			return Vector3.zero;
		}

		public virtual float SegmentToTF(CurvySplineSegment segment)
		{
			return this.SegmentToTF(segment, 0f);
		}

		public virtual float SegmentToTF(CurvySplineSegment segment, float localF)
		{
			return 0f;
		}
		public virtual U InterpolateMetadata<T, U>(float tf)
		{
			return default(U);
		}

		public virtual object InterpolateMetadata(Type type, float tf)
		{
			return null;
		}

		public virtual Vector3 InterpolateScale(float tf)
		{
			return Vector3.zero;
		}

		public virtual Vector3 GetTangent(float tf, Vector3 position)
		{
			return Vector3.zero;
		}

		public virtual Vector3 GetTangentFast(float tf)
		{
			return Vector3.zero;
		}

		public virtual float TFToDistance(float tf, CurvyClamping clamping)
		{
			return 0f;
		}

		public float TFToDistance(float tf)
		{
			return this.TFToDistance(tf, CurvyClamping.Clamp);
		}

		public virtual float DistanceToTF(float distance, CurvyClamping clamping)
		{
			return 0f;
		}
		public virtual Vector3 GetOrientationUpFast(float tf)
		{
			return Vector3.zero;
		}

		public virtual Quaternion GetOrientationFast(float tf)
		{
			return this.GetOrientationFast(tf, false);
		}

		public virtual Quaternion GetOrientationFast(float tf, bool inverse)
		{
			return Quaternion.identity;
		}

		public virtual Vector3 GetTangent(float tf)
		{
			return Vector3.zero;
		}
		public virtual Component GetMetadata(Type type, float tf)
		{
			return null;
		}
		public abstract float Length { get; }
		public bool IsInitialized
		{
			get
			{
				return true;
			}
		}
		public virtual bool IsContinuous
		{
			get
			{
				return false;
			}
		}

		public virtual bool IsClosed
		{
			get
			{
				return false;
			}
		}

		public virtual bool Dirty
		{
			get
			{
				return false;
			}
		}

		public virtual Vector3 Move(ref float tf, ref int direction, float fDistance, CurvyClamping clamping)
		{
			return Vector3.zero;
		}
		public virtual Vector3[] GetApproximationUpVectors()
		{
			return new Vector3[0];
		}

		public virtual float GetNearestPointTF(Vector3 p)
		{
			return 0f;
		}

		public virtual void Refresh()
		{
		}

		public virtual void SetDirtyAll()
		{
		}
		public virtual Vector3[] GetApproximationT()
		{
			return new Vector3[0];
		}
		public virtual float GetNearestPointTF(Vector3 p, out Vector3 nearest)
		{
			nearest = Vector3.zero;
			return 0f;
		}
		public virtual Vector3[] GetApproximation(Space space = Space.Self)
		{
			return new Vector3[0];
		}
		public virtual void Clear()
		{
		}
		public virtual Vector3 MoveFast(ref float tf, ref int direction, float fDistance, CurvyClamping clamping)
		{
			return Vector3.zero;
		}

		public virtual Vector3 MoveByLengthFast(ref float tf, ref int direction, float distance, CurvyClamping clamping)
		{
			return Vector3.zero;
		}
		public virtual CurvySplineSegment TFToSegment(float tf, out float localF, CurvyClamping clamping)
		{
			localF = 0f;
			return null;
		}
		public virtual Vector3 Interpolate(float tf)
		{
			return Vector3.zero;
		}

		public virtual Vector3 Interpolate(float tf, CurvyInterpolation interpolation)
		{
			return Vector3.zero;
		}

		public abstract Bounds Bounds { get; }

		public virtual int Count
		{
			get
			{
				return 0;
			}
		}
	}
}
