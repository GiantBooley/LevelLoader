using System;
using UnityEngine;

namespace Modpack.LevelLoader
{
    public class OnlyColliders_Hammer : MonoBehaviour
    {
        private void Start()
        {
            base.gameObject.GetComponent<PolygonCollider2D>().usedByEffector = true;
            base.gameObject.AddComponent<PlatformEffector2D>();
            PlatformEffector2D component = base.gameObject.GetComponent<PlatformEffector2D>();
            component.useOneWay = false;
            component.useColliderMask = true;
            component.colliderMask = -270795137;
        }
    }
}