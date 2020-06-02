using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerOverlapDimmer : MonoBehaviour
{
    public SpriteRenderer[] to_dim;
    public float dim_amount;
    
    public void OnTriggerEnter2D(Collider2D col) {
        if (col.tag != "Player") return;
        if (col.isTrigger) return;
        foreach (var dim in to_dim) {
            dim.color = new Color(dim.color.r, dim.color.g, dim.color.b, dim_amount);
        }
    }

    public void OnTriggerExit2D(Collider2D col) {
        if (col.tag != "Player") return;
        if (col.isTrigger) return;
        foreach (var dim in to_dim) {
            dim.color = new Color(dim.color.r, dim.color.g, dim.color.b, 1);
        }
    }
}
