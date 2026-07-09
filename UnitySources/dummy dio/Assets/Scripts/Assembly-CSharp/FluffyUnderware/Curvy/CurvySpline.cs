using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using JetBrains.Annotations;
using UnityEngine;

namespace FluffyUnderware.Curvy
{
	public class CurvySpline : CurvySplineBase, ISerializationCallbackReceiver
	{
		public CurvySpline()
		{
		}

		public CurvyInterpolation Interpolation
		{
			get;
			set;
		}

		public bool RestrictTo2D
		{
			get;
			set;
		}

		public float AutoHandleDistance
		{
			get;
			set;
		}

		public bool Closed
		{
			get;
			set;
		}

		public bool AutoEndTangents
		{
			get;
			set;
		}

		public CurvyOrientation Orientation
		{
			get;
			set;
		}

		public CurvyUpdateMethod UpdateIn
		{
			get;
			set;
		}

		public Color GizmoColor
		{
			get;
			set;
		}

		public Color GizmoSelectionColor
		{
			get;
			set;
		}

		public int CacheDensity
		{
			get;
			set;
		}

		public float MaxPointsPerUnit
		{
			get;
			set;
		}

		public bool UsePooling
		{
			get;
			set;
		}

		public bool UseThreading
		{
			get;
			set;
		}

		public bool CheckTransform
		{
			get;
			set;
		}

		public float Tension
		{
			get;
			set;
		}

		public float Continuity {

			get;
			set;
		}

		public float Bias
		{
			get;
			set;
		}

		public override Bounds Bounds
		{
			get;
		}

		public override int Count
		{
			get;
		}

		public int ControlPointCount
		{
			get;
		}

		public int CacheSize
		{
			get;
		}

		public override float Length
		{
			get;
		}

		public override bool Dirty
		{
			get;
		}

		public CurvySplineSegment this[int idx]
		{
			get
			{
				return this.Segments[idx];
			}
		}

		[Obsolete("Either use the indexer property (this[idx]), or the ControlPointsList and the IsControlPointASegment(controlPoint)")]
		public List<CurvySplineSegment> Segments
		{
			get;
		}

		public ReadOnlyCollection<CurvySplineSegment> ControlPointsList
		{
			get;
		}

		[CanBeNull]
		public CurvySplineSegment FirstVisibleControlPoint
		{
			get;
		}

		[CanBeNull]
		public CurvySplineSegment LastVisibleControlPoint
		{
			get;
		}

		public CurvySplineSegment FirstSegment
		{
			get;
		}

		public CurvySplineSegment LastSegment
		{
			get;
		}

		public override bool IsContinuous
		{
			get;
		}

		public override bool IsClosed
		{
			get;
		}

		public CurvySpline NextSpline
		{
			get;
		}

		public CurvySpline PreviousSpline
		{
			get;
		}

		public void Start()
		{
		}

		public static CurvySpline Create()
		{
			CurvySpline component = new GameObject("Curvy Spline", new Type[] { typeof(CurvySpline) }).GetComponent<CurvySpline>();
			return component;
		}

		public static CurvySpline Create(CurvySpline takeOptionsFrom)
		{
			CurvySpline curvySpline = CurvySpline.Create();
			return curvySpline;
		}

		public static int CalculateCacheSize(int density, float splineLength, float maxPointsPerUnit)
		{
			return 0;
		}

		public static float CalculateSamplingPointsPerUnit(int density, float maxPointsPerUnit)
		{
			return 0f;
		}

		public static Vector3 Bezier(Vector3 T0, Vector3 P0, Vector3 P1, Vector3 T1, float f)
		{
			return Vector3.zero;
		}

		public static float BezierTangent(float T0, float P0, float P1, float T1, float t)
		{
			return 0f;
		}

		public static Vector3 BezierTangent(Vector3 T0, Vector3 P0, Vector3 P1, Vector3 T1, float f)
		{			
			return Vector3.zero;
		}

		public static Vector3 CatmullRom(Vector3 T0, Vector3 P0, Vector3 P1, Vector3 T1, float f)
		{
			return Vector3.zero;
		}

		public static Vector3 TCB(Vector3 T0, Vector3 P0, Vector3 P1, Vector3 T1, float f, float FT0, float FC0, float FB0, float FT1, float FC1, float FB1)
		{
			return Vector3.zero;
		}

		public override Vector3 Interpolate(float tf)
		{
			return Vector3.zero;
		}

		public override Vector3 Interpolate(float tf, CurvyInterpolation interpolation)
		{
			return Vector3.zero;
		}

		public override Vector3 InterpolateFast(float tf)
		{
			return Vector3.zero;
		}

		public override Component GetMetadata(Type type, float tf)
		{
			return new Component();
		}

		public override U InterpolateMetadata<T, U>(float tf)
		{
			return default(U);
		}

