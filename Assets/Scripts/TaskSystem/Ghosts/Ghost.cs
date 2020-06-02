using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using BlueGraph;
using Interactions;

using Data;

namespace Task.Ghost {
    [IncludeModules("Ghost")]
    public class Ghost : InteractionsGraph, Interactable, FlagUpdateReciever
    {
        public GhostInfo ghost_info;
        public float minutes_following_before_angry;
        public float minutes_waiting_before_angry;
        public float minutes_before_angrier;
        public float progress_before_subdued;

        public float move_time = 1.0f;
        public float min_rotate_speed = 0.25f;
        public float max_rotate_speed = 1f;

        public float max_follow_radius = 3.5f;
        public float min_follow_radius = 1f;

        public float insistence = 1f;

        private float following_anger_rate;
        private float angry_anger_rate;
        private float progress_subdue_amount;
        private float angry_at;
        private float anger_level;
        private Vector2 home_location;
        private Vector2 move_param;

        public bool satisfied { get => this.state == GhostState.Satisfied; }
        public bool guiding_task { get => this.state == GhostState.Following || this.state == GhostState.Angry; }
        private GhostState state;
        private bool has_become_primary;

        public enum GhostState {
            Waiting,
            Following,
            Angry,
            GoalsCompleted,
            Satisfied
        }

        #region Utilities

        public bool is_angry() {
            return this.state == GhostState.Angry;
        }

        float get_target_dist() {
            return Mathf.Lerp(max_follow_radius, min_follow_radius, this.anger_level / this.angry_at);
        }

        private float noise_time;
        private float noise;

        float next_noise(float min, float max) {
            noise_time += Time.deltaTime;
            noise = Mathf.PerlinNoise(noise_time, noise);
            return min + (max - min) * noise;
        }

        private void register_trackers() {
            if (this.items_to_track != null) {
                foreach (var item in this.items_to_track) {
                    if (item.providers.Count > 0) {
                        GhostManager.Instance.track(this, this.transform.position, item.base_item);
                    }
                }
            }

            if (this.ghost_info != null) {
                var player = GhostManager.Instance.player;
                foreach (var flag in this.ghost_info.flag_requirements) {
                    if (!player.flags[flag])
                        GhostManager.Instance.track(this, this.transform.position, flag);
                }
            }
        }

        #endregion

        #region Unity Events

        public override void Awake() {
            this.angry_at = 60 * this.minutes_waiting_before_angry;
            this.following_anger_rate = this.angry_at / (60 * this.minutes_following_before_angry);
            this.progress_subdue_amount = this.angry_at / progress_before_subdued;
            this.angry_anger_rate = this.progress_subdue_amount / (60 * this.minutes_before_angrier);
            this.anger_level = 0;
            this.has_become_primary = false;
        }

        void Start() {
            GhostManager.register(this);
            GhostManager.Instance.player.AddFlagChangeHandler(this);
            this.state = GhostState.Waiting;
            if (this.ghost_info != null) {
                var flags = GhostManager.Instance.player.flags;
                if (flags[$"satisfied:{this.ghost_info.name}"]) {
                    this.state = GhostState.Satisfied;
                }
                if (flags[$"was_primary:{this.ghost_info.name}"]) {
                    this.has_become_primary = true;
                }
                if (flags[$"primary:{this.ghost_info.name}"]) {
                    GhostManager.Instance.try_take_primary(this);
                }
            }
            this.home_location = this.transform.position;
        }

        Vector2 wander_around(Vector2 position, float radius) {
            var angle = next_noise(0, 2 * Mathf.PI);
            var dist = next_noise(0, radius);
            position = position + dist * new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            return position;
        }

