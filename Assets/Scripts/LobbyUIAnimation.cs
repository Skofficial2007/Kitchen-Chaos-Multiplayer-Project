using UnityEngine;

public class LobbyUIAnimation : MonoBehaviour
{
    private const string CREATE_LOBBY_UI_IS_ACTIVE = "CreateLobbyUi_IsActive";

    [SerializeField] private Animator animator;

    private void OnEnable()
    {
        LobbyCreateUI.OnCreateLobbyUIActiveChanged += HandleLobbyCreateUIActiveChanged;
    }

    private void OnDisable()
    {
        LobbyCreateUI.OnCreateLobbyUIActiveChanged -= HandleLobbyCreateUIActiveChanged;
    }

    private void HandleLobbyCreateUIActiveChanged(bool isActive)
    {
        animator.SetBool(CREATE_LOBBY_UI_IS_ACTIVE, isActive);
    }
}
