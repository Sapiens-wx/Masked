using UnityEngine;
using System.Collections.Generic;

public class DraggableManager : Singleton<DraggableManager>
{
    public float maxPickDistance = 1f;
    public Joint2D mousePosJoint;

    List<Draggable> draggables;
    Draggable draggingObj;
    bool dragging=false;
    protected override void Awake() {
        base.Awake();
        draggables=new List<Draggable>();
    }
    public void RegisterDraggable(Draggable draggable) {
        draggables.Add(draggable);
    }
    void Update()
    {
        if (Input.GetMouseButtonDown(0)) 
            OnBeginDrag();
        else if(Input.GetMouseButtonUp(0))
            OnEndDrag();
        OnDrag();
    }
    void OnBeginDrag() {
        if(dragging) return;
        Vector2 mousePos=GetMouseWorldPosition();
        Draggable draggable=FindClosestDraggable(mousePos);
        if (draggable != null) {
            dragging=true;
            draggingObj=draggable;
            Debug.Log($"Drag {draggingObj.name}");
            Debug.Log($"mask {draggingObj.mask.name}");
            if(draggable.mask.isSleeping)
                draggable.mask.WakeUp();
            mousePosJoint.attachedRigidbody.position=mousePos;
            mousePosJoint.connectedBody=draggable.rgb;
        }
    }
    void OnDrag() {
        if(!dragging) return;
        mousePosJoint.attachedRigidbody.position=GetMouseWorldPosition();
        draggingObj.mask.lastDrag=Time.time;
    }
    void OnEndDrag() {
        if(!dragging) return;
        dragging=false;
        mousePosJoint.connectedBody=null;
        draggingObj.rgb.linearVelocity=Vector2.zero;
        draggingObj=null;
    }
    #region dragging
    private Draggable FindClosestDraggable(Vector3 position)
    {
        Draggable closest = null;
        float minSqrDist = maxPickDistance * maxPickDistance;

        foreach (Draggable draggable in draggables)
        {
            if(draggable.mask.endGameDraggable==false&&GameManager.inst!=null&&GameManager.inst.gameEnded) continue;
            if(draggable.mask.isSleeping) continue;
            float sqrDist =
                (draggable.transform.position - position).sqrMagnitude;

            if (sqrDist < minSqrDist )
            {
                minSqrDist = sqrDist;
                closest = draggable;
            }
        }

        if (closest == null)// try to find a sleeping one
        {
            foreach (Draggable draggable in draggables)
            {
                if(draggable.mask.endGameDraggable==false&&GameManager.inst!=null&&GameManager.inst.gameEnded) continue;
                float sqrDist =
                    (draggable.transform.position - position).sqrMagnitude;

                if (sqrDist < minSqrDist )
                {
                    minSqrDist = sqrDist;
                    closest = draggable;
                }
            }
        }

        return closest;
    }
    private Vector3 GetMouseWorldPosition()
    {
        Vector2 screenPos = Input.mousePosition;
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(screenPos);
        worldPos.z = 0f;
        return worldPos;
    }
    #endregion
}