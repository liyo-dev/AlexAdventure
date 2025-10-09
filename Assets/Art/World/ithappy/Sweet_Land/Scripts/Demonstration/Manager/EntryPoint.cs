using UnityEngine;

namespace ithappy
{
    public class EntryPoint : MonoBehaviour
    {
        [SerializeField] private CharacterBase[] _characters;

        private void Start()
        {
            Application.targetFrameRate = 120;
            
            for (int i = 0; i < _characters.Length; i++)
            {
                _characters[i].Initialize();
            }

            DistributePrioritiesEvenly();
        }

        private void DistributePrioritiesEvenly()
        {
            if (_characters.Length == 0) return;

            float priorityStep = 99f / (_characters.Length - 1);

            for (int i = 0; i < _characters.Length; i++)
            {
                if (_characters[i] == null) continue;
                _characters[i].SetPriority(Mathf.RoundToInt(i * priorityStep));
            }
        }
    }
}
