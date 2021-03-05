using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    internal class UniversalRenderPipelineCameraStackUI
    {
        // This is the valid list of types, so if we need to add more types we just add it here
        static readonly List<CameraRenderType> s_ValidCameraTypes = new List<CameraRenderType>
        {
            CameraRenderType.Overlay
            // Add here the supported types of cameras
        };

        static class Styles
        {
            public const int cameraNameWidth = 150;
            public const int errorIconWidth = 12;

            public static readonly Vector2 errorIconSize = new Vector2(errorIconWidth, errorIconWidth);
            public static readonly GUIStyle errorStyle = new GUIStyle(EditorStyles.label) { padding = new RectOffset { left = -errorIconWidth }};

            static readonly Texture2D errorIcon = EditorGUIUtility.Load("icons/console.erroricon.sml.png") as Texture2D;
            public static readonly Dictionary<CameraRenderType, GUIContent> typeNotSupportedMessage = Enum.GetValues(typeof(CameraRenderType))
                .Cast<CameraRenderType>()
                .Where(cameraRenderType => !s_ValidCameraTypes.Contains(cameraRenderType))
                .ToDictionary(cameraRenderType => cameraRenderType, cameraRenderType => EditorGUIUtility.TrTextContent(string.Empty, cameraRenderType.GetName() + " is not supported", errorIcon));

            public static readonly GUIContent[] camerasToAddNotFound = { EditorGUIUtility.TrTextContent("Could not find suitable cameras") };
            public static readonly GUIContent PP = EditorGUIUtility.TrTextContent("PP", "The camera has enabled the Post Processing");

            public static readonly string validTypes = $"Valid types are: {string.Join(",", s_ValidCameraTypes.Select(i => $"{i}"))}";
        }

        readonly Camera m_Camera;
        readonly ReorderableList m_LayerList;
        readonly UniversalRenderPipelineSerializedCamera m_SerializedCamera;

        bool CanRemove(ReorderableList list)
        {
            // As the list can delete the last item, allow the delete action if there is at least one item
            return m_SerializedCamera.cameras.arraySize > 0;
        }

        /// <summary>
        /// Returns a <see cref="Camera"/> at a given index
        /// </summary>
        /// <param name="index">The index to fetch the camera</param>
        /// <returns><see cref="Camera"/> if the index is valid, otherwise returns null</returns>
        public Camera this[int index]
        {
            get
            {
                if (index < 0 || index >= m_SerializedCamera.cameras.arraySize)
                    return null;

                // Return the camera on that index
                return m_SerializedCamera.cameras
                    .GetArrayElementAtIndex(index)
                    .objectReferenceValue as Camera;
            }
        }

        void RemoveCameraByIndexFromList(int selectedIndex)
        {
            Camera cam = this[selectedIndex];
            if (cam == null)
                return;

            // Delete the camera from the object
            m_SerializedCamera.cameras.DeleteArrayElementAtIndex(selectedIndex);
        }

        void RemoveSelectedCamerasFromList(ReorderableList list)
        {
            var selectedIndices = list.selectedIndices;

            if (selectedIndices.Any())
            {
                // Remove the selected indices on the list
                foreach (int selectedIndex in selectedIndices)
                    RemoveCameraByIndexFromList(selectedIndex);
            }
            else
            {
                // Nothing has selected, remove the last item on the list
                ReorderableList.defaultBehaviours.DoRemoveButton(list);
            }

            m_SerializedCamera.serializedObject.ApplyModifiedProperties();
        }

        List<Camera> availableCamerasToAdd { get; } = new List<Camera>();

        void ShowCustomMenuWithAvailableCamerasToAdd(Rect rect, ReorderableList list)
        {
            // Need to do clear the list here otherwise the menu just fills up with more and more entries
            availableCamerasToAdd.Clear();

            var allCameras = FindCamerasToReference(m_Camera.gameObject);
            foreach (var camera in allCameras)
            {
                var component = camera.gameObject.GetComponent<UniversalAdditionalCameraData>();
                if (component == null)
                    continue;

                if (s_ValidCameraTypes.Contains(component.renderType))
                {
                    availableCamerasToAdd.Add(camera);
                }
            }

            var names = (availableCamerasToAdd.Any())
                ? availableCamerasToAdd
                    .Select((camera, i) => new GUIContent((i + 1) + " " + availableCamerasToAdd[i].name))
                    .ToArray()
                : Styles.camerasToAddNotFound;

            EditorUtility.DisplayCustomMenu(rect, names, -1, AddSelectedCameraFromCustomMenu, null);
        }

        void AddSelectedCameraFromCustomMenu(object userData, string[] options, int selected)
        {
            if (!availableCamerasToAdd.Any())
                return;

            var length = m_SerializedCamera.cameras.arraySize;
            ++m_SerializedCamera.cameras.arraySize;
            m_SerializedCamera.serializedObject.ApplyModifiedProperties();
            m_SerializedCamera.cameras.GetArrayElementAtIndex(length).objectReferenceValue = availableCamerasToAdd[selected];
            m_SerializedCamera.serializedObject.ApplyModifiedProperties();
        }

        // Modified version of StageHandle.FindComponentsOfType<T>()
        // This version more closely represents unity object referencing restrictions.
        // I added these restrictions:
        // - Can not reference scene object outside scene
        // - Can not reference cross scenes
        // - Can reference child objects if it is prefab
        Camera[] FindCamerasToReference(GameObject gameObject)
        {
            var scene = gameObject.scene;

            var inScene = !EditorUtility.IsPersistent(m_Camera) || scene.IsValid();
            var inPreviewScene = EditorSceneManager.IsPreviewScene(scene) && scene.IsValid();
            var inCurrentScene = !EditorUtility.IsPersistent(m_Camera) && scene.IsValid();

            Camera[] cameras = Resources.FindObjectsOfTypeAll<Camera>();
            if (!cameras.Any())
                return Array.Empty<Camera>();

            if (!inScene)
            {
                return cameras
                    .Where(camera => camera.transform.IsChildOf(gameObject.transform))
                    .ToArray();
            }

            if (inPreviewScene)
            {
                return cameras
                    .Where(camera => camera.gameObject.scene == scene)
                    .ToArray();
            }

            if (inCurrentScene)
            {
                return cameras
                    .Where(camera =>
                    !EditorUtility.IsPersistent(camera) &&
                    !EditorSceneManager.IsPreviewScene(camera.gameObject.scene) && camera.gameObject.scene == scene)
                    .ToArray();
            }

            return Array.Empty<Camera>();
        }

        void SelectElement(ReorderableList list)
        {
            Camera cam = this[list.index];
            if (cam == null)
                return;

            if (Event.current.clickCount == 2)
            {
                Selection.activeObject = cam;
            }

            EditorGUIUtility.PingObject(cam);
        }

        void DrawElementCallback(Rect rect, int index, bool isActive, bool isFocused)
        {
            Camera cam = this[index];
            if (cam == null)
            {
                m_Camera
                    .GetComponent<UniversalAdditionalCameraData>()
                    .UpdateCameraStack();

                return;
            }

            rect.height = EditorGUIUtility.singleLineHeight;
            rect.y += 1;

            EditorGUI.LabelField(rect, cam.name, EditorStyles.label);

            var typeRect = rect;
            typeRect.xMin += Styles.cameraNameWidth + Styles.errorIconWidth;

            var type = cam.gameObject.GetComponent<UniversalAdditionalCameraData>().renderType;
            if (!s_ValidCameraTypes.Contains(type))
            {
                using (new EditorGUIUtility.IconSizeScope(Styles.errorIconSize))
                    EditorGUI.LabelField(typeRect, Styles.typeNotSupportedMessage[type], Styles.errorStyle);
            }

            EditorGUI.LabelField(typeRect, type.GetName(), EditorStyles.label);

            // Printing if Post Processing is on or not.
            if (cam.gameObject.GetComponent<UniversalAdditionalCameraData>().renderPostProcessing)
            {
                var ppWidth = EditorStyles.label.CalcSize(Styles.PP).x;
                Rect ppRect = new Rect(rect.width - ppWidth, rect.y, ppWidth, EditorGUIUtility.singleLineHeight);
                EditorGUI.LabelField(ppRect, Styles.PP, EditorStyles.label);
            }
        }

        /// <summary>
        /// Renders the list of camera stacks and the possible errors that might be found
        /// </summary>
        public UniversalRenderPipelineCameraStackUI(Camera camera, UniversalRenderPipelineSerializedCamera serializedCamera)
        {
            m_Camera = camera;
            m_SerializedCamera = serializedCamera;

            m_LayerList = new ReorderableList(m_SerializedCamera.serializedObject, m_SerializedCamera.cameras, true, false, true, true);
            m_LayerList.drawElementCallback += DrawElementCallback;
            m_LayerList.onSelectCallback += SelectElement;
            m_LayerList.onRemoveCallback = RemoveSelectedCamerasFromList;
            m_LayerList.onAddDropdownCallback = ShowCustomMenuWithAvailableCamerasToAdd;
            m_LayerList.onCanRemoveCallback = CanRemove;
        }

        readonly StringBuilder m_ErrorsStringBuilder = new StringBuilder();
        void DisplayErrors()
        {
            m_ErrorsStringBuilder.Clear();
            m_ErrorsStringBuilder.AppendLine("These cameras are not of a valid type:");

            bool errorsFound = false;
            for (int index = 0; index < m_SerializedCamera.cameras.arraySize; ++index)
            {
                Camera cam = this[index];
                var type = cam.gameObject.GetComponent<UniversalAdditionalCameraData>().renderType;
                if (!s_ValidCameraTypes.Contains(type))
                {
                    errorsFound = true;
                    m_ErrorsStringBuilder.AppendLine($" - {cam.name}({type})");
                }
            }

            m_ErrorsStringBuilder.Append(Styles.validTypes);

            if (errorsFound)
                EditorGUILayout.HelpBox(m_ErrorsStringBuilder.ToString(), MessageType.Warning);
        }

        /// <summary>
        /// Renders the list of camera stack and the possible errors that might be found
        /// </summary>
        public void OnGUI()
        {
            EditorGUILayout.Space();

            // Obtain the size of the list ( all the items and the buttons ), and expand to the inspector width and draw it
            var rect = GUILayoutUtility.GetRect(1, m_LayerList.GetHeight(), GUILayout.ExpandWidth(true));
            m_LayerList.DoList(EditorGUI.IndentedRect(rect));

            DisplayErrors();

            EditorGUILayout.Space();
            EditorGUILayout.Space();

            // Apply any changes to the object that might had happened
            m_SerializedCamera.serializedObject.ApplyModifiedProperties();
        }
    }
}
