namespace Oblivion.Core.Feedback
{
    public interface IHitStopProvider
    {
        void HitStop(UnityEngine.MonoBehaviour runner, float timeScale, float durationSeconds);
    }
}

