using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Modpack.LevelLoader
{
    public class LoadWithoutSaviour : MonoBehaviour
    {
        public bool playStartingAnimation;

        public PlayerControl pc;

        public CameraControl cc;

        // Start is called before the first frame update
        void Start()
        {
            this.pc.loadFinished = true;
            this.cc.loadFinished = true;
            this.pc.StartAnimator();
        }

        // Update is called once per frame
        void Update()
        {
            if (playStartingAnimation)
            {
                this.pc.PlayOpeningAnimation();
                playStartingAnimation = false;
                this.pc.loadFinished = true;
                this.cc.loadFinished = true;
            }
        }
    }
}