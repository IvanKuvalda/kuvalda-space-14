// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.DeadSpace.OrganPrinter;

[Serializable, NetSerializable]
public enum OrganPrinterUiKey : byte
{
    Key,
}

/// <summary>
/// Запись о печатаемом органе для UI.
/// </summary>
[Serializable, NetSerializable]
public sealed record OrganPrinterProduct(EntProtoId Product, int Cost);

[Serializable, NetSerializable]
public sealed class OrganPrinterUpdateState : BoundUserInterfaceState
{
    public List<OrganPrinterProduct> Products;

    /// <summary>
    /// Доступно биомассы в хранилище машины.
    /// </summary>
    public int Biomass;

    public OrganPrinterUpdateState(List<OrganPrinterProduct> products, int biomass)
    {
        Products = products;
        Biomass = biomass;
    }
}

/// <summary>
/// Запрос на печать органа после мини-игры.
/// Качество (0-100) влияет на отходы биомассы: чем ниже, тем больше расход.
/// </summary>
[Serializable, NetSerializable]
public sealed class OrganPrinterPrintRequestMessage : BoundUserInterfaceMessage
{
    public readonly EntProtoId Product;
    public readonly int Quality;

    public OrganPrinterPrintRequestMessage(EntProtoId product, int quality)
    {
        Product = product;
        Quality = quality;
    }
}

/// <summary>
/// Сообщение о провале мини-игры: принтер повреждается. Быстрая печать бьёт сильнее.
/// </summary>
[Serializable, NetSerializable]
public sealed class OrganPrinterGameFailedMessage : BoundUserInterfaceMessage
{
    public readonly bool FastMode;

    public OrganPrinterGameFailedMessage(bool fastMode)
    {
        FastMode = fastMode;
    }
}
