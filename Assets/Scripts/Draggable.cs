using UnityEngine;
using System.Collections.Generic;
using System;

public class Draggable : MonoBehaviour
{
    [HideInInspector] [NonSerialized] public Rigidbody2D rgb;
    [HideInInspector] [NonSerialized] public MaskObj mask;
    void Awake() {
        rgb=GetComponent<Rigidbody2D>();
    }
    void Start() {
        DraggableManager.inst.RegisterDraggable(this);
    }
}