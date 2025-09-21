using System;
using UnityEngine;

public interface IInteractionSession
{
    /// <summary>
    /// Empieza la sesión de control. Llama a onFinish() cuando termines para devolver el control.
    /// </summary>
    void BeginSession(GameObject interactor, Action onFinish);
}