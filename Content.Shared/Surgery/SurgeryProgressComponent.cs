using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Surgery;

/// <summary>
/// Прогресс операций на конкретном теле: что начато, на каком шаге, что уже сделано.
/// </summary>
/// <remarks>
/// Пока в ядре одна активная операция на тело. Когда появится UI с выбором части тела,
/// сюда добавятся записи по частям, не меняя логику шагов.
/// </remarks>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SurgeryProgressComponent : Component
{
    /// <summary>
    /// Операция, которая сейчас выполняется (если есть).
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<SurgeryPrototype>? Active;

    /// <summary>
    /// Индекс текущего шага активной операции.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int StepIndex;

    /// <summary>
    /// Завершённые операции. Используются полем <c>requires</c> у операций.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<ProtoId<SurgeryPrototype>> Completed = [];

    /// <summary>
    /// Завершена ли операция (с учётом <c>repeatable</c>).
    /// </summary>
    public bool IsCompleted(ProtoId<SurgeryPrototype> surgery)
    {
        return Completed.Contains(surgery);
    }
}
