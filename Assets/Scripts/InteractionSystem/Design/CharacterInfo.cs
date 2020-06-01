using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Interactions {
	[CreateAssetMenu(menuName = "Character/CharacterInfo")]
	public class CharacterInfo : ScriptableObject {
        // name is an implicit field of a ScriptableObject
        public Texture2D portrait;
		public Color color;
	}
}