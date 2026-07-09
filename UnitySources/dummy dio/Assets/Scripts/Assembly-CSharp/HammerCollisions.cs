using System;
using System.Collections.Generic;
using UnityEngine;

public class HammerCollisions : MonoBehaviour
{

	public ImpactBlur impactBlur;

	public Dictionary<Collider2D, ContactPoint2D[]> collisionPoints;

	public PhysicsMaterial2D staticFriction;

	public PhysicsMaterial2D slidingFriction;

	public MeshRenderer headMesh;

	public ParticleSystem debris;

	public ParticleSystem sparks;

	public ParticleSystem dust;

	public AudioClip[] whooshes;

	public GameObject player;

	public HitSoundProvider hs;
}
