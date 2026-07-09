using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class layertester : MonoBehaviour
{
    public int layer = 0;
    // Start is called before the first frame update
    void Start()
    {
        gameObject.AddComponent<PlatformEffector2D>().colliderMask = layer;
    }

    // Update is called once per frame
    void Update()
    {
    }
}
