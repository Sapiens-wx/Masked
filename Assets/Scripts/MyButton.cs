using UnityEngine;

public class MyButton : MonoBehaviour
{
    public LayerMask anchorLayer;
    public Vector2 p1,p2;
    public float detectRadius=.3f;

    public System.Action onClick;
    bool clicked=false;
    void OnDrawGizmosSelected()
    {
        Gizmos.color=Color.green;
        Gizmos.DrawWireSphere(p1+(Vector2)transform.position, detectRadius);
        Gizmos.DrawWireSphere(p2+(Vector2)transform.position, detectRadius);
    }
    void FixedUpdate()
    {
        Vector2 offset=transform.position;
        Collider2D cd1=Physics2D.OverlapCircle(p1+offset, detectRadius, anchorLayer);
        Collider2D cd2=Physics2D.OverlapCircle(p2+offset, detectRadius, anchorLayer);
        if (cd1 && cd2 && clicked==false)
        {
            Draggable draggable=cd1.GetComponent<Draggable>();
            if (Time.time - draggable.mask.lastDrag > .1f)
                OnClick();
        }
        else
        {
            clicked=false;
        }
    }
    void OnClick()
    {
        if(clicked) return;
        clicked=true;
        onClick?.Invoke();
    }
}