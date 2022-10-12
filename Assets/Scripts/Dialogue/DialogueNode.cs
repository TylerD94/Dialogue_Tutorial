using System.Collections.Generic;
using UnityEngine;

namespace Dialogue
{
    [System.Serializable]
    public class DialogueNode
    {
        public string id;
        public string text;
        public List<string> childNodes = new();
        public Rect rect = new Rect(10, 10, 200, 75);

        public void AddRectOffset(Vector2 parentNodePosition)
        {
            var offset = parentNodePosition + new Vector2(250, 50);
            rect.position += offset;
        }
    }
}