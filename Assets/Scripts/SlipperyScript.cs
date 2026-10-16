using UnityEngine;

public class SlipperyScript : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        PlayerControls player = other.GetComponent<PlayerControls>();

        if (player != null)
        {
            player.SetSlippery(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerControls player = other.GetComponent<PlayerControls>();

        if (player != null)
        {
            player.SetSlippery(false);
        }
    }
}