        private bool has_item_requirement = false;
        void Update() {
            if (state == GhostState.Following) {
                this.anger_level += Time.deltaTime * this.following_anger_rate;
                try_become_angry();
            } else if (state == GhostState.Angry) {
                this.anger_level += Time.deltaTime * this.angry_anger_rate;
                try_subdue();
            }

            var player = GhostManager.Instance.player;

            // Check to see if we accomplished our goal of getting and item and try to
            // enter the "Goals Completed" state in response.
            if (this.guiding_task) {
                if (this.ghost_info != null && this.ghost_info.item_requirement != null) {
                    if (GhostManager.Instance.player.inventory[this.ghost_info.item_requirement] > 0) {
                        if (!has_item_requirement) {
                            Debug.Log("Got target!");
                            try_enter_goals_completed();
                        }
                        has_item_requirement = true;
                        return;
                    } else {
                        if (has_item_requirement) {
                            Debug.Log("0 of target!");
                        }
                        has_item_requirement = false;
                    }
                }
            }

            Vector2 target_offset = Vector2.zero;
            if (this.guiding_task) {
                var offset = (Vector2)(this.transform.position - player.transform.position);
                var target_dist = get_target_dist() + next_noise(-0.1f, 0.1f);
                var new_offset = offset.normalized * target_dist;
                target_offset = Quaternion.AngleAxis(
                    Mathf.Lerp(
                        min_rotate_speed,
                        max_rotate_speed,
                        this.anger_level / this.angry_at
                    ),
                    Vector3.forward
                ) * new_offset;
            } else if (state == GhostState.GoalsCompleted) {
                // If the target item of this ghost is no longer in the player's inventory,
                // then we should no longer be in the goals completed state. Transition out
                // to the follower state.
                if (this.ghost_info != null && this.ghost_info.item_requirement != null) {
                    if (GhostManager.Instance.player.inventory[this.ghost_info.item_requirement] == 0) {
                        try_exit_goals_completed();
                        return;
                    }
                }
                var target_location = this.home_location;
                target_offset = this.home_location - (Vector2) player.transform.position;
                var dist_to_home = target_offset.magnitude;
                if (dist_to_home > max_follow_radius) {
                    var target_radius = Mathf.Lerp(min_follow_radius, max_follow_radius, next_noise(0.5f, 1f));
                    target_offset = target_offset.normalized * target_radius;
                    GhostManager.Instance.player.add_follow(this.transform, insistence);
                } else {
                    GhostManager.Instance.player.remove_follow();
                    target_offset = wander_around(target_offset, min_follow_radius);
                }
            } else {
                target_offset = wander_around(
                    this.home_location - (Vector2) player.transform.position,
                    min_follow_radius
                );
            }

            this.transform.position = Vector2.SmoothDamp(
                this.transform.position,
                (Vector2)player.transform.position + target_offset,
                ref move_param,
                move_time
            );
        }

        #endregion

        #region State Transition Functions

        private bool try_become_angry() {
            if (this.satisfied) return false;
            if (this.anger_level < this.angry_at || this.state == GhostState.Angry) {
                return false;
            }
            this.state = GhostState.Angry;
            providers_changed();
            return true;
        }

        private void try_subdue() {
            if (this.anger_level >= this.progress_subdue_amount || this.state != GhostState.Angry) {
                return;
            }
            this.anger_level = 0;
            this.state = GhostState.Following;
        }

        private bool try_enter_goals_completed() {
            if (this.satisfied) return false;
            if (this.ghost_info == null) {
                this.state = GhostState.GoalsCompleted;
                return true;
            }
            var completed = true;
            if (recalculate_tracking_items()) {
                completed &= this.items_to_track.Count == 0;
            }
            var flags = GhostManager.Instance.player.flags;
            foreach (var flag in this.ghost_info.flag_requirements) {
                completed &= flags[flag];
            }

            if (completed) {
                this.state = GhostState.GoalsCompleted;
                this.anger_level = 0;
                GhostManager.Instance.abandon_all_trackers(this);
                GhostManager.Instance.player.add_follow(this.transform, insistence);
            }

            return completed;
        }

        private bool try_exit_goals_completed() {
            if (this.satisfied) return false;
            if (this.ghost_info == null) {
                return false;
            }
            if (recalculate_tracking_items() && this.items_to_track.Count > 0) {
                this.state = GhostState.Following;
                try_become_angry();
                GhostManager.Instance.player.remove_follow();
                register_trackers();
                return true;
            }
            
            var flags = GhostManager.Instance.player.flags;
            foreach (var flag in this.ghost_info.flag_requirements) {
                if (!flags[flag]) {
                    this.state = GhostState.Following;
                    try_become_angry();
                    GhostManager.Instance.player.remove_follow();
                    register_trackers();
                    return true;
                }
            }
            return false;
        }

        private void satisfy() {
            this.state = GhostState.Satisfied;
            GhostManager.Instance.satify_ghost(this);
            if (this.ghost_info != null) {
                GhostManager.Instance.player.flags[$"satisfied:{this.ghost_info.name}"] = true;
            }
            GhostManager.Instance.player.remove_follow();
        }

        #endregion

        #region Main Ghost Transitions

        public bool become_primary(float time) {
            if (this.satisfied) {
                return false;
            }
            this.state = GhostState.Following;
            anger_level = time;
            if (this.ghost_info != null) {
                GhostManager.Instance.player.flags[$"was_primary:{this.ghost_info.name}"] = true;
                GhostManager.Instance.player.flags[$"primary:{this.ghost_info.name}"] = true;
            }
            if (!this.has_become_primary) {
                GhostManager.Instance.player.QueueInteraction(new FollowCall(this));
            }
            this.has_become_primary = true;
            
            if (!try_enter_goals_completed()) {
                recalculate_tracking_items();
                register_trackers();
                if (!try_become_angry()) {
                    providers_changed();
                }
            }
            return true;
        }

