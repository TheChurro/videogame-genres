using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SOTU {
    [RequireComponent(typeof(SpriteRenderer))]
    public class ParentYOrder : MonoBehaviour
    {
        public SpriteRenderer sprite_renderer;

        public void UpdateSortingOrder() {
            if (sprite_renderer == null) {
                sprite_renderer = GetComponent<SpriteRenderer>();
            }
            if (this.transform.parent != null) {
                var parent_renderer = this.transform.parent.GetComponent<SpriteRenderer>();
                sprite_renderer.sortingOrder = parent_renderer.sortingOrder;
                Vector3 pos = transform.position;
                pos.z = 0;
                transform.position = pos;
            }
        }
    }
}