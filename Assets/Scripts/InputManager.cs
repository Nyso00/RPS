using UnityEngine;

public class InputManager : Singleton<InputManager>
{
    bool inputAvailable = false;

    // Update is called once per frame
    void Update()
    {
        if (!inputAvailable)
            return;
        
        // Handle player input here (e.g., using Input.GetKeyDown for testing)
    }

    public void SetInputAvailable(bool available)
    {
        inputAvailable = available;
    }
}
