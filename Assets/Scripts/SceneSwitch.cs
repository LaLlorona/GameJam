using UnityEngine;
using UnityEngine.SceneManagement; // 1. Required for scene handling

public class SceneSwitcher : MonoBehaviour
{
    // 2. Pass the exact name of the scene you want to load
    public void LoadTargetScene(string SampleScene)
    {
        SceneManager.LoadScene(SampleScene); // 3. Load the specified scene
    }
}
