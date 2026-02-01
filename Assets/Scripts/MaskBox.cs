using UnityEngine;
using DG.Tweening;

public class MaskBox : Singleton<MaskBox>
{
    public MaskObj mask;
    public Vector3 spawnOffset;
    public float spawnInterval = 1f;
    public float spawnForce;
    BoxCollider2D bc;
    float lastSpawned=-100f;
    void OnDrawGizmosSelected() {
        Gizmos.color=Color.green;
        Gizmos.DrawWireSphere(transform.position+spawnOffset, .5f);
    }
    protected override void Awake()
    {
        base.Awake();
        bc=GetComponent<BoxCollider2D>();
    }
    void Update()
    {
        if (Input.GetMouseButtonDown(0)&&Time.time-lastSpawned>spawnInterval)
        {
            Vector2 mouseWorldPos=Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Bounds bd=bc.bounds;
            if (mouseWorldPos.x >= bd.min.x && mouseWorldPos.x <= bd.max.x && mouseWorldPos.y >= bd.min.y && mouseWorldPos.y <= bd.max.y)
            {
                lastSpawned=Time.time;
                transform.DOScale(transform.localScale*1.3f, .1f).SetLoops(2, LoopType.Yoyo);
                CreateMask();
            }
        }
    }
    public MaskObj CreateMask()
    {
        MaskObj newMask=Instantiate(mask, transform.position+spawnOffset, Quaternion.identity).GetComponent<MaskObj>();
        newMask.lastDrag=Time.time;
        newMask.leftAnchor.GetComponent<Rigidbody2D>().AddForce(new Vector3(0,spawnForce,0));
        newMask.rightAnchor.GetComponent<Rigidbody2D>().AddForce(new Vector3(0,spawnForce,0));
        return newMask;
    }
}