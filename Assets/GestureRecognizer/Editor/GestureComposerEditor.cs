using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using SFB;
using System.IO;

namespace GestureRecognizer
{
    [CustomEditor(typeof(GestureComposer))]
    public class GestureComposerEditor : Editor
    {
        private GestureComposer m_gestureComposer;

        private bool mExtraConfiguration = false;


        void OnEnable()
        {
            if (m_gestureComposer == null)
            {
                m_gestureComposer = target as GestureComposer;
                if (!EditorApplication.isPlaying)
                {
                    m_gestureComposer.UpdateGestureSet();
                }
            }
        }

        private void Awake()
        {

        }

        public override void OnInspectorGUI()
        {

            if (GUILayout.Button("New Gesture Set"))
            {
                m_gestureComposer.GestureSet = new GestureGroup();
            }


            EditorGUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("Import"))
                {
                    var extensions = new[] {
                        new ExtensionFilter("Gesture Sets", "json"),
                    };
                    var paths = StandaloneFileBrowser.OpenFilePanel("Import Gestset", "", extensions, false);
                    if (paths.Length > 0)
                    {
                        var file = paths[0];
                        var name = Path.GetFileNameWithoutExtension(file);
                        m_gestureComposer.ActiveGestureSet = name;
                        m_gestureComposer.LoadGestureSet(file);
                        m_gestureComposer.GestureSet.GestureSetName = name;

                        SaveToStreamingAssets(name, m_gestureComposer.GestureSet);
                    }

                }

                if (GUILayout.Button("Export"))
                {
                    string name = m_gestureComposer.GestureSet.GestureSetName;
                    string str = JsonUtility.ToJson(m_gestureComposer.GestureSet);

                    var extensionList = new[] {
                        new ExtensionFilter("JSON", "json"),
                    };
                    var path = StandaloneFileBrowser.SaveFilePanel("Save File", "", name, extensionList);

                    using (FileStream fs = new FileStream(path, FileMode.Create))
                    {
                        using (StreamWriter writer = new StreamWriter(fs))
                        {
                            writer.Write(str);
                        }
                    }

                }
            }
            EditorGUILayout.EndHorizontal();

            // Disable when running            
            EditorGUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("Save to Assets"))
                {
                    m_gestureComposer.GestureSet.NormalizeAllGesturesInPlace();
                    ExportToStreamingAssets(m_gestureComposer.GestureSet);
                }
            }
            EditorGUILayout.EndHorizontal();


            GUI.enabled = EditorApplication.isPlaying;
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("Add Gesture"))
                {
                    m_gestureComposer.AddGesture();
                }

                if (GUILayout.Button("Delete Gesture"))
                {
                    m_gestureComposer.Undo();
                }
            }
            EditorGUILayout.EndHorizontal();






            // General Content
            GUI.enabled = true;
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            // Extra Room Configuration
            mExtraConfiguration = EditorGUILayout.Foldout(mExtraConfiguration, "Details");
            EditorGUI.indentLevel++;
            using (var extraConfigGroup = new EditorGUILayout.FadeGroupScope(Convert.ToSingle(mExtraConfiguration)))
            {
                if (extraConfigGroup.visible)
                {
                    DrawDefaultInspector();
                }
            }
            EditorGUI.indentLevel--;


            if (GUI.changed && !EditorApplication.isPlaying)
            {
                EditorUtility.SetDirty(m_gestureComposer);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(m_gestureComposer.gameObject.scene);
            }




        }

        private void ExportToStreamingAssets(GestureGroup gestureGroup)
        {
            if (gestureGroup == null) return;
            string name = gestureGroup.GestureSetName;
            SaveToStreamingAssets(name, gestureGroup);
        }

        private void SaveToStreamingAssets(string name, GestureGroup gestureGroup)
        {
            Directory.CreateDirectory(Application.streamingAssetsPath);

            string path = Path.Combine(Application.streamingAssetsPath, $"{name}.json");
            string str = JsonUtility.ToJson(gestureGroup);

            using (FileStream fs = new FileStream(path, FileMode.Create))
            {
                using (StreamWriter writer = new StreamWriter(fs))
                {
                    writer.Write(str);
                }
            }
        }
    }
}

