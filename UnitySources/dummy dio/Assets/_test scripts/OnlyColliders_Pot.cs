using System;
using UnityEngine;

namespace Modpack.LevelLoader
{
    public class OnlyColliders_Pot : MonoBehaviour
    {
        private void Start()
        {
            base.gameObject.AddComponent<PlatformEffector2D>();
            PlatformEffector2D component = base.gameObject.GetComponent<PlatformEffector2D>();
            component.useOneWay = false;
            component.useColliderMask = true;
            component.colliderMask = -539230977;
            PolygonCollider2D[] components = base.gameObject.GetComponents<PolygonCollider2D>();
            for (int i = 0; i < components.Length; i++)
            {
                components[i].usedByEffector = true;
            }
        }
    }
}