using UnityEngine;

public class LinkHandler : MonoBehaviour
{
    [SerializeField] private string customURL = "https://github.com/LaLlorona/GameJam";

    public void OpenExternalLink()
    {
        // Opens the web browser to the specified address
        Application.OpenURL(customURL);
    }
}
