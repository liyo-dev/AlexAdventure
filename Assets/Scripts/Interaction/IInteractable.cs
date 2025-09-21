using UnityEngine;

public interface IInteractable
{
    string GetPrompt();           // Texto: "Pulsa E para ..."
    Transform GetTransform();     // Para UI (si quisieras seguir al objeto)
    bool CanInteract(GameObject interactor);
    void Interact(GameObject interactor);
}