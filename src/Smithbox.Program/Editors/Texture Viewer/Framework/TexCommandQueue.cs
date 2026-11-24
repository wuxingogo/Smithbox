using StudioCore.Application;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace StudioCore.Editors.TextureViewer;

public class TexCommandQueue
{
    public TextureViewerScreen Editor;
    public ProjectEntry Project;

    public bool DoFocus = false;

    public TexCommandQueue(TextureViewerScreen editor, ProjectEntry project)
    {
        Editor = editor;
        Project = project;
    }

    /// <summary>
    /// Parse the editor command queue for this editor.
    /// </summary>
    public void Parse(string[] initcmd)
    {
        if (initcmd != null && initcmd.Length > 1)
        {
            // View Image:
            // e.g. "texture/view/01_common/SB_GarageTop_04"
            if (initcmd[0] == "view" && initcmd.Length >= 3)
            {
                var fileName = initcmd[1];
                var textureName = initcmd[2];

                HandleView(fileName, textureName, initcmd.Length > 3 ? initcmd[3] : null);
            }
        }
    }

    public static FileDictionaryEntry FindContainerEntry(ProjectEntry project, string containerPath)
    {
        if (string.IsNullOrEmpty(containerPath))
            return null;

        var key = NormalizeContainerPath(containerPath.Replace(".tpfbdt", ".tpfbhd", StringComparison.OrdinalIgnoreCase));

        return project.Locator.TextureFiles.Entries?.FirstOrDefault(e => NormalizeContainerPath(e.Path) == key)
            ?? project.Locator.TexturePackedFiles.Entries?.FirstOrDefault(e => NormalizeContainerPath(e.Path) == key);
    }

    private static string NormalizeContainerPath(string path) =>
        (path ?? "").Replace('\\', '/').TrimStart('/').ToLowerInvariant();

    public void HandleView(string filename, string textureName, string containerPath = null)
    {
        var activeView = Editor.ViewHandler.ActiveView;

        if (activeView == null)
            return;

        // Packed (.tpfbhd) containers are listed apart from the others
        var targetFile = string.IsNullOrEmpty(containerPath)
            ? Project.Locator.TextureFiles.Entries.FirstOrDefault(e => e.Filename == filename)
                ?? Project.Locator.TexturePackedFiles.Entries?.FirstOrDefault(e => e.Filename == filename)
            : FindContainerEntry(Project, containerPath);

        if (targetFile == null)
            return;

        var bank = Project.Handler.TextureData.PrimaryBank;
        var isPacked = string.Equals(targetFile.Extension, "tpfbhd", StringComparison.OrdinalIgnoreCase);
        Task<bool> loadTask = isPacked ? bank.LoadPackedTextureBinder(targetFile) : bank.LoadTextureBinder(targetFile);

        Task.WaitAll(loadTask);

        var containers = isPacked ? bank.PackedEntries : bank.Entries;
        var targetBinder = containers.FirstOrDefault(e => string.Equals(e.Key.Path, targetFile.Path, StringComparison.OrdinalIgnoreCase));

        if (targetBinder.Key == null || targetBinder.Value == null)
            return;

        activeView.Movie.Close();
        activeView.Selection.ResetSelection();
        activeView.Selection.AutoSelectTpf = false;
        activeView.TextureList.SelectAndLoad(null);
        activeView.Selection.SelectTextureFile(targetBinder.Key, targetBinder.Value);

        // Prefer the TPF named after the container if several TPFs have the same texture.
        var files = targetBinder.Value.Files;
        var preferred = files.FirstOrDefault(entry =>
            string.Equals(entry.Key.Name, filename, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(Path.GetFileNameWithoutExtension(entry.Key.Name ?? ""), filename, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrEmpty(textureName))
        {
            var match = preferred.Value?.Textures.FirstOrDefault(t =>
                string.Equals(t.Name, textureName, StringComparison.OrdinalIgnoreCase));
            if (match != null)
            {
                activeView.Selection.SelectTpfFile(preferred.Key, preferred.Value);
                activeView.TextureList.SelectAndLoad(match);
                return;
            }
            foreach (var file in files)
            {
                match = file.Value.Textures.FirstOrDefault(t =>
                    string.Equals(t.Name, textureName, StringComparison.OrdinalIgnoreCase));
                if (match != null)
                {
                    activeView.Selection.SelectTpfFile(file.Key, file.Value);
                    activeView.TextureList.SelectAndLoad(match);
                    return;
                }
            }
        }

        var fallback = preferred.Key != null ? preferred : files.Count == 1 ? files.First() : default;
        if (fallback.Key != null)
            activeView.Selection.SelectTpfFile(fallback.Key, fallback.Value);
    }
}
