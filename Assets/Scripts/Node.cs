using UnityEngine;

public class Node : MonoBehaviour
{
    public Rigidbody rb;

    void Awake()
    {
        if (!rb) rb = GetComponent<Rigidbody>();
    }
}