using UnityEngine;

public interface IInteractable
{
    void Interact(PlayerHands playerHands);
    string GetInteractionPrompt();
}
