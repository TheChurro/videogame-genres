using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Data;

namespace Task {
    public class ResourceProvider : MonoBehaviour
    {
        public ItemInfo resource;

        public virtual void Start() {
            ResourceManager.Instance.register(this);
        }
    }
}