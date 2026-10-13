# Хирургия (DeadSpace)

Ядро операций: операции добавляются данными (YAML), без правок кода.

- Shared: `Content.Shared/DeadSpace/Surgery` — прототипы, условия, эффекты, состояние, API.
- Server: `Content.Server/DeadSpace/Surgery/SurgerySystem.cs` — взаимодействие, DoAfter, шаги, осложнения.
- Контент: `Resources/Prototypes/_DeadSpace/Surgery/`.
- Локали: `Resources/Locale/{ru-RU,en-US}/_DeadSpace/surgery/surgery.ftl`.

## Как это работает

1. Игрок использует предмет на теле с `SurgeryTarget`.
2. Ядро ищет шаг: активная операция, иначе доступные операции по `priority`.
3. У шага берётся первый вариант, чей инструмент подходит и чьи условия проходят.
4. Играется DoAfter (время делится на `SpeedModifier` инструмента).
5. После шага применяются эффекты и бросаются осложнения; при прерывании — `failEffects`.
6. После последнего шага операция попадает в `completed` (для `requires`) и применяются её эффекты.

Роботы (КПБ и прочие синтетики с `SurgeryImmune`) не оперируются вообще.

## Операция

```yaml
- type: surgery
  id: MyAppendectomy
  name: my-appendectomy
  priority: 0
  requires: [SurgeryOpening]
  repeatable: false
  conditions:
    - !type:SurgeryConditionMobState
      states: [Alive, Critical]
  complications: [SurgeryComplicationInfection]
  steps:
    - SurgeryStepIncision
    - SurgeryStepRetract
    - MyStepCutAppendix
  effects:
    - !type:SurgeryEntityEffect
      target: Patient
      effects:
        - !type:HealthChange
          damage:
            types:
              Slash: -4
```

## Шаг

```yaml
- type: surgeryStep
  id: MyStepCutAppendix
  variants:
    - tool:
        tags: [SurgeryToolScalpel]
      qualities: [Slicing]
      conditions:
        - !type:SurgeryConditionOperatingTable
      duration: 4
      startMessage: my-step-start
      successMessage: my-step-success
      effects:
        - !type:SurgeryEntityEffect
          target: Patient
          effects:
            - !type:ModifyBleed
              amount: 5
      failEffects:
        - !type:SurgeryDirtEffect
          target: Surgeon
          amount: 10
      complications: [SurgeryComplicationJerk]
```

## Осложнение

```yaml
- type: surgeryComplication
  id: MyComplication
  chance: 0.05
  modifiers:
    - conditions:
        - !type:SurgeryConditionEquipment
          slot: gloves
          inverted: true
      add: 0.15
  effects:
    - !type:SurgeryEntityEffect
      target: Patient
      effects:
        - !type:HealthChange
          damage:
            types:
              Poison: 4
  message: my-complication-message
  patientMessage: my-complication-patient
```

## Условия

| Условие | Что проверяет | Поля |
| --- | --- | --- |
| `SurgeryConditionAnatomy` | body-прототип цели (Human, Diona, Skeleton) | `bodies`, `target` |
| `SurgeryConditionEquipment` | предмет в слоте | `slot`, `whitelist`, `target` |
| `SurgeryConditionDirt` | загрязнение | `target`, `threshold` |
| `SurgeryConditionTrashNearby` | мусор/лужи/трупы вокруг | `radius`, `minCount`, `countTrash`, `countPuddles`, `countCorpses` |
| `SurgeryConditionOperatingTable` | пациент пристёгнут к столу | `requireBuckle` |
| `SurgeryConditionMobState` | Alive / Critical / Dead / PreCritical | `states`, `target` |
| `SurgeryConditionSleeping` | спит ли цель | `target` |
| `SurgeryConditionToolQuality` | качества инструмента | `qualities`, `requireAll` |

Любое условие инвертируется полем `inverted: true`.

## Эффекты

| Эффект | Что делает |
| --- | --- |
| `SurgeryEntityEffect` | прогоняет обычные `EntityEffect` (урон, реагенты, статусы) по выбранной сущности |
| `SurgeryDirtEffect` | меняет загрязнение (отрицательное значение отмывает) |

У эффектов есть `probability` и `target` (`Patient`, `Surgeon`, `Tool`, `OperatingTable`).

## Расширение

Новое условие — класс `SurgeryConditionBase<T>` плюс система `SurgeryConditionSystem<T>`.
Новый эффект — `SurgeryEffectBase<T>` плюс `SurgeryEffectSystem<T>`.

Хуки без правки ядра: `SurgeryStartedEvent`, `SurgeryStepCompletedEvent`, `SurgeryCompletedEvent`,
`SurgeryComplicationTriggeredEvent`, `SurgeryComplicationRollEvent` (правка шанса).

## Не сделано

UI выбора операции и части тела, прогресс по частям тела, предсказание на клиенте,
отдельная механика хирургической инфекции.