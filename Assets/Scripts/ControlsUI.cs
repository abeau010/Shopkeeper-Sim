using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

// Controls reminder in the bottom-right corner
public class ControlsUI : MonoBehaviour
{
    [SerializeField] private GameObject controls;
    [SerializeField] private TextMeshProUGUI tip;
     private const float MessageSeconds = 3f;

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
        {
            if (controls != null)
            {
                controls.SetActive(!controls.activeSelf); // Disables if active, other way around if active
            }
        }
    }

    public void ShowMessage(string message)
    {
        if (tip != null)
        {
            StopAllCoroutines();
            StartCoroutine(ShowMessageRoutine(message));
        }
    }

    private IEnumerator ShowMessageRoutine(string message)
    {
        tip.text = message;
        yield return new WaitForSeconds(MessageSeconds);
        tip.text = "";
    }
}
