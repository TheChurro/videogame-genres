using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Data;

namespace Task {
    public class ResourceManager
    {
        private static ResourceManager manager;
        public static ResourceManager Instance {
            get {
                if (manager == null) {
                    manager = new ResourceManager();
                }
                return manager;
            }
        }

        private List<ResourceProvider> providers;

        public ResourceManager() {
            this.providers = new List<ResourceProvider>();
        }

        public void register(ResourceProvider provider) {
            this.providers.Add(provider);
        }

        public IEnumerable<ResourceProvider> those_providing(ItemInfo item) {
            foreach (var provider in this.providers) {
                if (provider.resource == item) {
                    yield return provider;
                }
            }
        }
    }
}