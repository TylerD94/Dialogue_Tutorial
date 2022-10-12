using System;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    public class FileNamePopup : EditorWindow
    {
        private static DialogueEditor _editor;
        private string _filename;
        
        public static Action<ScriptObjs.Dialogue> OnFileCreated;

        private void OnGUI()
        {
            EditorGUILayout.LabelField("File name?");

            var newDialogue = CreateInstance<ScriptObjs.Dialogue>();
            _filename = EditorGUILayout.TextField(_filename);

            var create = GUILayout.Button("Create file");
            
            if (create)
            {
                newDialogue.name = _filename;
                AssetDatabase.CreateAsset(newDialogue, $"Assets/Dialogue/{newDialogue.name}.asset");
                OnFileCreated(newDialogue);
                Close();
            }        
        }
    }
}