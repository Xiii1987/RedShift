using UnityEngine;

public class RedshiftPlayerStateController : MonoBehaviour
{
    public static RedshiftPlayerStateController Instance { get; private set; }

    public bool CanMove { get; private set; } = true;
    public bool CanLook { get; private set; } = true;
    public bool CanInteract { get; private set; } = true;
	public bool IsInUI { get; private set; }

    private void Awake()
    {
        Instance = this;
        EnterGameplay();
    }

public void EnterGameplay()
{
    IsInUI = false;

    CanMove = true;
    CanLook = true;
    CanInteract = true;

    Cursor.lockState = CursorLockMode.Locked;
    Cursor.visible = false;
}

public void EnterUI()
{
    IsInUI = true;

    CanMove = false;
    CanLook = false;
    CanInteract = false;

    Cursor.lockState = CursorLockMode.None;
    Cursor.visible = true;
}
}