# Архитектура хирургии (DeadSpace)

## Слои

```
Resources/Prototypes/_DeadSpace/Surgery/*.yml     контент: операции, шаги, осложнения
                 | ProtoId / !type
                 v
Content.Shared/DeadSpace/Surgery                  правила
  Prototypes/   SurgeryPrototype, SurgeryStepPrototype(+Variant), SurgeryComplicationPrototype(+Modifier)
  Conditions/   SurgeryCondition + 8 условий
  Effects/      SurgeryEffect + 2 эффекта
  SharedSurgerySystem   API: подбор шага, условия, эффекты, шанс, загрязнение
  Компоненты    SurgeryTarget/Progress/Dirtiness/DirtCleaner/Immune, OperatingTable
                 | события Surgery*Event
                 v
Content.Server/DeadSpace/Surgery/SurgerySystem    действия: InteractUsing/InteractHand, DoAfter, попапы, звук
                 |
                 v
              игрок
```

Правило разделения: shared решает, что возможно, сервер делает это в мире. Shared не знает ни одной
конкретной операции, сервер не знает ни одного конкретного условия — оба работают с прототипами.

## Один шаг по шагам

1. `InteractUsingEvent` / `InteractHandEvent` приходит на тело с `SurgeryTargetComponent`.
2. `SurgerySystem.TryStartSurgery` берёт `SurgeryProgressComponent` (может не быть) и зовёт
   `SharedSurgerySystem.TryFindStep`.
3. `TryFindStep`: если есть `Active` — продолжаем только её; иначе перебираем `Surgeries`
   (сортировка по `Priority`, затем ID) и для каждой проверяем `IsAvailable`
   (`Repeatable`, `Requires`, `Conditions`).
4. Для шага `TryGetVariant` берёт первый вариант, где `ToolMatches` (инструмент + качества) и
   `ConditionsMet` (условия варианта).
5. Сервер считает `Duration / SpeedModifier`, поднимает `StartMessage` и запускает DoAfter с
   payload `SurgeryDoAfterEvent { Surgery, StepIndex, Tool }`.
6. На завершении DoAfter шаг перепроверяется (`TryGetStepIndex` + `TryGetVariant`): за время
   анимации могли смениться инструмент, условия или прогресс.
7. Применяются `Effects` варианта (или `FailEffects` при прерывании), звук, попап.
8. `RollComplications`: осложнения операции, шага и варианта; шанс = `GetComplicationChance`.
9. `Advance`: либо `StepIndex++`, либо `Active = null`, операция в `Completed`, эффекты операции,
   событие `SurgeryCompletedEvent`.

## Условия

Полиморфные данные + событийная диспетчеризация (тот же приём, что у `EntityCondition` в движке).

- `SurgeryCondition` — абстрактный `[ImplicitDataDefinitionForInheritors]` класс; в YAML `!type:...`.
- `Check(ctx, raiser)` = `Inverted != RaiseEvent(...)`, поэтому «нет перчаток» и «не на столе» —
  те же условия с `inverted: true`, а не отдельные классы.
- `SharedSurgerySystem` реализует `ISurgeryConditionRaiser` и поднимает `SurgeryConditionEvent<T>`
  (`[ByRefEvent]`), который несёт и условие, и полный `SurgeryContext`.
- Каждое условие — своя система `SurgeryConditionSystem<T>`; подписка глобальная (без компонента-цели),
  потому что проверять надо пациента, хирурга, инструмент и стол сразу.
- Новое условие = один файл (класс + система), ядро не трогается.

`SurgeryContext` — `readonly record struct` с `Patient/Surgeon/Tool/OperatingTable/Surgery/Step/StepIndex`
и селектором `SurgeryEntityTarget` (`Patient`, `Surgeon`, `Tool`, `OperatingTable`).

## Эффекты

Та же схема (`SurgeryEffect` / `SurgeryEffectEvent<T>` / `SurgeryEffectSystem<T>` / `ISurgeryEffectRaiser`), плюс:

- `SurgeryEntityEffect` — обёртка: прогоняет обычные `EntityEffect[]` через `SharedEntityEffectsSystem`
  по выбранной сущности. Весь существующий контент (урон, `ModifyBleed`, реагенты, статусы) доступен
  операциям без нового кода.
- `SurgeryDirtEffect` — загрязнение; `EnsureComp<SurgeryDirtinessComponent>`, поэтому работает
  на любом мобе или предмете, даже если компонента не было в прототипе.
- У эффектов есть `probability` (0..1) и `target`.

## Состояние

| Компонент | Где | Зачем |
| --- | --- | --- |
| `SurgeryTarget` | все виды (`BaseMobSpecies`) | точка подписки на взаимодействие |
| `SurgeryImmune` | КПБ и синтетики (`BaseMobSpeciesComputer`) | запрет операций: `IsOperable` возвращает false |
| `SurgeryProgress` | пациент | `Active`, `StepIndex`, `Completed` (для `requires`) |
| `SurgeryDirtiness` | органики, инструмент, стол | число загрязнения: читают условия, меняют эффекты |
| `SurgeryDirtCleaner` | мыло и прочее | смывает загрязнение |
| `OperatingTable` | операционный стол | условие «на столе» и попадание в контекст |

## Хуки расширения

- События: `SurgeryStartedEvent`, `SurgeryStepCompletedEvent`, `SurgeryCompletedEvent`,
  `SurgeryComplicationTriggeredEvent`.
- `SurgeryComplicationRollEvent` — `[ByRefEvent]` перед броском: любой код может поправить `Chance`
  (навык, состояние пациента, наркоз) без правок ядра.

## Инварианты

- Активная операция блокирует выбор других: начатое нельзя незаметно подменить.
- DoAfter хранит операцию, номер шага и инструмент; на завершении всё перепроверяется.
- `TryFindStep` не вешает компонент прогресса: пустой инстанс живёт один вызов, компонент появляется
  только когда шаг реально начался.
- Ошибка данных падает на загрузке прототипов (неизвестный `!type`, тег, `ProtoId`), а не в бою.

## Осознанные ограничения

1. Одна активная операция на тело; прогресса по частям тела нет (это слой UI и частей).
2. Хирургической инфекции как механики нет: осложнение наносит урон-заглушку.
3. Предсказание на клиенте отсутствует — обработчики живут на сервере.
4. Загрязнение не спадает само (нет decay) и живёт в одном числе.
5. Инструменты различаются тегами `SurgeryTool*`; новый инструмент нужно пометить тегом.
6. Автотестов нет: проверено сборкой Shared/Server/Client и загрузкой прототипов на живом сервере.