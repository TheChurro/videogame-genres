using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Data {
    [System.Serializable]
    public enum FlagInstruction {
        Set,
        Unset,
        Require,
        Disables
    }
    [System.Serializable]
    public struct Flag {
        public FlagInstruction Instruction;
        public string Domain;
        public string Value;
    }

    public class FlagSet {
        HashSet<string> current_flags;
        public List<Flag> change_log;

        public FlagSet() {
            this.current_flags = new HashSet<string>();
            this.change_log = new List<Flag>();
        }

        public List<Flag> pull_change_log() {
            var log = new List<Flag>(this.change_log);
            this.change_log.Clear();
            return log;
        }

        public bool this[string key]
        {
            get => this.current_flags.Contains(key.ToLower());
            set {
                if (value) {
                    if (this.current_flags.Add(key.ToLower())) {
                        this.change_log.Add(new Flag{
                            Instruction = FlagInstruction.Set,
                            Domain = "",
                            Value = key,
                        });
                    }
                } else {
                    if (this.current_flags.Remove(key.ToLower())) {
                        this.change_log.Add(new Flag{
                            Instruction = FlagInstruction.Unset,
                            Domain = "",
                            Value = key,
                        });
                    }
                }
            }
        }
    }
}
