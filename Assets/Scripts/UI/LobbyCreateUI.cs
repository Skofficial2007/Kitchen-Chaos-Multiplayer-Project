using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyCreateUI : MonoBehaviour
{
    // Event to indicate whether the Create Lobby UI is active or not
    public static event Action<bool> OnCreateLobbyUIActiveChanged;

    [SerializeField] private Button closeButton;
    [SerializeField] private Button createPrivateButton;
    [SerializeField] private Button createPublicButton;
    [SerializeField] private TMP_InputField lobbyNameInputField;

    // Reference to the animation controller for this UI.
    [SerializeField] private CreateLobbyUIAnimation createLobbyUIAnimation;

    private void Awake()
    {
        createPublicButton.onClick.AddListener(() =>
        {
            KitchenGameLobby.Instance.CreateLobby(lobbyNameInputField.text, false);
        });
        createPrivateButton.onClick.AddListener(() =>
        {
            KitchenGameLobby.Instance.CreateLobby(lobbyNameInputField.text, true);
        });
        closeButton.onClick.AddListener(() =>
        {
            Hide();
        });
    }

    private void Start()
    {
        // Initially ensure the UI is hidden.
        gameObject.SetActive(false);
    }

    public void Show()
    {
        // Activate the UI and immediately play the PopOut animation.
        gameObject.SetActive(true);
        createLobbyUIAnimation.PlayPopOutAnimation();
        OnCreateLobbyUIActiveChanged?.Invoke(true);
    }

    public void Hide()
    {
        // Instead of instantly hiding, play the PopIn animation.
        createLobbyUIAnimation.PlayPopInAnimation();
        OnCreateLobbyUIActiveChanged?.Invoke(false);
    }
}
