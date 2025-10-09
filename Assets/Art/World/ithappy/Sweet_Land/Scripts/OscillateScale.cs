using UnityEngine;
using System.Collections;

namespace ithappy
{
    public class OscillateScale : MonoBehaviour
    {
        public Vector3 scaleAxis = Vector3.one;
        public float scaleFactor = 2f;
        public float duration = 2f;
        public bool useRandomDelay = false; // Toggle random delay
        public float maxRandomDelay = 1f; // Maximum random delay
        public bool useFixedDelay = false; // Toggle fixed delay
        public float fixedDelay = 0f; // Fixed delay duration
        public float delayBetweenCycles = 0f; // Delay between animation cycles

        private Vector3 startScale;
        private float timeElapsed = 0f;
        private float delayElapsed = 0f;
        private bool isReversing = false;
        private float randomDelay = 0f;

        void Start()
        {
            startScale = transform.localScale;

            if (useRandomDelay)
            {
                randomDelay = Random.Range(0f, maxRandomDelay);
            }
        }

        void Update()
        {
            if (useFixedDelay && delayElapsed < fixedDelay)
            {
                delayElapsed += Time.deltaTime;
                return;
            }

            if (timeElapsed < randomDelay)
            {
                timeElapsed += Time.deltaTime;
                return;
            }

            float progress = (timeElapsed - randomDelay) / (duration / 2f);
            progress = Mathf.Clamp01(progress);

            progress = EaseInOut(progress);

            Vector3 currentScale = startScale + scaleAxis.normalized * (scaleFactor - 1) * (isReversing ? (1 - progress) : progress);

            transform.localScale = currentScale;

            timeElapsed += Time.deltaTime;

            if (timeElapsed >= duration / 2f + randomDelay)
            {
                timeElapsed = 0f;
                isReversing = !isReversing;

                StartCoroutine(CycleDelay());
            }
        }

        private IEnumerator CycleDelay()
        {
            yield return new WaitForSeconds(delayBetweenCycles);
        }

        private float EaseInOut(float t)
        {
            return t < 0.5f ? 4 * t * t * t : 1 - Mathf.Pow(-2 * t + 2, 3) / 2;
        }
    }
}
