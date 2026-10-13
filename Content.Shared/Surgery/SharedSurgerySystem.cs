using Content.Shared.Random.Helpers;
using Content.Shared.Surgery.Conditions;
using Content.Shared.Surgery.Effects;
using Content.Shared.Surgery.Prototypes;
using Content.Shared.Tools.Components;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.Surgery;

/// <summary>
/// Ядро хирургии: сопоставляет инструмент, цель и обстоятельства с прототипами операций.
/// </summary>
/// <remarks>
/// <para>
/// Здесь нет ни одного упоминания конкретной операции, расы или осложнения. Всё, что делает
/// ядро — собирает <see cref="SurgeryContext"/>, проверяет условия (<see cref="SurgeryCondition"/>)
/// и применяет последствия (<see cref="SurgeryEffect"/>). Поэтому новая операция — это YAML.
/// </para>
/// <para>
/// Ядро живёт в Shared, а взаимодействие и DoAfter — в серверном <c>SurgerySystem</c>.
/// Публичное API отсюда позже сможет использовать UI без переписывания логики.
/// </para>
/// </remarks>
public sealed partial class SharedSurgerySystem : EntitySystem, ISurgeryConditionRaiser, ISurgeryEffectRaiser
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    private readonly List<SurgeryPrototype> _surgeries = [];
    private bool _surgeriesDirty = true;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (args.WasModified<SurgeryPrototype>() || args.WasModified<SurgeryStepPrototype>())
            _surgeriesDirty = true;
    }

    /// <summary>
    /// Все операции, отсортированные по приоритету (больше — раньше), затем по ID.
    /// </summary>
    public IReadOnlyList<SurgeryPrototype> Surgeries
    {
        get
        {
            if (_surgeriesDirty)
                RefreshSurgeries();

            return _surgeries;
        }
    }

    private void RefreshSurgeries()
    {
        _surgeries.Clear();

        foreach (var surgery in _prototype.EnumeratePrototypes<SurgeryPrototype>())
        {
            _surgeries.Add(surgery);
        }

        _surgeries.Sort((a, b) =>
        {
            var byPriority = b.Priority.CompareTo(a.Priority);
            return byPriority != 0 ? byPriority : string.CompareOrdinal(a.ID, b.ID);
        });

        _surgeriesDirty = false;
    }

    #region Контекст

    /// <summary>
    /// Собрать контекст операции: кто, над кем, чем и на чём.
    /// </summary>
    public SurgeryContext CreateContext(
        EntityUid patient,
        EntityUid? surgeon = null,
        EntityUid? tool = null,
        SurgeryPrototype? surgery = null,
        SurgeryStepPrototype? step = null,
        int stepIndex = 0)
    {
        EntityUid? table = null;

        if (TryComp<BuckleComponent>(patient, out var buckle) &&
            buckle.BuckledTo is { } strap &&
            HasComp<OperatingTableComponent>(strap))
        {
            table = strap;
        }

        return new SurgeryContext(patient, surgeon, tool, table, surgery, step, stepIndex);
    }

    #endregion

    #region Условия

    /// <summary>
    /// Проверить список условий. Пустой список или null — условия выполнены.
    /// </summary>
    public bool ConditionsMet(SurgeryCondition[]? conditions, SurgeryContext context)
    {
        if (conditions == null)
            return true;

        foreach (var condition in conditions)
        {
            if (!condition.Check(context, this))
                return false;
        }

        return true;
    }

    /// <inheritdoc/>
    public bool RaiseConditionEvent<T>(T condition, SurgeryContext context) where T : SurgeryConditionBase<T>
    {
        var ev = new SurgeryConditionEvent<T>(condition, context);
        RaiseLocalEvent(ref ev);
        return ev.Result;
    }

    #endregion

    #region Подбор шага и варианта

    /// <summary>
    /// Подходит ли инструмент под требования варианта шага.
    /// </summary>
    /// <remarks>
    /// Вариант без требований к инструменту — это шаг голыми руками: он сработает только
    /// без предмета в руке, чтобы случайный предмет не «выполнял» шаг.
    /// </remarks>
    public bool ToolMatches(SurgeryStepVariant variant, EntityUid? tool)
    {
        var needsTool = variant.Tool != null || variant.Qualities.Count > 0;

        if (!needsTool)
            return tool == null;

        if (tool is not { } used)
            return false;

        if (variant.Tool is { } whitelist && !_whitelist.IsValid(whitelist, used))
            return false;

        if (variant.Qualities.Count == 0)
            return true;

        if (!TryComp<ToolComponent>(used, out var toolComponent))
            return false;

        foreach (var quality in variant.Qualities)
        {
            if (!toolComponent.Qualities.Contains(quality.Id))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Выбрать вариант выполнения шага: первый, чей инструмент подходит и чьи условия проходят.
    /// Именно здесь работают развилки: у дионы пройдёт вариант «смола + топор», у человека — «плоть + скальпель».
    /// </summary>
    public bool TryGetVariant(
        SurgeryStepPrototype step,
        EntityUid? tool,
        SurgeryContext context,
        out SurgeryStepVariant variant)
    {
        foreach (var candidate in step.Variants)
        {
            if (!ToolMatches(candidate, tool))
                continue;

            if (!ConditionsMet(candidate.Conditions, context))
                continue;

            variant = candidate;
            return true;
        }

        variant = default!;
        return false;
    }

    /// <summary>
    /// Номер шага, который сейчас должна выполнять операция для этого прогресса.
    /// </summary>
    public bool TryGetStepIndex(SurgeryPrototype surgery, SurgeryProgressComponent progress, out int stepIndex)
    {
        stepIndex = progress.Active == surgery.ID ? progress.StepIndex : 0;
        return stepIndex >= 0 && stepIndex < surgery.Steps.Length;
    }

    /// <summary>
    /// Доступна ли операция на этом теле прямо сейчас (требования, условия, повторяемость).
    /// </summary>
    public bool IsAvailable(
        SurgeryPrototype surgery,
        SurgeryProgressComponent progress,
        EntityUid patient,
        EntityUid? surgeon,
        EntityUid? tool)
    {
        if (progress.Active is { } active && active != surgery.ID)
            return false;

        if (!surgery.Repeatable && progress.IsCompleted(surgery.ID))
            return false;

        foreach (var requirement in surgery.Requires)
        {
            if (!progress.IsCompleted(requirement))
                return false;
        }

        var context = CreateContext(patient, surgeon, tool, surgery);
        return ConditionsMet(surgery.Conditions, context);
    }

    /// <summary>
    /// Найти, какой шаг игрок сейчас может выполнить этим инструментом.
    /// </summary>
    /// <remarks>
    /// Если операция уже начата, продолжаем только её — начатую операцию нельзя незаметно подменить другой.
    /// </remarks>
    public bool TryFindStep(
        EntityUid patient,
        SurgeryProgressComponent progress,
        EntityUid? surgeon,
        EntityUid? tool,
        out SurgeryAttempt attempt)
    {
        attempt = default;

        if (progress.Active is { } activeId && _prototype.TryIndex(activeId, out var activeSurgery))
            return TryFindStepIn(activeSurgery, progress, patient, surgeon, tool, out attempt);

        foreach (var surgery in Surgeries)
        {
            if (!IsAvailable(surgery, progress, patient, surgeon, tool))
                continue;

            if (TryFindStepIn(surgery, progress, patient, surgeon, tool, out attempt))
                return true;
        }

        return false;
    }

    private bool TryFindStepIn(
        SurgeryPrototype surgery,
        SurgeryProgressComponent progress,
        EntityUid patient,
        EntityUid? surgeon,
        EntityUid? tool,
        out SurgeryAttempt attempt)
    {
        attempt = default;

        if (!TryGetStepIndex(surgery, progress, out var stepIndex))
            return false;

        var step = _prototype.Index(surgery.Steps[stepIndex]);
        var context = CreateContext(patient, surgeon, tool, surgery, step, stepIndex);

        if (!TryGetVariant(step, tool, context, out var variant))
            return false;

        attempt = new SurgeryAttempt(surgery, step, variant, stepIndex);
        return true;
    }

    #endregion

    #region Эффекты

    /// <summary>
    /// Применить список последствий с учётом их шансов.
    /// </summary>
    public void ApplyEffects(SurgeryEffect[]? effects, SurgeryContext context, EntityUid? user = null, float scale = 1f)
    {
        if (effects == null)
            return;

        foreach (var effect in effects)
        {
            if (effect.Probability <= 0f)
                continue;

            if (effect.Probability < 1f &&
                !SharedRandomExtensions.PredictedProb(
                    _timing,
                    effect.Probability,
                    GetNetEntity(context.Patient),
                    GetNetEntity(user)))
            {
                continue;
            }

            effect.RaiseEvent(context, this, scale, user);
        }
    }

    /// <inheritdoc/>
    public void RaiseEffectEvent<T>(T effect, SurgeryContext context, float scale, EntityUid? user)
        where T : SurgeryEffectBase<T>
    {
        var ev = new SurgeryEffectEvent<T>(effect, context, scale, user);
        RaiseLocalEvent(ref ev);
    }

    #endregion

    #region Осложнения

    /// <summary>
    /// Итоговый шанс осложнения: база плюс модификаторы, чьи условия выполнены.
    /// </summary>
    /// <remarks>
    /// Перед возвратом поднимается <see cref="SurgeryComplicationRollEvent"/>, так что другие
    /// системы могут поправить шанс, не трогая ядро.
    /// </remarks>
    public float GetComplicationChance(SurgeryComplicationPrototype complication, SurgeryContext context)
    {
        var chance = complication.Chance;

        foreach (var modifier in complication.Modifiers)
        {
            if (!ConditionsMet(modifier.Conditions, context))
                continue;

            chance = (chance + modifier.Add) * modifier.Multiply;
        }

        var ev = new SurgeryComplicationRollEvent(context, complication) { Chance = chance };
        RaiseLocalEvent(ref ev);

        return Math.Clamp(ev.Chance, 0f, 1f);
    }

    #endregion

    #region Загрязнение

    /// <summary>
    /// Текущее загрязнение сущности (0, если компонента нет).
    /// </summary>
    public float GetDirt(EntityUid uid, SurgeryDirtinessComponent? dirt = null)
    {
        return Resolve(uid, ref dirt, false) ? dirt.Amount : 0f;
    }

    /// <summary>
    /// Изменить загрязнение сущности.
    /// </summary>
    public void AddDirt(EntityUid uid, float amount)
    {
        if (MathHelper.CloseTo(amount, 0f))
            return;

        var dirt = EnsureComp<SurgeryDirtinessComponent>(uid);
        dirt.Amount = Math.Clamp(dirt.Amount + amount, 0f, dirt.Max);
        Dirty(uid, dirt);
    }

    #endregion
}
