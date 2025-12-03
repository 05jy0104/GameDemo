using UnityEngine;

public class CollisionTest : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"触发器进入: {other.name} (Tag: {other.tag})");
    }

    void OnCollisionEnter(Collision collision)
    {
        Debug.Log($"碰撞检测到: {collision.gameObject.name} (Tag: {collision.gameObject.tag})");
    }
}