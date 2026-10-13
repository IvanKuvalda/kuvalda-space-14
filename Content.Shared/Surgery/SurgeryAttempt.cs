using Content.Shared.Buckle.Components;
using Content.Shared.Surgery.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared.Surgery;

/// <summary>
/// Результат подбора шага: какую операцию и каким вариантом сейчас выполняет игрок.
/// </summary>
public readonly record struct SurgeryAttempt(
    SurgeryPrototype Surgery,
    SurgeryStepPrototype Step,
    SurgeryStepVariant Variant,
    int StepIndex);
