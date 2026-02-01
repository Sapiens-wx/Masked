using UnityEngine;

public class Singleton<T>:MonoBehaviour where T : MonoBehaviour
{
    public static T inst;
    protected virtual void Awake()
    {
        inst=this as T;
    }
}