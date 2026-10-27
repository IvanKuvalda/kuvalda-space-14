using Robust.Client.GameObjects;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.Utility;

namespace Content.Client.NormalMap;

// THE CLOSEST THING TO THE OPPOSITE OF Him A HIVEMIND THAT USES DISGUISE TO ENSLAVE THE WEAK INJECTS ITSELF INTO YOUR SOUL GRACE LEFT ROTTED A STAB WHOSE PAIN YOU DON'T FEEL BUT He DOES THE ONLY CURE? RETALLIATION.
public sealed class NormalMapVisualsSystem : EntitySystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly IResourceManager _res = default!;

    private const string ShaderId = "NormalMapped";
    private const string TargetKeyPrefix = "normalmap-target-";
    private const string SourceKeyPrefix = "normalmap-source-";
    private const string NormalSuffix = "_normal";

    private readonly Dictionary<(ResPath RsiPath, string State), bool> _probeCache = new();
    private readonly HashSet<EntityUid> _queued = new();
    private float _scanTimer;

    public override void Initialize()
    {
        // (SpriteComponent, ComponentInit) уже занят SpriteSystem — шина запрещает
        // вторую подписку на ту же пару (компонент, событие), поэтому новые спрайты
        // ищем лёгким опросом раз в полсекунды, а изменения ловим эппирансом.
        SubscribeLocalEvent<SpriteComponent, AppearanceChangeEvent>(OnAppearance);
    }

    private void OnAppearance(Entity<SpriteComponent> ent, ref AppearanceChangeEvent args)
    {
        Queue(ent);
    }

    private void Queue(Entity<SpriteComponent> ent)
    {
        // откладывается на один тик, чтобы визуализаторы сначала завершили настройку слоев
        _queued.Add(ent);
    }

    public override void Update(float frameTime)
    {
        _scanTimer += frameTime;
        if (_scanTimer >= 0.5f)
        {
            _scanTimer = 0;
            var query = AllEntityQuery<SpriteComponent>();
            while (query.MoveNext(out var uid, out _))
            {
                if (!HasComp<NormalMapVisualsComponent>(uid))
                    _queued.Add(uid);
            }
        }

        foreach (var uid in _queued)
        {
            if (!Deleted(uid) && TryComp<SpriteComponent>(uid, out var sprite))
                Apply((uid, sprite));
        }

        _queued.Clear();
    }

    private void Apply(Entity<SpriteComponent> entity)
    {
        Entity<SpriteComponent?> ent = new(entity.Owner, entity.Comp);
        var comp = EnsureComp<NormalMapVisualsComponent>(entity);

        // фоткаем слои, а во время этого мы вставляем слои параметров
        var layerIndices = new List<int>();
        var i = 0;
        foreach (var _ in entity.Comp.AllLayers)
            layerIndices.Add(i++);

        //  избыточная работа, наверное
        var sb = new System.Text.StringBuilder();
        foreach (var index in layerIndices)
        {
            var rsi = _sprite.LayerGetEffectiveRsi(ent, index);
            var state = _sprite.LayerGetRsiState(ent, index);
            sb.Append(rsi?.Path.ToString() ?? "-").Append(':').Append(state.Name ?? "-").Append(';');
        }

        var signature = sb.ToString();
        if (comp.Signature == signature)
            return;
        comp.Signature = signature;

        // сносим ранее примененные исходные слои, и восстанавиваем шейдеры целевого слоя
        foreach (var applied in comp.Applied)
        {
            if (_sprite.LayerMapTryGet(ent, applied.TargetKey, out var targetIdx, false))
                entity.Comp.LayerSetShader(targetIdx, (Robust.Client.Graphics.ShaderInstance?) null);
            _sprite.RemoveLayer(ent, applied.SourceKey, false);
        }
        comp.Applied.Clear();

        foreach (var index in layerIndices)
        {
            var layer = entity.Comp[index];
            if (!layer.Visible)
                continue;

            var rsi = _sprite.LayerGetEffectiveRsi(ent, index);
            if (rsi == null)
                continue;

            var stateName = _sprite.LayerGetRsiState(ent, index).Name;
            if (stateName == null)
                continue;

            if (!HasNormalFile(rsi.Path, stateName))
                continue;

            var targetKey = $"{TargetKeyPrefix}{index}";
            var sourceKey = $"{SourceKeyPrefix}{index}";

            _sprite.AddLayer(ent, new PrototypeLayerData
            {
                TexturePath = (rsi.Path / $"{stateName}{NormalSuffix}.png").ToString(),
                CopyToShaderParameters = new PrototypeCopyToShaderParameters
                {
                    LayerKey = targetKey,
                    ParameterTexture = "normalMap",
                    ParameterUV = "normalUV",
                },
            }, index);
            _sprite.LayerMapSet(ent, sourceKey, index);
            _sprite.LayerMapSet(ent, targetKey, index + 1);
            entity.Comp.LayerSetShader(index + 1, ShaderId);

            comp.Applied.Add(new NormalMapVisualsComponent.AppliedEntry(targetKey, sourceKey));
        }
    }

    private bool HasNormalFile(ResPath rsiPath, string state)
    {
        var key = (rsiPath, state);
        if (_probeCache.TryGetValue(key, out var cached))
            return cached;

        var has = _res.ContentFileExists(rsiPath / $"{state}{NormalSuffix}.png");
        _probeCache[key] = has;
        return has;
    }
}
