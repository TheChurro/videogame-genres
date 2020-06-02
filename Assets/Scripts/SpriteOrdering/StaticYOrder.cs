using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SOTU {
    [RequireComponent(typeof(SpriteRenderer))]
    public class StaticYOrder : MonoBehaviour
    {
        public SpriteRenderer sprite_renderer;
        // Start is called before the first frame update
        void Start()
        {
            sprite_renderer = GetComponent<SpriteRenderer>();
            UpdateSortingOrder();
        }

        protected void UpdateSortingOrder() {
            sprite_renderer.sortingOrder = -Mathf.FloorToInt(transform.position.y);
            Vector3 pos = transform.position;
            pos.z = (pos.y + sprite_renderer.sortingOrder);
            transform.position = pos;

            foreach (Transform child in this.transform) {
                var parent_y_order = child.GetComponent<ParentYOrder>();
                if (parent_y_order != null) {
                    parent_y_order.UpdateSortingOrder();
                }
            }
        }
    }
}