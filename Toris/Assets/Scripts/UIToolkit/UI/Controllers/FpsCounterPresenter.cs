using UnityEngine;

namespace OutlandHaven.UIToolkit
{
    public sealed class FpsCounterPresenter
    {
        private const float SampleIntervalSeconds = 0.5f;
        private const string InitialLabel = "FPS: --";

        private float _elapsedSeconds;
        private int _frameCount;

        public string CurrentText { get; private set; } = InitialLabel;

        public bool TryTick(float unscaledDeltaTime, out string text)
        {
            text = CurrentText;
            if (unscaledDeltaTime <= 0f)
            {
                return false;
            }

            _elapsedSeconds += unscaledDeltaTime;
            _frameCount++;

            if (_elapsedSeconds < SampleIntervalSeconds)
            {
                return false;
            }

            int framesPerSecond = Mathf.RoundToInt(_frameCount / _elapsedSeconds);
            CurrentText = $"FPS: {framesPerSecond}";
            text = CurrentText;
            _elapsedSeconds = 0f;
            _frameCount = 0;
            return true;
        }

        public void Reset()
        {
            _elapsedSeconds = 0f;
            _frameCount = 0;
            CurrentText = InitialLabel;
        }
    }
}
