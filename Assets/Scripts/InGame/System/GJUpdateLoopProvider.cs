using System;
using UnityEngine;

namespace GameJam.InGame.System
{
    public class GJUpdateLoopProvider : MonoBehaviour
    {
        private GJController _controller;

        public void Register(GJController controller)
        {
            _controller = controller;
        }

        private void Update()
        {
            if (_controller == null)
            {
                return;
            }

            var deltaTime = TimeSpan.FromSeconds(Time.deltaTime);
            _controller.OnUpdate(deltaTime);
        }
    }
}
