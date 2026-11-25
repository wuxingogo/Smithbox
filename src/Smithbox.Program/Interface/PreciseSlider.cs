using Hexa.NET.ImGui;
using System.Globalization;
using System.Numerics;

namespace StudioCore.Application;

public static class PreciseSlider
{
    private static string _draft = "";
    private static bool _applied;
    private static CultureInfo NumberCulture => CultureInfo.InvariantCulture;

    public static bool IsCommitted() => _applied || ImGui.IsItemDeactivatedAfterEdit();

    public static bool SliderFloat(string label, ref float value, float min, float max,
        string format = "%.3f", ImGuiSliderFlags flags = ImGuiSliderFlags.None)
    {
        var changed = ImGui.SliderFloat(label, ref value, min, max, format, flags);
        _applied = NumericPopup(label, ref value, min, max, flags);
        return changed || _applied;
    }

    public static bool SliderFloat(string label, ref float value, float min, float max, ImGuiSliderFlags flags)
        => SliderFloat(label, ref value, min, max, "%.3f", flags);

    public static bool SliderInt(string label, ref int value, int min, int max,
        string format = "%d", ImGuiSliderFlags flags = ImGuiSliderFlags.None)
    {
        var changed = ImGui.SliderInt(label, ref value, min, max, format, flags);
        _applied = NumericPopup(label, ref value, min, max, flags);
        return changed || _applied;
    }

    public static bool SliderInt(string label, ref int value, int min, int max, ImGuiSliderFlags flags)
        => SliderInt(label, ref value, min, max, "%d", flags);

    public static bool SliderFloat3(string label, ref Vector3 value, float min, float max,
        string format = "%.3f", ImGuiSliderFlags flags = ImGuiSliderFlags.None)
    {
        var changed = ImGui.SliderFloat3(label, ref value, min, max, format, flags);
        _applied = false;
        if (BeginEditor(label, flags, out var item, out var opened))
        {
            if (opened)
                _draft = string.Create(NumberCulture, $"{value.X:R}, {value.Y:R}, {value.Z:R}");

            ImGui.InputText($"{LOC.Get("GUI_PreciseSlider_Vector")}##preciseValue", ref _draft, 128);
            var parts = _draft.Split(',');
            Vector3 next = default;
            var valid = parts.Length == 3 && TryNumber(parts[0], min, max, out next.X)
                && TryNumber(parts[1], min, max, out next.Y) && TryNumber(parts[2], min, max, out next.Z);
            if (DrawButtons(valid))
            {
                _applied = value != next;
                value = next;
            }
            ImGui.EndPopup();
        }
        EndEditor(item);
        return changed || _applied;
    }

    private static bool NumericPopup<T>(string label, ref T value, T min, T max, ImGuiSliderFlags flags) where T : INumber<T>
    {
        var changed = false;
        if (BeginEditor(label, flags, out var item, out var opened))
        {
            changed = EditNumber(ref value, min, max, opened);
            ImGui.EndPopup();
        }
        EndEditor(item);
        return changed;
    }

    private static bool BeginEditor(string label, ImGuiSliderFlags flags, out ImGuiLastItemData item, out bool opened)
    {
        item = ImGui.GetCurrentContext().LastItemData;
        ImGui.PushID(unchecked((int)ImGui.GetID(label)));
        opened = (flags & ImGuiSliderFlags.NoInput) == 0 && ImGui.IsItemHovered()
            && ImGui.IsMouseClicked(ImGuiMouseButton.Right);
        if (opened)
            ImGui.OpenPopup("##preciseSlider");
        return ImGui.BeginPopup("##preciseSlider");
    }

    private static void EndEditor(ImGuiLastItemData item)
    {
        ImGui.PopID();
        // Callers still query the slider for tooltips and drag state after drawing the popup.
        ImGui.GetCurrentContext().LastItemData = item;
    }

    public static bool EditNumber<T>(ref T value, T min, T max, bool initialize) where T : INumber<T>
    {
        if (initialize)
            _draft = value.ToString(null, NumberCulture);
        ImGui.InputText("##preciseValue", ref _draft, 128);
        var valid = TryNumber(_draft, min, max, out var next);
        if (!DrawButtons(valid) || value == next)
            return false;
        value = next;
        return true;
    }

    private static bool TryNumber<T>(string text, T min, T max, out T value) where T : INumber<T>
        => T.TryParse(text, NumberCulture, out value) && T.IsFinite(value)
            && value >= T.Min(min, max) && value <= T.Max(min, max);

    private static bool DrawButtons(bool valid)
    {
        ImGui.BeginDisabled(!valid);
        var apply = ImGui.Button(LOC.Get("GUI_PreciseSlider_Apply"));
        ImGui.EndDisabled();
        ImGui.SameLine();
        var cancel = ImGui.Button(LOC.Get("GUI_PreciseSlider_Cancel"));
        if (apply || cancel)
            ImGui.CloseCurrentPopup();
        return apply;
    }
}
