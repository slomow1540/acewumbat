using UnityEngine;
using System.Collections.Generic;

public class DestroyWhenPartsDead : MonoBehaviour
{
    public List<GameObject> parts = new List<GameObject>();

    public GameObject explosionPrefab;

    bool dead = false;

    void Update()
    {
        if (dead) return;

        bool allDead = true;

        foreach (GameObject p in parts)
        {
            if (p != null)
            {
                allDead = false;
                break;
            }
        }

        if (allDead)
        {
            Die();
        }
    }

    void Die()
    {
        dead = true;

        if (explosionPrefab != null)
        {
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        }

        Destroy(gameObject);
    }
}