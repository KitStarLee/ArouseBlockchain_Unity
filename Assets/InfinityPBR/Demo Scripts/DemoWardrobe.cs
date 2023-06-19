using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DemoWardrobe {
    
    public List<WardrobeItem> wardrobeObjects = new List<WardrobeItem>();

    
    [System.Serializable]
    public class WardrobeItem
    {
        public GameObject wardrobe;
        public bool active = false;
    }
}
