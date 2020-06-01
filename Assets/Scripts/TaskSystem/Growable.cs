using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Interactions;
using Data;
namespace Task {
    public class Growable : ResourceProvider, Interactable
    {
        public GameObject[] positions;
        public float min_growth_time;
        public float max_growth_time;
        private float time_until_next_grow;

        // Start is called before the first frame update
        public override void Start()
        {
            base.Start();
            this.time_until_next_grow = Random.Range(min_growth_time, max_growth_time);
        }

        // Update is called once per frame
        void Update()
        {
            if (this.time_until_next_grow >= 0) {
                this.time_until_next_grow -= Time.deltaTime;
                if (this.time_until_next_grow < 0) {
                    int start = Random.Range(0, positions.Length);
                    for (int i = 0; i < positions.Length; i++) {
                        GameObject to_activate = positions[(i + start) % positions.Length];
                        if (!to_activate.activeSelf) {
                            to_activate.SetActive(true);
                            this.time_until_next_grow = Random.Range(min_growth_time, max_growth_time);
                            break;
                        }
                    }
                }
            }
        }

        public IEnumerator<bool> run_interaction(
            Inventory inventory,
            FlagSet flags,
            UIController controller
        ) {
            int start = Random.Range(0, positions.Length);
            for (int i = 0; i < positions.Length; i++) {
                GameObject to_deactivate = positions[(i + start) % positions.Length];
                if (to_deactivate.activeSelf) {
                    to_deactivate.SetActive(false);
                    this.time_until_next_grow = Random.Range(min_growth_time, max_growth_time);
                    inventory.add(resource, 1);
                    break;
                }
            }
            // Hook into non-immediate harvesting. Maybe an animation or something.
            yield break;
        }
    }
}