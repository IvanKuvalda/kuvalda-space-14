using Robust.Shared.GameObjects;

namespace Content.Client.NormalMap;

    /// <summary>
    //      отслеживает, к каким слоям спрайтов в данный момент применяется шейдер с нормальным отображением
    /// </summary>
[RegisterComponent]
public sealed partial class NormalMapVisualsComponent : Component
{
    /// <summary>
    ///     бредовый метод определения состояния спрайта для пропуска мусорного лишнего применения
    /// </summary>
    [ViewVariables] public string? Signature;

    public readonly List<AppliedEntry> Applied = new();

    public sealed record AppliedEntry(string TargetKey, string SourceKey);
}
