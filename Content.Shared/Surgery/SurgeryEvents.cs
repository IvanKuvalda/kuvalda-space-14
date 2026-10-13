using Content.Shared.Surgery.Prototypes;

namespace Content.Shared.Surgery;

/// <summary>
/// Операция начата (первый шаг).
/// </summary>
[ByRefEvent]
public record struct SurgeryStartedEvent(SurgeryContext Context);

/// <summary>
/// Шаг операции успешно завершён.
/// </summary>
[ByRefEvent]
public readonly record struct SurgeryStepCompletedEvent(SurgeryContext Context);

/// <summary>
/// Все шаги операции пройдены.
/// </summary>
[ByRefEvent]
public readonly record struct SurgeryCompletedEvent(SurgeryContext Context);

/// <summary>
/// Осложнение сработало.
/// </summary>
[ByRefEvent]
public readonly record struct SurgeryComplicationTriggeredEvent(
    SurgeryContext Context,
    SurgeryComplicationPrototype Complication,
    float Chance);

/// <summary>
/// Событие-хук перед броском на осложнение: любой другой код может поменять <see cref="Chance"/>,
/// не трогая ядро (например, добавить бонус за мед-навык или штраф за состояние пациента).
/// </summary>
[ByRefEvent]
public record struct SurgeryComplicationRollEvent(
    SurgeryContext Context,
    SurgeryComplicationPrototype Complication)
{
    /// <summary>
    /// Итоговый шанс срабатывания осложнения, 0..1.
    /// </summary>
    public float Chance;
}
