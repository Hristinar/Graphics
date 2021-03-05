using UnityEngine;

namespace UnityEditor.Rendering.HighDefinition
{
    static partial class HDCameraUI
    {
        class Styles
        {
            public static GUIContent projectionSettingsHeaderContent { get; } = EditorGUIUtility.TrTextContent("Projection");
            public static GUIContent physicalSettingsHeaderContent { get; } = EditorGUIUtility.TrTextContent("Physical Camera");
            public static GUIContent environmentSettingsHeaderContent { get; } = EditorGUIUtility.TrTextContent("Environment");
            public static GUIContent outputSettingsHeaderContent { get; } = EditorGUIUtility.TrTextContent("Output");

            public static GUIContent clippingPlaneMultiFieldTitle = EditorGUIUtility.TrTextContent("Clipping Planes");

            public const string msaaWarningMessage = "Manual MSAA target set with deferred rendering. This will lead to undefined behavior.";

            public static readonly GUIContent clearModeContent = EditorGUIUtility.TrTextContent("Background Type", "Specifies the type of background the Camera applies when it clears the screen before rendering a frame. Be aware that when setting this to None, the background is never cleared and since HDRP shares render texture between cameras, you may end up with garbage from previous rendering.");
            public static readonly GUIContent backgroundColorContent = EditorGUIUtility.TrTextContent("Background Color", "The Background Color used to clear the screen when selecting Background Color before rendering.");
            public static readonly GUIContent volumeLayerMaskContent = EditorGUIUtility.TrTextContent("Volume Layer Mask");
            public static readonly GUIContent volumeAnchorOverrideContent = EditorGUIUtility.TrTextContent("Volume Anchor Override");
            
            public static readonly GUIContent projectionContent = EditorGUIUtility.TrTextContent("Projection", "How the Camera renders perspective.\n\nChoose Perspective to render objects with perspective.\n\nChoose Orthographic to render objects uniformly, with no sense of perspective.");
            public static readonly GUIContent sizeContent = EditorGUIUtility.TrTextContent("Size");
            public static readonly GUIContent fieldOfViewContent = EditorGUIUtility.TrTextContent("Field of View", "The height of the Camera’s view angle, measured in degrees along the local Y axis.");
            public static readonly GUIContent focalLengthContent = EditorGUIUtility.TrTextContent("Focal Length", "The simulated distance between the lens and the sensor of the physical camera. Larger values give a narrower field of view.");
            public static readonly GUIContent FOVAxisModeContent = EditorGUIUtility.TrTextContent("FOV Axis", "Field of view axis.");
            public static readonly GUIContent sensorSizeContent = EditorGUIUtility.TrTextContent("Sensor Size", "The size of the camera sensor in millimeters.");
            public static readonly GUIContent lensShiftContent = EditorGUIUtility.TrTextContent("Shift", "Offset from the camera sensor. Use these properties to simulate a shift lens. Measured as a multiple of the sensor size.");
            public static readonly GUIContent physicalCameraContent = EditorGUIUtility.TrTextContent("Link FOV to Physical Camera", "Enables Physical camera mode for FOV calculation. When checked, the field of view is calculated from properties for simulating physical attributes (focal length, sensor size, and lens shift).");
            public static readonly GUIContent cameraTypeContent = EditorGUIUtility.TrTextContent("Sensor Type", "Common sensor sizes. Choose an item to set Sensor Size, or edit Sensor Size for your custom settings.");
            public static readonly GUIContent gateFitContent = EditorGUIUtility.TrTextContent("Gate Fit", "Determines how the rendered area (resolution gate) fits into the sensor area (film gate).");
            public static readonly GUIContent nearPlaneContent = EditorGUIUtility.TrTextContent("Near", "The closest point relative to the camera that drawing occurs.");
            public static readonly GUIContent farPlaneContent = EditorGUIUtility.TrTextContent("Far", "The furthest point relative to the camera that drawing occurs.");
            public static readonly GUIContent probeLayerMaskContent = EditorGUIUtility.TrTextContent("Probe Layer Mask", "The layer mask to use to cull probe influences.");

            // TODO: Tooltips
            public static readonly GUIContent isoContent = EditorGUIUtility.TrTextContent("Iso", "Sets the light sensitivity of the Camera sensor. This property affects Exposure if you set its Mode to Use Physical Camera.");
            public static readonly GUIContent shutterSpeedContent = EditorGUIUtility.TrTextContent("Shutter Speed", "The amount of time the Camera sensor is capturing light.");
            public static readonly GUIContent apertureContent = EditorGUIUtility.TrTextContent("Aperture", "The f-stop (f-number) of the lens. Lower values give a wider lens aperture.");
            public static readonly GUIContent bladeCountContent = EditorGUIUtility.TrTextContent("Blade Count", "The number of blades in the lens aperture. Higher values give a rounder aperture shape.");
            public static readonly GUIContent curvatureContent = EditorGUIUtility.TrTextContent("Curvature", "Controls the curvature of the lens aperture blades. The minimum value results in fully-curved, perfectly-circular bokeh, and the maximum value results in visible aperture blades.");
            public static readonly GUIContent barrelClippingContent = EditorGUIUtility.TrTextContent("Barrel Clipping", "Controls the self-occlusion of the lens, creating a cat's eye effect.");
            public static readonly GUIContent anamorphismContent = EditorGUIUtility.TrTextContent("Anamorphism", "Use the slider to stretch the sensor to simulate an anamorphic look.");

            public static readonly GUIContent allowDynResContent = EditorGUIUtility.TrTextContent("Allow Dynamic Resolution", "Whether to support dynamic resolution.");

            public static readonly GUIContent viewportContent = EditorGUIUtility.TrTextContent("Viewport Rect", "Four values that indicate where on the screen HDRP draws this Camera view. Measured in Viewport Coordinates (values in the range of [0, 1]).");
            public static readonly GUIContent depthContent = EditorGUIUtility.TrTextContent("Depth");

#if ENABLE_VR && ENABLE_XR_MANAGEMENT
            public static readonly GUIContent xrRenderingContent = EditorGUIUtility.TrTextContent("XR Rendering");
#endif

#if ENABLE_MULTIPLE_DISPLAYS
            public static readonly GUIContent targetDisplayContent = EditorGUIUtility.TrTextContent("Target Display");
#endif

            public static readonly GUIContent[] antialiasingModeNames =
            {
                EditorGUIUtility.TrTextContent("No Anti-aliasing"),
                EditorGUIUtility.TrTextContent("Fast Approximate Anti-aliasing (FXAA)"),
                EditorGUIUtility.TrTextContent("Temporal Anti-aliasing (TAA)"),
                EditorGUIUtility.TrTextContent("Subpixel Morphological Anti-aliasing (SMAA)")
            };
        }
    }
}