		public override object InterpolateMetadata(Type type, float tf)
		{
			return null;
		}

		public override Vector3 InterpolateScale(float tf)
		{
			return Vector3.zero;
		}

		public override Vector3 GetOrientationUpFast(float tf)
		{
			return Vector3.zero;

		}

		public override Quaternion GetOrientationFast(float tf, bool inverse)
		{
			return new Quaternion();
		}

		public override Vector3 GetTangent(float tf)
		{
			return Vector3.zero;
		}

		public override Vector3 GetTangent(float tf, Vector3 position)
		{
			return Vector3.zero;
		}

		public override Vector3 GetTangentFast(float tf)
		{
			return Vector3.zero;
		}

		public override float TFToDistance(float tf, CurvyClamping clamping)
		{
			return 0f;
		}

		public override CurvySplineSegment TFToSegment(float tf, out float localF, CurvyClamping clamping)
		{
			localF = 0f;
			return null;
		}

		public int TFToSegmentIndex(float tf, CurvyClamping clamping = CurvyClamping.Clamp)
		{
			return 0;
		}

		public override float SegmentToTF(CurvySplineSegment segment)
		{
			return 0f;
		}

		public override float SegmentToTF(CurvySplineSegment segment, float localF)
		{
			return 0f;
		}

		public override float DistanceToTF(float distance, CurvyClamping clamping)
		{
			return 0f;
		}

		public CurvySplineSegment DistanceToSegment(float distance, CurvyClamping clamping = CurvyClamping.Clamp)
		{
			return null;
		}

		public CurvySplineSegment DistanceToSegment(float distance, out float localDistance, CurvyClamping clamping = CurvyClamping.Clamp)
		{
			localDistance = 0f;
			return null;
		}

		public override Vector3 Move(ref float tf, ref int direction, float fDistance, CurvyClamping clamping)
		{
			return Vector3.zero;
		}

		public override Vector3 MoveFast(ref float tf, ref int direction, float fDistance, CurvyClamping clamping)
		{
			return Vector3.zero;
		}

		public override Vector3 MoveByLengthFast(ref float tf, ref int direction, float distance, CurvyClamping clamping)
		{
			return Vector3.zero;
		}

		public CurvySplineSegment Add()
		{
			return null;
		}

		public CurvySplineSegment[] Add(params Vector3[] controlPoints)
		{
			return new CurvySplineSegment[0];
		}

		public CurvySplineSegment InsertBefore(CurvySplineSegment controlPoint)
		{
			return null;
		}

		public CurvySplineSegment InsertAfter(CurvySplineSegment controlPoint)
		{
			return null;
		}

		public override void Clear()
		{
		}

		public void Delete(CurvySplineSegment controlPoint)
		{
		}

		public override Vector3[] GetApproximation(Space space = Space.Self)
		{
			return new Vector3[0];
		}

		public override Vector3[] GetApproximationT()
		{
			return new Vector3[0];
		}

		public override Vector3[] GetApproximationUpVectors()
		{
			return new Vector3[0];
		}

		public override float GetNearestPointTF(Vector3 p)
		{
			return 0f;
		}

		public override float GetNearestPointTF(Vector3 p, out Vector3 nearest)
		{
			nearest = Vector3.zero;
			return 0f;
		}

		public float GetNearestPointTF(Vector3 p, int startSegmentIndex = 0, int stopSegmentIndex = -1)
		{
			return 0f;
		}

		public float GetNearestPointTF(Vector3 p, out Vector3 nearest, int startSegmentIndex = 0, int stopSegmentIndex = -1)
		{
			nearest = Vector3.zero;
			return 0f;
		}

		public float GetNearestPointTF(Vector3 p, out Vector3 nearestPoint, [CanBeNull] out CurvySplineSegment nearestSegment, out float nearestSegmentF, int startSegmentIndex = 0, int stopSegmentIndex = -1)
		{
			nearestPoint = Vector3.zero;
			nearestSegmentF = 0f;
			nearestSegment = null;
			return 0f;
		}

		public override void Refresh()
		{
		}

		public override void SetDirtyAll()
		{
		}

		[Obsolete("Use SetDirtyAll(DirtyingType dirtyingType, bool dirtyConnectedControlPoints) instead")]
		public void SetDirtyAll(bool dirtyCurve = true, bool dirtyOrientation = true)
		{
			SplineDirtyingType splineDirtyingType;
			if (dirtyCurve && dirtyOrientation)
			{
				splineDirtyingType = SplineDirtyingType.Everything;
			}
			else if (!dirtyCurve && dirtyOrientation)
			{
				splineDirtyingType = SplineDirtyingType.OrientationOnly;
			}
			else
			{
				Debug.LogError("[Curvy] Invalid dirtying request");
				splineDirtyingType = SplineDirtyingType.Everything;
			}
			this.SetDirtyAll(splineDirtyingType, true);
		}

