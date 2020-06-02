using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Data;

namespace Task.Ghost {
    public class TrackingGhost : MonoBehaviour, FlagUpdateReciever
    {
        public const int Uninteractable = 8;

        public float search_ring_radius = 3.5f;
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
        [SerializeField] private string tracked_goal;

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
                    this.player_track.RemoveFlagChangeHandler(this);
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
            foreach (var trackable in this.trackables) {
                var new_dist = trackable.Distance(player_track.Collider);
                if (!dist.isValid || new_dist.distance < dist.distance) {
                    dist = new_dist;
                }
            }
            if (dist.isValid) {
                noise_param += 0.1f * Time.deltaTime;
                var x_noise = 0.5f - Mathf.PerlinNoise(this.transform.position.x, noise_param);
                var y_noise = 0.5f - Mathf.PerlinNoise(this.transform.position.y, noise_param);
                target_pos = new Vector2(x_noise, y_noise) + dist.pointA;

                // If we aren't block, then we should place the tracker pointing in the
                // direction of the target if we are too far away.
                if (this.track_type != TrackingType.Block) {
                    var offset = target_pos - (Vector2) this.player_track.transform.position;
                    if (offset.magnitude > search_ring_radius) {
                        target_pos = (Vector2) this.player_track.transform.position + offset.normalized * search_ring_radius;
                    }
                }

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

        private List<Collider2D> trackables = new List<Collider2D>();
        public void providers_changed() {
            if (this.track_type != TrackingType.Block) return;
            foreach (var provider in trackables) {
                provider.gameObject.layer = 0;
            }
            trackables.Clear();
            foreach (var provider in GhostManager.Instance.nearby_providers) {
                if (provider.resource == this.tracked_resource) {
                    var collider = provider.GetComponent<Collider2D>();
                    if (collider == null) continue;
                    trackables.Add(collider);
                    provider.gameObject.layer = Uninteractable;
                }
            }
            if (this.trackables.Count == 0) {
                this.abandon();
            }
        }

        public void track(PlayerController player, ItemInfo item_type) {
            this.track_type = TrackingType.TrackResource;
            this.tracked_resource = item_type;
            this.sprite_renderer.color = Color.green;
            this.player_track = player;
            this.num_resource_in_inventory = this.player_track.inventory[item_type];
            foreach (var provider in ResourceManager.Instance.those_providing(item_type)) {
                var collider = provider.GetComponent<Collider2D>();
                if (collider != null) {
                    this.trackables.Add(collider);
                }
            }
        }

        public void block(PlayerController player, ItemInfo item_type) {
            this.track_type = TrackingType.Block;
            this.tracked_resource = item_type;
            this.sprite_renderer.color = Color.red;
            this.player_track = player;
            providers_changed();
        }

        public void track(PlayerController player, string flag) {
            this.track_type = TrackingType.TrackGoal;
            this.tracked_goal = flag;
            this.sprite_renderer.color = Color.yellow;
            this.player_track = player;
            this.player_track.AddFlagChangeHandler(this);
            this.trackables.Clear();
            foreach (var goal in GoalManager.Instance.for_flag(flag)) {
                var collider = goal.GetComponent<Collider2D>();
                if (collider != null) {
                    this.trackables.Add(collider);
                }
            }
        }

        public bool is_abandoned() {
            return this.abandoned;
        }

        public void abandon() {
            abandoned = true;
            if (this.track_type == TrackingType.Block) {
                foreach (var trackable in trackables) {
                    trackable.gameObject.layer = 0;
                }
                trackables.Clear();
            }
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

        public string get_tracked_flag() {
            return this.tracked_goal;
        }

        void OnDrawGizmos() {
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(target_pos, 0.25f);
        }

        public void flag_set(string flag) {
            if (flag == this.tracked_goal) {
                this.abandon();
            }
        }

        public void flag_unset(string flag) {
            if (!satisfied && flag == this.tracked_goal) {
                this.recover();
            }
        }
    }
}
