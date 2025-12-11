using UnityEngine;

/// <summary>
/// Skroty developerskie
/// </summary>
public class ControllsTest : MonoBehaviour
{
    public UIManager ui;
    private bool isDevModeActive = false;

    void Update()
    {
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.Z))
        {
            isDevModeActive = !isDevModeActive;
        }

        if (!isDevModeActive) return;

        if (Input.GetKeyDown(KeyCode.L))
        {
            ui.DamagePlayer(1, 2);
        }

        if (Input.GetKeyDown(KeyCode.P))
        {
            ui.DamagePlayer(2, 2);
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            ui.ResetGameUI();
        }
    }
}