using UnityEngine;

namespace ithappy
{
    public class TextureOffsetAnimator : MonoBehaviour
    {
        public enum OffsetAxis
        {
            X,
            Y
        }

        [Header("Material Settings")]
        public int materialIndex = 0; // Индекс материала (слот)

        [Header("Offset Settings")]
        public OffsetAxis offsetAxis = OffsetAxis.X;
        public float offsetSpeed = 1f;

        private Material targetMaterial;
        private Vector2 currentOffset = Vector2.zero;

        void Start()
        {
            MeshRenderer targetMeshRenderer = GetComponent<MeshRenderer>();
            if (targetMeshRenderer == null || materialIndex < 0 || materialIndex >= targetMeshRenderer.materials.Length)
            {
                Debug.LogError("Invalid MeshRenderer or Material Index!");
                enabled = false;
                return;
            }

            // Получаем выбранный материал
            targetMaterial = targetMeshRenderer.materials[materialIndex];
        }

        void Update()
        {
            if (targetMaterial == null)
                return;

            float deltaOffset = offsetSpeed * Time.deltaTime;

            if (offsetAxis == OffsetAxis.X)
            {
                currentOffset.x += deltaOffset;
            }
            else if (offsetAxis == OffsetAxis.Y)
            {
                currentOffset.y += deltaOffset;
            }

            // Применяем смещение текстуры
            targetMaterial.mainTextureOffset = currentOffset;
        }
    }
}
