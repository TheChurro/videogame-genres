using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Data;

namespace Task.Ghost {
    public class TrackingGhost : MonoBehaviour
    {
        public const int Uninteractable = 8;

        public float abandon_time = 1.0f;
        public float move_time = 2.0f;
        private Vector2 move_param;
        private float noise_param;
        public enum TrackingType {
            Block,
            TrackResource,
            TrackGoal
        }
        public TrackingType track_type;
        private SpriteRenderer sprite_renderer;
        private PlayerController player_track;

        [SerializeField] private ItemInfo tracked_resource;
        [SerializeField] private int num_resource_in_inventory;
        [SerializeField] private Goal tracked_goal;

        private bool abandoned;
        private bool satisfied;
        private float abandon_time_left;

        void Awake() {
            this.sprite_renderer = GetComponent<SpriteRenderer>();
            this.abandoned = false;
            this.abandon_time_left = this.abandon_time;
        }

        private Vector2 target_pos;

        void Update() {
            // Abandon state. Trackers can be abandoned and they should
            // wait an amount of time before destroying themselves.
            // To show their abandonment, fade their sprite.
            if (abandoned || satisfied) {
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
            col.a = this.abandon_time_left / this.abandon_time;
            this.sprite_renderer.color = col;

            // Now we do the tracking part.
            ColliderDistance2D dist = new ColliderDistance2D{};
            foreach (var provider in this.providers) {
                var new_dist = provider.Item2.Distance(player_track.Collider);
                if (!dist.isValid || new_dist.distance < dist.distance) {
                    dist = new_dist;
                }
            }
            if (dist.isValid) {
                noise_param += 0.1f * Time.deltaTime;
                var x_noise = 0.5f - Mathf.PerlinNoise(this.transform.position.x, noise_param);
                var y_noise = 0.5f - Mathf.PerlinNoise(this.transform.position.y, noise_param);
                target_pos = new Vector2(x_noise, y_noise) + dist.pointA;
                this.transform.position = Vector2.SmoothDamp(
                    this.transform.position,
                    target_pos,
                    ref move_param,
                    move_time
                );
            }

            if (this.track_type == TrackingType.TrackResource && !abandoned) {
                int current_in_inventory = this.player_track.inventory[this.tracked_resource];
                if (this.num_resource_in_inventory < current_in_inventory) {
                    this.abandon();
                    GhostManager.Instance.primary_ghost.progress_made();
                }
                this.num_resource_in_inventory = current_in_inventory;
            }
        }

        private List<(ResourceProvider, Collider2D)> providers = new List<(ResourceProvider, Collider2D)>();
        public void providers_changed() {
            if (this.track_type == TrackingType.TrackGoal) return;
            if (this.track_type == TrackingType.Block) {
                foreach (var provider in providers) {
                    if (!GhostManager.Instance.nearby_providers.Contains(provider.Item1)) {
                        provider.Item1.gameObject.layer = 0;
                    }
                }
            }
            providers.Clear();
            foreach (var provider in GhostManager.Instance.nearby_providers) {
                if (provider.resource == this.tracked_resource) {
                    var collider = provider.GetComponent<Collider2D>();
                    if (collider == null) continue;
                    providers.Add((provider, collider));
                    if (this.track_type == TrackingType.Block) {
                        provider.gameObject.layer = Uninteractable;
                    }
                }
            }
            if (this.track_type == TrackingType.Block && this.providers.Count == 0) {
                this.abandon();
            }
        }

        public void track(PlayerController player, ItemInfo item_type) {
            this.track_type = TrackingType.TrackResource;
            this.tracked_resource = item_type;
            this.sprite_renderer.color = Color.green;
            this.player_track = player;
            this.num_resource_in_inventory = this.player_track.inventory[item_type];
            providers_changed();
        }

        public void block(PlayerController player, ItemInfo item_type) {
            this.track_type = TrackingType.Block;
            this.tracked_resource = item_type;
            this.sprite_renderer.color = Color.red;
            this.player_track = player;
            providers_changed();
        }

        public void track(PlayerController player, Goal goal) {
            this.track_type = TrackingType.TrackGoal;
            this.tracked_goal = goal;
            this.sprite_renderer.color = Color.yellow;
            this.player_track = player;
        }

        public bool is_abandoned() {
            return this.abandoned;
        }

        public void abandon() {
            abandoned = true;
            if (this.track_type == TrackingType.Block) {
                foreach (var provider in providers) {
                    provider.Item1.gameObject.layer = 0;
                }
            }
            providers.Clear();
        }

        public void recover() {
            abandoned = false;
            providers_changed();
            if (this.track_type == TrackingType.TrackResource) {
                this.num_resource_in_inventory = this.player_track.inventory[this.tracked_resource];
            }
        }

        public void satisfy() {
            satisfied = true;
            abandon();
        }

        public ItemInfo get_tracked_resource() {
            return this.tracked_resource;
        }

        void OnDrawGizmos() {
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(target_pos, 0.25f);
        }
    }
}
