using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Task.Ghost {
    public class Ghost : MonoBehaviour
    {
        public GhostInfo ghost_info;
        public float minutes_following_before_angry;
        public float minutes_waiting_before_angry;
        public float minutes_before_angrier;
        public float progress_before_subdued;

        public float move_time = 2.0f;
        public float min_rotate_speed = 0.25f;
        public float max_rotate_speed = 1f;

        private float following_anger_rate;
        private float angry_anger_rate;
        private float progress_subdue_amount;
        [SerializeField] private float angry_at;
        [SerializeField] private float anger_level;
        private Vector3 home_location;
        private float move_param;

        public bool satisfied { get => this.state == GhostState.Satisfied; }
        public bool guiding_tasks { get => this.state == GhostState.Following || this.state == GhostState.Angry; }
        public GhostState state;

        public enum GhostState {
            Waiting,
            Following,
            Angry,
            ReturnToGoal,
            Satisfied
        }

        public bool is_angry() {
            return this.state == GhostState.Angry;
        }

        void Awake() {
            this.angry_at = 60 * this.minutes_waiting_before_angry;
            this.following_anger_rate = this.angry_at / (60 * this.minutes_following_before_angry);
            this.progress_subdue_amount = this.angry_at / progress_before_subdued;
            this.angry_anger_rate = this.progress_subdue_amount / (60 * this.minutes_before_angrier);
            this.anger_level = 0;
        }

        void Start() {
            GhostManager.register(this);
            this.state = GhostState.Waiting;
            this.home_location = this.transform.position;
        }

        float get_target_dist() {
            return Mathf.Lerp(1.0f, 3.5f, this.anger_level / this.angry_at);
        }

        void Update() {
            if (state == GhostState.Waiting) return;
            if (state == GhostState.Following) {
                this.anger_level += Time.deltaTime * this.following_anger_rate;
                try_become_angry();
            } else {
                this.anger_level += Time.deltaTime * this.angry_anger_rate;
                try_subdue();
            }
            if (this.guiding_tasks) {
                var player = GhostManager.Instance.player.transform;
                var offset = (Vector2)(this.transform.position - player.position);
                var cur_dist = offset.magnitude;
                var target_dist = get_target_dist() + Random.Range(-0.25f, 0.25f);
                var new_dist = Mathf.SmoothDamp(cur_dist, target_dist, ref move_param, move_time);
                var new_offset = offset * new_dist / cur_dist;
                new_offset = Quaternion.AngleAxis(
                    Mathf.Lerp(
                        min_rotate_speed,
                        max_rotate_speed,
                        this.anger_level / this.angry_at
                    ),
                    Vector3.forward
                ) * new_offset;
                this.transform.position = (Vector2)player.position + new_offset;
            } else {

            }
        }

        public void providers_changed() {

        }

        private void try_become_angry() {
            if (this.anger_level < this.angry_at || this.state == GhostState.Angry) {
                return;
            }
            this.state = GhostState.Angry;
        }

        private void try_subdue() {
            if (this.anger_level >= this.progress_subdue_amount || this.state != GhostState.Angry) {
                return;
            }
            this.anger_level = 0;
            this.state = GhostState.Following;
        }

        public void become_primary(float time) {
            this.state = GhostState.Following;
            anger_level = time;
        }

        public void release_primary() {
            this.state = GhostState.Waiting;
        }
    }
}
