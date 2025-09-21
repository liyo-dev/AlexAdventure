using System;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Sesión genérica: dispara eventos al empezar y expone End() para cerrar.
/// Úsalo para abrir paneles de UI, reproducir Timeline, etc.
/// Llama a End() (por botón UI, Timeline Signal, etc.) para devolver control.
/// </summary>
public class UnityEventSession : MonoBehaviour, IInteractionSession
{
    public UnityEvent<GameObject> OnBegin; // recibe el interactor
    public UnityEvent OnEnd;

    private Action finishCb;

    public void BeginSession(GameObject interactor, Action onFinish)
    {
        finishCb = onFinish;
        OnBegin?.Invoke(interactor);
    }

    public void End()
    {
        OnEnd?.Invoke();
        var cb = finishCb;
        finishCb = null;
        cb?.Invoke();
    }
}