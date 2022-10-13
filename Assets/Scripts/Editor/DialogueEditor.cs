using System.IO;
using Codice.Client.Commands.Tree;
using Dialogue;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace Editor
{
    public class DialogueEditor : EditorWindow
    {
        private static ScriptObjs.Dialogue _selectedDialogue;
        private static readonly string DirectoryPath = $"{Directory.GetCurrentDirectory()}/Assets/Dialogue";
        private static DialogueEditor _window;
        private static DialogueNode _createdNode;
        private static DialogueNode _linkingParentNode;
        private static DialogueNode _deletedNode;
        private static Vector2 _scrollPosition;

        private DialogueNode _draggingNode;
        private Vector2 _draggingOffset;
        private bool _draggingWindow;
        private Vector2 _windowDragPoint;

        private const float CanvasSize = 4000;
        private const float BackgroundSize = 50;



#region Static Methods

        [MenuItem("Custom Tools/Dialogue Editor")]
        private static void ShowWindow()
        {
            _window = GetWindow<DialogueEditor>();
            if (_selectedDialogue == null)
            {
                CreateNewDialogue();
            }

            _window.Show();
        }
        
        [OnOpenAsset(1)]
        public static bool OnOpenAsset(int instanceId, int line)
        {
            _selectedDialogue = EditorUtility.InstanceIDToObject(instanceId) as ScriptObjs.Dialogue;
            if (_selectedDialogue == null) return false; 

            ShowWindow();
            return true;
        }
        
        private static void CreateNewDialogue()
        {
            var popup = GetWindow(typeof(FileNamePopup), true, "Enter new file name") as FileNamePopup;
            popup.maxSize = new Vector2(300, 100);
            popup.Show();
        }

        private static void SetNewDialogue(ScriptObjs.Dialogue d)
        {
            _selectedDialogue = d;
        }

        private static void CheckForDirectory()
        {
            if (!Directory.Exists(DirectoryPath))
                Directory.CreateDirectory(DirectoryPath);
        }
        
        private static void DrawNode(DialogueNode node)
        {
            GUILayout.BeginArea(node.rect);

            EditorGUI.BeginChangeCheck();

            var newText = EditorGUILayout.TextField(node.text);

            if(EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_selectedDialogue, "Update dialogue."); 
                node.text = newText;
            }

            DisplayButtons(node);

            GUILayout.EndArea();
        }

        private static void DisplayButtons(DialogueNode node)
        {
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Create"))
                _createdNode = node;
            
            if (_linkingParentNode == null)
            {
                if (GUILayout.Button("Link"))
                    _linkingParentNode = node;
            }
            else
            {
                SetLinkMode(node);
            }

            if (GUILayout.Button("Delete"))
                _deletedNode = node;
            
            GUILayout.EndHorizontal();
        }
        
        private static void CreateNode()
        {
            if (_createdNode == null) return;
            Undo.RecordObject(_selectedDialogue, "Create node");
            _selectedDialogue.CreateNode(_createdNode);
            _createdNode = null;
        }

        private static void EndLink()
        {
            _linkingParentNode = null;
        }

        private static void SetLinkMode(DialogueNode node)
        {
            if (node == _linkingParentNode)
            {
                if (GUILayout.Button("Cancel"))
                    EndLink();
            }
            else
            {
                if (_linkingParentNode.childNodes.Contains(node.id))
                    RemoveLink(node);
                else
                    CreateLink(node);
            }
        }

        private static void CreateLink(DialogueNode node)
        {
            if (GUILayout.Button("Add Link"))
            {
                Undo.RecordObject(_selectedDialogue, "Add link");
                _linkingParentNode.childNodes.Add(node.id);
                EndLink();
            }
        }

        private static void RemoveLink(DialogueNode node)
        {
            if (GUILayout.Button("Remove Link"))
            {
                Undo.RecordObject(_selectedDialogue, "Remove link");
                _linkingParentNode.childNodes.Remove(node.id);
                EndLink();
            }
        }
        
        private static void DeleteNode()
        {
            if (_deletedNode == null) return;
            Undo.RecordObject(_selectedDialogue, "Delete node");
            _selectedDialogue.DeleteNode(_deletedNode);
            _deletedNode = null;
        }

        private static void DrawConnections(DialogueNode node)
        {
            const int lineOffset = 10;
            const int controlOffset = 20;
                
            var startPos = new Vector2(node.rect.xMax + lineOffset, node.rect.center.y);
            var startControl = new Vector2(startPos.x + controlOffset, startPos.y);

            foreach (var childNode in _selectedDialogue.GetAllChildren(node))
            {
                var endPos = new Vector2(childNode.rect.xMin - lineOffset, childNode.rect.center.y);
                var endControl = new Vector2(endPos.x - controlOffset, endPos.y);
                
                Handles.DrawBezier(
                    startPos, endPos, 
                    startControl, endControl, 
                    Color.white, null, 3);
            }
        }
        
        private static DialogueNode GetNodeAtPoint(Vector2 mousePos)
        {
            DialogueNode selectedNode = null;
            foreach (var node in _selectedDialogue.GetAllNodes())
            {
                // Scroll position is added to mouse position to account for window position
                if (node.rect.Contains(mousePos + _scrollPosition))
                    selectedNode = node;
            }

            return selectedNode;
        }
        
#endregion

#region Instance Methods

        private void OnEnable()
        {
            FileNamePopup.OnFileCreated += SetNewDialogue;
            Selection.selectionChanged += OnSelectionChanged;
            CheckForDirectory();
        }

        private void OnSelectionChanged()
        {
            _selectedDialogue = Selection.activeObject as ScriptObjs.Dialogue;
            if (_selectedDialogue == null) return;
            Repaint();
        }

        private void OnGUI()
        {
            if (_selectedDialogue == null) return;
            _window.titleContent = new GUIContent(_selectedDialogue.name);

            ProcessEvents();

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            var canvas = GUILayoutUtility.GetRect(CanvasSize, CanvasSize);
            var backgroundTexture = Resources.Load("background") as Texture2D;
            var texCoords = new Rect(0, 0, CanvasSize / BackgroundSize, CanvasSize / BackgroundSize);
            GUI.DrawTextureWithTexCoords(canvas, backgroundTexture, texCoords);

            foreach (var node in _selectedDialogue.GetAllNodes())
            {
                DrawConnections(node);
            }
            
            foreach (var node in _selectedDialogue.GetAllNodes())
            { 
                DrawNode(node);
            }
            
            EditorGUILayout.EndScrollView();
            
            CreateNode();
            
            DeleteNode();
        }
        
        private void ProcessEvents()
        {
            if (Event.current.type == EventType.MouseDown && _draggingNode == null)
            {
                _draggingNode = GetNodeAtPoint(Event.current.mousePosition);
                if (_draggingNode != null)
                    _draggingOffset = _draggingNode.rect.position - Event.current.mousePosition;
                _draggingWindow = true;
                _windowDragPoint = Event.current.mousePosition + _scrollPosition;
            }
            else if (Event.current.type == EventType.MouseDrag && _draggingNode != null)
            {
                Undo.RecordObject(_selectedDialogue, "Move");
                _draggingNode.rect.position = Event.current.mousePosition + _draggingOffset;
                GUI.changed = true;
            }
            else if (Event.current.type == EventType.MouseDrag && _draggingWindow)
            {
                _scrollPosition = (Event.current.mousePosition - _windowDragPoint) * -1;
                GUI.changed = true;
            }
            else if (Event.current.type == EventType.MouseUp && _draggingNode != null)
            {
                _draggingNode = null;
            }
            else if (Event.current.type == EventType.MouseUp && _draggingWindow)
            {
                _draggingWindow = false;
            }
        }

        #endregion
    }
}