using System;
using System.Collections.Generic;
using System.IO;
using Dialogue;
using UnityEngine;

namespace ScriptObjs
{
    [CreateAssetMenu(fileName = "New Dialogue", menuName = "New Dialogue")]
    public class Dialogue : ScriptableObject
    {
        public List<DialogueNode> nodes = new();
        private Dictionary<string, DialogueNode> _nodeLookup = new();

#if UNITY_EDITOR
        private void Awake()
        {
            if (nodes.Count == 0)
            {
                var rootNode = new DialogueNode
                {
                    id = Guid.NewGuid().ToString()
                };
                nodes.Add(rootNode);
            }

            OnValidate();
        } 
#endif

        private void OnValidate()
        {
            _nodeLookup.Clear();
            foreach (var node in GetAllNodes())
            {
                _nodeLookup[node.id] = node;
            }
        }

        public IEnumerable<DialogueNode> GetAllChildren(DialogueNode parentNode)
        {
            foreach (var childId in parentNode.childNodes)
            {
                if (_nodeLookup.ContainsKey(childId))
                    yield return _nodeLookup[childId];
            }
        }

        public IEnumerable<DialogueNode> GetAllNodes()
        {
            return nodes;
        }

        public DialogueNode GetRootNode()
        {
            return nodes[0];
        }

        public void CreateNode(DialogueNode node)
        {
            var newNode = new DialogueNode
            {
                id = Guid.NewGuid().ToString()
            };
            newNode.AddRectOffset(node.rect.position);
            
            nodes.Add(newNode);
            node.childNodes.Add(newNode.id);
            OnValidate();
        }

        public void DeleteNode(DialogueNode node)
        {
            nodes.Remove(node);
            OnValidate();
            CleanChildren(node);
        }

        private void CleanChildren(DialogueNode nodeToDelete)
        {
            foreach (var node in GetAllNodes())
            {
                node.childNodes.Remove(nodeToDelete.id);
            }
        }
    }
}
