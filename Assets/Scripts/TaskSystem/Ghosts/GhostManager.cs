using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Data;

namespace Task.Ghost {
    public class GhostManager : MonoBehaviour, FlagUpdateReciever
    {
        private static List<Ghost> ghosts = new List<Ghost>();
        public static void register(Ghost ghost) {
            ghosts.Add(ghost);
        }
        private static GhostManager g_manager;
        public static GhostManager Instance {
            get => g_manager;
        }

        public PlayerController player;
        public GameObject tracking_ghost_prefab;
        public float ghost_interest_rate = 0.05f;
        public float initial_interest_radius = 2.0f;

        public HashSet<ResourceProvider> nearby_providers;
        private float time_since_last_player_restore;

        public Ghost primary_ghost;
        public List<TrackingGhost> tracking_ghosts;
        private float interest_radius;

        void Awake() {
            g_manager = this;
            this.primary_ghost = null;
            this.nearby_providers = new HashSet<ResourceProvider>();
            this.time_since_last_player_restore = 0;
        }

        void Start() {
            this.player.AddFlagChangeHandler(this);
        }

        void Update() {
            this.transform.position = player.transform.position;
            if (this.primary_ghost == null) {
                this.time_since_last_player_restore += Time.deltaTime;
                interest_radius = initial_interest_radius + time_since_last_player_restore * ghost_interest_rate;
                float thresholdDistance = interest_radius * interest_radius;
                foreach (var ghost in ghosts) {
                    if (ghost.satisfied)
                        continue;
                    float sqrDist = (ghost.transform.position - this.transform.position).sqrMagnitude;
                    if (sqrDist < thresholdDistance) {
                        set_primary(ghost);
                        break;
                    }
                }
            } else {

            }
        }

        private bool set_primary(Ghost ghost) {
            if (this.primary_ghost != null) {
                this.primary_ghost.release_primary();
                foreach (var tracking_ghost in this.tracking_ghosts) {
                    tracking_ghost.abandon();
                }
            }
            this.primary_ghost = ghost;
            if (ghost != null) {
                if (ghost.become_primary(this.time_since_last_player_restore)) {
                    return true;
                }
                this.primary_ghost = null;
            }
            return false;
        }

        public bool try_take_primary(Ghost ghost) {
            if (this.primary_ghost == null) {
                set_primary(ghost);
                return true;
            }
            if (this.primary_ghost.is_angry()) {
                return false;
            }
            set_primary(ghost);
            return true;
        }

        void providers_changed() {
            if (this.primary_ghost != null) {
                this.primary_ghost.providers_changed();
            }
            this.tracking_ghosts.RemoveAll(x => x == null);
            this.tracking_ghosts.ForEach(x => x.providers_changed());
        }

        public void abandon_all_trackers(Ghost g) {
            if (g != this.primary_ghost) return;
            foreach (var tracking_ghost in this.tracking_ghosts) {
                tracking_ghost.abandon();
            }
        }

        public void satify_ghost(Ghost g) {
            if (g != this.primary_ghost) return;
            foreach (var tracker in this.tracking_ghosts) {
                tracker.satisfy();
            }
            set_primary(null);
        }

        void OnTriggerEnter2D(Collider2D collider) {
            var provider = collider.GetComponent<ResourceProvider>();
            if (provider != null) {
                this.nearby_providers.Add(provider);
                providers_changed();
            }
        }

        void OnTriggerExit2D(Collider2D collider) {
            var provider = collider.GetComponent<ResourceProvider>();
            if (provider != null) {
                this.nearby_providers.Remove(provider);
                providers_changed();
            }
        }

        public void track(Ghost g, Vector3 pos, ItemInfo item) {
            if (g != primary_ghost) return;
            // Avoid double tracking!
            foreach (var tracker in this.tracking_ghosts) {
                if (tracker == null) continue;
                if (tracker.track_type == TrackingGhost.TrackingType.TrackResource) {
                    if (tracker.get_tracked_resource() == item) {
                        if (tracker.is_abandoned()) {
                            tracker.recover();
                        }
                        return;
                    }
                }
            }

            var go = Instantiate(this.tracking_ghost_prefab, pos, Quaternion.identity);
            if (!go.TryGetComponent(out TrackingGhost new_tracker)) {
                Destroy(go);
                return;
            }
            new_tracker.track(this.player, item);
            this.tracking_ghosts.Add(new_tracker);
        }

        public void block(Ghost g, Vector3 pos, ItemInfo item) {
            if (g != primary_ghost) return;
            // Avoid double blocking!
            foreach (var tracker in this.tracking_ghosts) {
                if (tracker == null) continue;
                if (tracker.track_type == TrackingGhost.TrackingType.Block) {
                    if (tracker.get_tracked_resource() == item) {
                        if (tracker.is_abandoned()) {
                            tracker.recover();
                        }
                        return;
                    }
                }
            }

            var go = Instantiate(this.tracking_ghost_prefab, pos, Quaternion.identity);
            if (!go.TryGetComponent(out TrackingGhost new_tracker)) {
                Destroy(go);
                return;
            }
            new_tracker.block(this.player, item);
            this.tracking_ghosts.Add(new_tracker);
        }

        public void flag_set(string flag) {
            if (flag.ToLower().StartsWith("restore")) {
                this.time_since_last_player_restore = 0;
            }
        }
        public void flag_unset(string flag) {}

        // Debug Drawing
        public void OnDrawGizmos() {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(this.transform.position, this.interest_radius);
        }
    }
}