using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public class MaskObj : MonoBehaviour
{
    public bool endGameDraggable=false;
    [Header("Anchors")]
    public Transform leftAnchor;
    public Transform rightAnchor;

    [Header("Joints")]
    public RelativeJoint2D leftJoint;
    public RelativeJoint2D rightJoint;

    [Header("Settings")]
    public int jointNumber = 3;
    [Header("Line Rendering")]
    public Line leftLine;
    public Line middleLine, rightLine;

    List<Draggable> draggables;
    [HideInInspector][NonSerialized] public bool isSleeping=false;
    [HideInInspector][NonSerialized] public float lastDrag=0;

    private void Start()
    {
        BuildJointChain();
        trySleepCoro=StartCoroutine(TryToSleep());
        // set draggable.mask=this
        foreach(Draggable draggable in draggables) {
            draggable.mask=this;
        }
    }
    #region sleep
    Coroutine trySleepCoro;
    IEnumerator TryToSleep() {
        WaitForSeconds wait=new WaitForSeconds(UnityEngine.Random.Range(2f,3f));
        const float SPD_THRESHOLD=.1f;
        const float DOWN_TIME=10f;
        while (true) {
            yield return wait;
            bool canSleep=true;
            foreach(Draggable draggable in draggables)
            {
                // if the rope is still moving AND
                // if the time elapsed since the last drag > DOWN_TIME, it cannot sleep
                float timeElapsedSinceLastDrag=Time.time-lastDrag;
                if (draggable.rgb.linearVelocity.sqrMagnitude > SPD_THRESHOLD && 
                    timeElapsedSinceLastDrag<DOWN_TIME)
                    canSleep=false;
                // cannot sleep when the player is dragging
                if(timeElapsedSinceLastDrag<.5f)
                    canSleep=false;
            }
            if(canSleep)
                Sleep();
        }
    }
    public void Sleep() {
        if(isSleeping) return;
        Debug.Log($"{gameObject.name} goes to sleep");
        isSleeping=true;
        if(trySleepCoro!=null)
            StopCoroutine(trySleepCoro);
        trySleepCoro=null;
        foreach(Draggable draggable in draggables) {
            draggable.rgb.bodyType=RigidbodyType2D.Static;
        }
    }
    public void WakeUp() {
        if(!isSleeping) return;
        isSleeping=false;
        trySleepCoro = StartCoroutine(TryToSleep());
        foreach(Draggable draggable in draggables) {
            draggable.rgb.bodyType=RigidbodyType2D.Dynamic;
        }
    }
    #endregion

    // =========================
    // Main Logic
    // =========================

    private void BuildJointChain()
    {
        if (!IsValid())
            return;

        RelativeJoint2D[] chain = CreateMiddleJoints();
        ConnectJointChain(chain);
        InitMiddleLine(chain);
        // add all draggables to this object
        draggables=new List<Draggable>(chain.Length+2);
        draggables.Add(leftAnchor.GetComponent<Draggable>());
        draggables.Add(leftJoint.GetComponent<Draggable>());
        foreach(RelativeJoint2D e in chain) {
            draggables.Add(e.GetComponent<Draggable>());
        }
        draggables.Add(rightJoint.GetComponent<Draggable>());
        draggables.Add(rightAnchor.GetComponent<Draggable>());
    }

    private void InitMiddleLine(RelativeJoint2D[] chain)
    {
        Transform[] anchors=new Transform[chain.Length+2];
        anchors[0]=leftJoint.attachedRigidbody.transform;
        for(int i = 0; i < chain.Length; ++i)
        {
            anchors[i+1]=chain[i].attachedRigidbody.transform;
        }
        anchors[chain.Length+1]=rightJoint.attachedRigidbody.transform;
        middleLine.SetAnchors(anchors);
    }

    // =========================
    // Validation
    // =========================

    private bool IsValid()
    {
        return leftJoint != null &&
               rightJoint != null &&
               leftAnchor != null &&
               rightAnchor != null &&
               jointNumber >= 0;
    }

    // =========================
    // Joint Creation
    // =========================

    private RelativeJoint2D[] CreateMiddleJoints()
    {
        RelativeJoint2D[] result = new RelativeJoint2D[jointNumber];

        Vector3 start = leftJoint.attachedRigidbody.transform.position;
        Vector3 end = rightJoint.attachedRigidbody.transform.position;

        for (int i = 0; i < jointNumber; i++)
        {
            float t = (i + 1f) / (jointNumber + 1f);
            Vector3 pos = Vector3.Lerp(start, end, t);

            GameObject jointObj = Instantiate(
                rightJoint.gameObject,
                pos,
                Quaternion.identity,
                transform
            );
            jointObj.transform.position=pos;

            result[i] = jointObj.GetComponent<RelativeJoint2D>();
        }

        return result;
    }

    // =========================
    // Chain Connection
    // =========================

    private void ConnectJointChain(RelativeJoint2D[] chain)
    {
        Rigidbody2D prevBody = leftJoint.GetComponent<Rigidbody2D>();

        // Left → middle joints
        for (int i = 0; i < chain.Length; i++)
        {
            chain[i].connectedBody = prevBody;
            prevBody = chain[i].GetComponent<Rigidbody2D>();
        }

        // Last → right
        rightJoint.connectedBody = prevBody;
    }
}