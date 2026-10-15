using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GameJam.InGame.Effect
{
    // A single dash slash trail (line segment). Stays until Flow ends, then fades out and is destroyed.
    public class GJFlowTrail
    {
        private readonly LineRenderer _lineRenderer;
        private readonly Color _color;
        private TimeSpan _fadeDuration = TimeSpan.Zero;
        private TimeSpan _leftFadeTime = TimeSpan.Zero;

        public Vector2 StartPosition { get; }
        public Vector2 EndPosition { get; }
        public bool IsFading { get; private set; }
        public bool IsFadeFinished => IsFading && _leftFadeTime <= TimeSpan.Zero;

        public GJFlowTrail(LineRenderer lineRenderer, Vector2 startPosition, Vector2 endPosition, float width, Color color)
        {
            _lineRenderer = lineRenderer;
            _color = color;
            StartPosition = startPosition;
            EndPosition = endPosition;

            _lineRenderer.useWorldSpace = true;
            _lineRenderer.positionCount = 2;
            _lineRenderer.SetPosition(0, startPosition);
            _lineRenderer.SetPosition(1, endPosition);
            _lineRenderer.startWidth = width;
            _lineRenderer.endWidth = width;
            SetAlpha(1f);
        }

        public void StartFadeOut(TimeSpan fadeDuration)
        {
            IsFading = true;
            _fadeDuration = fadeDuration;
            _leftFadeTime = fadeDuration;
        }

        public void UpdateTrail(TimeSpan deltaTime)
        {
            if (IsFading == false)
            {
                return;
            }

            _leftFadeTime -= deltaTime;
            var alpha = _fadeDuration > TimeSpan.Zero
                ? Mathf.Clamp01((float)(_leftFadeTime.TotalSeconds / _fadeDuration.TotalSeconds))
                : 0f;
            SetAlpha(alpha);
        }

        public void DestroyTrail()
        {
            Object.Destroy(_lineRenderer.gameObject);
        }

        private void SetAlpha(float alpha)
        {
            var color = new Color(_color.r, _color.g, _color.b, _color.a * alpha);
            _lineRenderer.startColor = color;
            _lineRenderer.endColor = color;
        }
    }
}
