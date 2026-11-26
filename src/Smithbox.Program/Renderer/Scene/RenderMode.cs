using StudioCore.Application;
using StudioCore.Editors.MapEditor;

namespace StudioCore.Renderer;

/// <summary>How the viewports draw the scene, <see cref="CFG.Viewport_Render_Mode" /></summary>
public enum ViewportRenderMode
{
    /// <summary>Cheap lighting: surfaces are lit by the viewport's sun and the environment map alone, so their
    /// normal, specular and metallic maps read, and the map lighting, GI, shadows, lights, fog and screen effects are
    /// off whatever the Rendering tool says</summary>
    Editor = 0,

    /// <summary>The Rendering tool's settings decide the lighting and effects</summary>
    Game = 1
}

/// <summary>What the textured image shows, <see cref="CFG.Viewport_Texturing_Mode" /></summary>
public enum ViewportTexturingMode
{
    Diffuse = 0,

    /// <summary>The Normals surface view in place of the image</summary>
    Normal = 1
}

/// <summary>
///     The one place the render mode is read: the pipeline and its passes ask here instead of reading the feature
///     switches on their own, so Editor mode turns them all off together and Game mode leaves them as set.
/// </summary>
public static class RenderMode
{
    public static ViewportRenderMode Current => (ViewportRenderMode)CFG.Current.Viewport_Render_Mode;

    public static bool IsGame => Current == ViewportRenderMode.Game;

    /// <summary>Whether the screen effects run: Game mode with them switched on</summary>
    public static bool PostEffects => IsGame && CFG.Current.Viewport_Post_Effects;

    /// <summary>
    ///     Whether Game mode lights the Map Editor's maps with their BTL lights alone, for the selected project's game
    ///     (<see cref="GparamProfile.MapLightsOnly" />). The pipelines it applies to say so in
    ///     <see cref="SceneRenderPipeline.MapLightsOnly" />.
    /// </summary>
    public static bool MapLightsOnly => IsGame && GparamProfile.Current.MapLightsOnly && FirelinkPreviewMap == null;

    /// <summary>The loaded DS3 Firelink map selected in the active Map Editor view.</summary>
    public static MapContainer FirelinkPreviewMap
    {
        get
        {
            var project = Smithbox.Orchestrator?.SelectedProject;
            if (!IsGame || project?.Descriptor.ProjectType != ProjectType.DS3)
                return null;

            var map = project.Handler?.MapEditor?.ViewHandler.ActiveView?.Selection.SelectedMapContainer;
            // DS3-Remake ports the DS1 shrine into a DS3 project with its own GPARAM and probes.
            return map is { Name: "m40_00_00_00" or "m10_02_00_00", LoadState: MapContentLoadState.Loaded }
                ? map
                : null;
        }
    }

    public static ViewportTexturingMode Texturing => (ViewportTexturingMode)CFG.Current.Viewport_Texturing_Mode;

    /// <summary>
    ///     The lighting debug view value the flver shader takes for Editor mode, see SceneParam.LightingDebugView. It
    ///     lights the whole surface by the view's sun and the environment map, without shadows or the light loops.
    /// </summary>
    public const uint FullbrightDebugView = 4;

    /// <summary>
    ///     Editor mode for Texturing (Normal). The shader treats it as <see cref="FullbrightDebugView" />, as both
    ///     write the normal-mapped normals.
    /// </summary>
    public const uint FullbrightSurfaceDebugView = 5;

    /// <summary>The fullbright debug view value for this frame</summary>
    public static uint FullbrightView => Texturing == ViewportTexturingMode.Normal
        ? FullbrightSurfaceDebugView
        : FullbrightDebugView;
}
