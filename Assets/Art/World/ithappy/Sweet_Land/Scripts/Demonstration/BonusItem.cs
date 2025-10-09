using System.Collections;
using UnityEngine;

namespace ithappy
{
    public class BonusItem : MonoBehaviour
    {
        [SerializeField] private float _reload;

        private MeshRenderer _meshRenderer;
        private Collider _collider;
        private AudioSource _audioSource;

        private void Start()
        {
            _meshRenderer = GetComponent<MeshRenderer>();
            _collider = GetComponent<Collider>();
            _audioSource = GetComponent<AudioSource>();
        }

        private void OnTriggerEnter(Collider collider)
        {
            BonusSeeker bonusSeeker = collider.GetComponent<BonusSeeker>();
            PlayerCharacterInputBase player = collider.GetComponent<PlayerCharacterInputBase>();
            if (bonusSeeker != null || player != null)
            {
                _meshRenderer.enabled = false;
                _collider.enabled = false;
                _audioSource.Play();
                StartCoroutine(ReloadProgress());
            }
        }

        private IEnumerator ReloadProgress()
        {
            yield return new WaitForSeconds(_reload);

            _collider.enabled = true;
            _meshRenderer.enabled = true;
        }
    }
}
