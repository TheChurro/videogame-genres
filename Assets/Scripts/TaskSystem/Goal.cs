using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Task {
    public class Goal : MonoBehaviour
    {
        public string completed_flag;
        void Awake() {
            GoalManager.Instance.register(this);
        }
    }

    public class GoalManager
    {
        private static GoalManager manager;
        public static GoalManager Instance {
            get {
                if (manager == null) {
                    manager = new GoalManager();
                }
                return manager;
            }
        }

        private List<Goal> goals;

        public GoalManager() {
            this.goals = new List<Goal>();
        }

        public void register(Goal goal) {
            this.goals.Add(goal);
        }

        public IEnumerable<Goal> for_flag(string flag) {
            foreach (var goal in this.goals) {
                if (goal.completed_flag == flag) {
                    yield return goal;
                }
            }
        }
    }
}