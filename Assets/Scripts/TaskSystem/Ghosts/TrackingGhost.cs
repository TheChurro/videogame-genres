using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Data;

namespace Task.Ghost {
    public class TrackingGhost : MonoBehaviour
    {
        public float abandon_time = 1.0f;
        public enum TrackingType {
            Block,
            TrackResource,
            TrackGoal
        }
        public TrackingType track_type;
        private SpriteRenderer sprite_renderer;
        private PlayerController player_track;

        private ItemInfo tracked_resource;
        private Goal tracked_goal;

        private bool abandoned;
        private float abandon_time_left;

        void Start() {
            this.sprite_renderer = GetComponent<SpriteRenderer>();
            this.abandoned = false;
            this.abandon_time_left = this.abandon_time;
        }

        void Update() {
            // Abandon state. Trackers can be abandoned and they should
            // wait an amount of time before destroying themselves.
            // To show their abandonment, fade their sprite.
            if (abandoned) {
                this.abandon_time_left -= Time.deltaTime;
                if (this.abandon_time_left <= 0) {
                    Destroy(this.gameObject);
                }
            } else {
                this.abandon_time_left += Time.deltaTime;
                this.abandon_time_left = Mathf.Min(this.abandon_time_left, this.abandon_time);
            }
            // Update color based on abandon time.
            var col = this.sprite_renderer.color;
            col.a = 1 - (this.abandon_time_left / this.abandon_time);
            this.sprite_renderer.color = col;

            // Now we do the tracking part.
        }

        public void providers_changed() {

        }

        public void track(PlayerController player, ItemInfo item_type) {
            this.track_type = TrackingType.TrackResource;
            this.sprite_renderer.color = Color.green;
            this.player_track = player;
        }

        public void block(PlayerController player, ItemInfo item_type) {
            this.track_type = TrackingType.Block;
            this.sprite_renderer.color = Color.red;
            this.player_track = player;
        }

        public void track(PlayerController player, Goal goal) {
            this.track_type = TrackingType.TrackGoal;
            this.sprite_renderer.color = Color.yellow;
            this.player_track = player;
        }

        public void abandon() {
            abandoned = true;
        }

        public void recover() {
            abandoned = false;
        }
    }
}
