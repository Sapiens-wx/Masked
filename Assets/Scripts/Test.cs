using UnityEngine;
using UnityEngine.InputSystem;

public class Test : MonoBehaviour
{
    public Rigidbody2D target;
    void FixedUpdate()
    {
        Vector2 worldPos=Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        Debug.Log(Mouse.current.position.ReadValue());
        target.position=worldPos;
    }
}