        public void release_primary() {
            if (this.state == GhostState.GoalsCompleted) {
                GhostManager.Instance.player.remove_follow();
            }
            if (this.ghost_info != null) {
                GhostManager.Instance.player.flags[$"primary:{this.ghost_info.name}"] = false;
            }
            if (!this.satisfied)
                this.state = GhostState.Waiting;
        }

        #endregion
        
        #region Tracking

        private RequirementTree requirements;
        private HashSet<RequirementTree.ItemRequirements> items_to_track;

        private bool recalculate_tracking_items() {
            if (this.ghost_info == null || this.ghost_info.item_requirement == null) {
                return false;
            }
            if (requirements == null) {
                Debug.Log("Requirements were null");
                requirements = new RequirementTree(this.ghost_info.item_requirement);
            }
            items_to_track = requirements.possibly_needed(GhostManager.Instance.player.inventory, 1);
            return true;
        }

        public void providers_changed() {
            if (!this.is_angry()) return;
            if (items_to_track == null) {
                if (!recalculate_tracking_items()) {
                    return;
                }
            }
            foreach (var provider in GhostManager.Instance.nearby_providers) {
                if (!this.items_to_track.Any(x => x.base_item == provider.resource)) {
                    GhostManager.Instance.block(this, this.transform.position, provider.resource);
                }
            }
        }

        #endregion
        
        #region Progress

        public void progress_made() {
            this.anger_level -= this.progress_subdue_amount;
            if (!try_enter_goals_completed()) {
                try_subdue();
                if (recalculate_tracking_items()) {
                    register_trackers();
                }
            }
        }

        public void flag_set(string flag) {
            if (flag.StartsWith("learned:")) {
                // When we learn a new recipe, we might be able to make something
                // we couldn't before. So, we need to recalculate requirements
                // next time we try to do tracking.
                requirements = null;
                if (recalculate_tracking_items())
                    register_trackers();
            }
            foreach (var required_flag in this.ghost_info.flag_requirements) {
                if (required_flag == flag) {
                    // Stop tracking goal for that flag
                }
            }
            try_enter_goals_completed();
        }

        public void flag_unset(string flag) {
            if (this.state == GhostState.GoalsCompleted) {
                try_exit_goals_completed();
            }
        }

        #endregion
        
        #region Interactable
        private bool has_checked_goal_entry = false;
        private InteractionEntryNode goal_entry;
        private bool has_checked_satisfied_entry = false;
        private InteractionEntryNode satisfied_entry;

        public IEnumerable<bool> run_interaction(Inventory inventory, FlagSet flags, UIController controller) {
            InteractionEntryNode entry_node = null;
            if (this.state == GhostState.GoalsCompleted) {
                if (!has_checked_goal_entry) {
                    goal_entry = this.GetEntryPoint("goal");
                    has_checked_goal_entry = true;
                }
                entry_node = goal_entry;
            } else if (this.state == GhostState.Satisfied) {
                if (!has_checked_satisfied_entry) {
                    satisfied_entry = this.GetEntryPoint("satisfied");
                    has_checked_satisfied_entry = true;
                }
                entry_node = satisfied_entry;
            } else {
                return null;
            }
            if (entry_node == null) {
                return null;
            }
            return run_graph_interaction(
                entry_node,
                inventory,
                flags,
                controller
            );
        }

        private IEnumerable<bool> run_graph_interaction(InteractionEntryNode entry, Inventory inventory, FlagSet flags, UIController controller) {
            if (!this.StartInteraction(entry, inventory, flags, controller, out var iteration)) {
                yield return true;
                while (!this.FollowInteraction(ref iteration)) {
                    yield return true;
                }
            }
            yield break;
        }

        private class FollowCall : Interactable {
            private Ghost ghost;
            public FollowCall(Ghost g) {
                ghost = g;
            }
            public IEnumerable<bool> run_interaction(Inventory inventory, FlagSet flags, UIController controller) {
                var entry = ghost.GetEntryPoint("follow");
                if (entry == null) return null;
                return ghost.run_graph_interaction(entry, inventory, flags, controller);
            }

        }

        #endregion
        
        #region InteractionsGraph

        protected override bool handle_custom_node(
            InteractionNode node,
            InteractionContext context,
            out InteractionNode next
        ) {
            if (node is SatisfyGhostNode) {
                this.satisfy();
            }
            return base.handle_custom_node(node, context, out next);
        }

        #endregion
    }
}
