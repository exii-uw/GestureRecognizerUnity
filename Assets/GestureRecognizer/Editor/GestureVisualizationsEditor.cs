using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

namespace GestureRecognizer
{
    [CustomEditor(typeof(GestureVisualizer))]
    public class GestureVisualizationEditor : Editor
    {
        private GestureVisualizer m_gestureVisualizer;

        private bool mExtraConfiguration = false;


        void OnEnable()
        {
            if (m_gestureVisualizer == null)
            {
                m_gestureVisualizer = target as GestureVisualizer;
            }
        }

        public override void OnInspectorGUI()
        {
            m_gestureVisualizer.GestureComposerInstance = (GestureComposer)EditorGUILayout.ObjectField(
                "Gesture Composer",
                m_gestureVisualizer.GestureComposerInstance,
                typeof(GestureComposer), 
                true);

            m_gestureVisualizer.GestureColor = EditorGUILayout.ColorField("Gesture Color", m_gestureVisualizer.GestureColor);
            m_gestureVisualizer.LineThickness = EditorGUILayout.Slider("Line Thickness", m_gestureVisualizer.LineThickness, 1, 40);
            m_gestureVisualizer.Completeness = EditorGUILayout.Slider("Gesture Completeness", m_gestureVisualizer.Completeness, 0, 1);
            m_gestureVisualizer.Trail = EditorGUILayout.Slider("Gesture Trail", m_gestureVisualizer.Trail, 0, 0.5f);
            m_gestureVisualizer.GlobalAlpha = EditorGUILayout.Slider("Gesture Alpha", m_gestureVisualizer.GlobalAlpha, 0, 1.0f);

            if (m_gestureVisualizer.GestureComposerInstance != null)
            {
                string[] names = m_gestureVisualizer.GestureComposerInstance.GetListofGestureNames().ToArray();
                if (names.Length > m_gestureVisualizer.m_GestureIndex && m_gestureVisualizer.m_GestureIndex >= 0)
                {
                    m_gestureVisualizer.m_GestureIndex = EditorGUILayout.Popup(m_gestureVisualizer.m_GestureIndex, names);
                    m_gestureVisualizer.GestureName = names[m_gestureVisualizer.m_GestureIndex];
                }
            }

            // Renderer Type
            m_gestureVisualizer.LineRenderer = (GestureVisualizer.LineRendererMaterial) EditorGUILayout.EnumPopup("Line Renderer", m_gestureVisualizer.LineRenderer);


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
                EditorUtility.SetDirty(m_gestureVisualizer);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(m_gestureVisualizer.gameObject.scene);
            }




        }
    }
}