		public void SetDirtyAll(SplineDirtyingType dirtyingType, bool dirtyConnectedControlPoints)
		{
		}

		[Obsolete("Use SetDirty(CurvySplineSegment controlPoint, DirtyingType dirtyingType) instead")]
		public void SetDirty(CurvySplineSegment controlPoint, bool dirtyCurve = true, bool dirtyOrientation = true)
		{
		}

		public void SetDirty(CurvySplineSegment controlPoint, SplineDirtyingType dirtyingType)
		{
		}

		public void SetDirtyPartial(CurvySplineSegment controlPoint, SplineDirtyingType dirtyingType)
		{
		}

		[Obsolete("SyncHierarchyFromSpline does not need to be called from outside CurvySpline no more. CurvySpline now handles this internally")]
		public void SyncHierarchyFromSpline(bool renameControlPoints = true)
		{
		}

		public Vector3 ToWorldPosition(Vector3 localPosition)
		{
			return Vector3.zero;
		}

		public void SyncSplineFromHierarchy()
		{
		}

		public bool IsPlanar(out int ignoreAxis)
		{
			ignoreAxis = 0;
			return true;
		}

		public bool IsPlanar(out bool xplanar, out bool yplanar, out bool zplanar)
		{
			xplanar = true;
			yplanar = true;
			zplanar = true;
			return true;
		}

		public bool IsPlanar(CurvyPlane plane)
		{
			return true;
		}

		public void MakePlanar(CurvyPlane plane)
		{
		}

		public void Subdivide(CurvySplineSegment fromCP = null, CurvySplineSegment toCP = null)
		{
		}

		public void Simplify(CurvySplineSegment fromCP = null, CurvySplineSegment toCP = null)
		{
		}

		public void Equalize(CurvySplineSegment fromCP = null, CurvySplineSegment toCP = null)
		{
		}

		public void Normalize()
		{
		}

		public void MakePlanar(int axis)
		{
		}

		public Vector3 SetPivot(float xRel = 0f, float yRel = 0f, float zRel = 0f, bool preview = false)
		{
			return Vector3.zero;
		}

		public void Flip()
		{
		}

		public void MoveControlPoints(int startIndex, int count, CurvySplineSegment destCP)
		{
		}

		public void JoinWith(CurvySplineSegment destCP)
		{
		}

		public CurvySpline Split(CurvySplineSegment controlPoint)
		{
			return null;
		}

		public void SetFirstControlPoint(CurvySplineSegment controlPoint)
		{
		}

		public bool IsControlPointAnOrientationAnchor(CurvySplineSegment controlPoint)
		{
			return true;
		}

		public bool CanControlPointHaveFollowUp(CurvySplineSegment controlPoint)
		{
			return true;
		}

		public short GetControlPointIndex(CurvySplineSegment controlPoint)
		{
			return 0;
		}

		public short GetSegementIndex(CurvySplineSegment segment)
		{
			return 0;
		}

		[CanBeNull]
		public CurvySplineSegment GetNextControlPoint(CurvySplineSegment controlPoint)
		{
			return null;
		}

		[CanBeNull]
		public CurvySplineSegment GetNextControlPointUsingFollowUp(CurvySplineSegment controlPoint)
		{
			return null;
		}

		[CanBeNull]
		public CurvySplineSegment GetPreviousControlPoint(CurvySplineSegment controlPoint)
		{
			return null;
		}

		[CanBeNull]
		public CurvySplineSegment GetPreviousControlPointUsingFollowUp(CurvySplineSegment controlPoint)
		{
			return null;
		}

		[CanBeNull]
		public CurvySplineSegment GetNextSegment(CurvySplineSegment segment)
		{
			return null;
		}

		[CanBeNull]
		public CurvySplineSegment GetPreviousSegment(CurvySplineSegment segment)
		{
			return null;
		}

		public bool IsControlPointASegment(CurvySplineSegment controlPoint)
		{
			return true;
		}

		public bool IsControlPointVisible(CurvySplineSegment controlPoint)
		{
			return true;
		}

		public short GetControlPointOrientationAnchorIndex(CurvySplineSegment controlPoint)
		{
			return 0;
        }

		public void SetFromString(string fieldAndValue)
		{
		}

		public void OnBeforeSerialize()
		{
		}

		public void OnAfterDeserialize()
		{
		}

		public const string VERSION = "";

		public const string APIVERSION = "";

		public const string WEBROOT = "";

		public const string DOCLINK = "";

		[SerializeField]
		[HideInInspector]
		[Obsolete("Use the readonly version which is ControlPointsList")]
		public List<CurvySplineSegment> ControlPoints = new List<CurvySplineSegment>();

	}
}
