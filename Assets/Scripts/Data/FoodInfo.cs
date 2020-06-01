using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Data {
    [CreateAssetMenu(menuName = "Items/FoodInfo")]
	public class FoodInfo : ItemInfo {
        public FoodEffect effect;
	}

    [System.Serializable]
    public enum FoodEffect {
        None,
        Speed,
        Strength,
        Growth
    }
}