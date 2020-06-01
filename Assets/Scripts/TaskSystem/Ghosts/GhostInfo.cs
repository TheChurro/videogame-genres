using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Interactions;
using Data;

namespace Task.Ghost {
    [CreateAssetMenu(menuName = "Character/GhostInfo")]
    public class GhostInfo : Interactions.CharacterInfo
    {
        public string[] flag_requirements;
        public ItemInfo item_requirement;
    }
}