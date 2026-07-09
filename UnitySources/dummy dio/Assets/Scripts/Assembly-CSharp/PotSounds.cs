using System;
using System.Collections.Generic;
using UnityEngine;

public class PotSounds : MonoBehaviour
{

	public AudioClip[] scrapes;

	public AudioClip[] clunks;

	public AudioClip[] hardclunks;

	public AudioClip[] rolls;

	public AudioClip[] splashes;

	public PhysicsMaterial2D staticFriction;

	public PhysicsMaterial2D slidingFriction;

	public ParticleSystem sparks;

	public ParticleSystem debris;

	public ParticleSystem water;

	public PolygonCollider2D bottomCol;

	public Rigidbody2D potRB;
}
