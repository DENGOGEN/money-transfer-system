# CONFLICT_RESOLUTION.md

## Информация о проекте
- **Проект:** Money Transfer System
- **Язык:** C# / WPF
- **IDE:** Visual Studio 2022

## Описание конфликта

| Параметр | Значение |
|----------|----------|
| **Дата** | 2026-06-10 |
| **Файл** | `Commission.cs` |
| **Ветка A (develop)** | `public static decimal P2P = 0.0m;` |
| **Ветка B (hotfix/logging-fix)** | `public static decimal P2P = 0.01m;` |
| **Причина** | Параллельное изменение одной строки кода |

## Решение

Выбрано значение `0.0m` (0% комиссии) для P2P-переводов, так как:
- По бизнес-требованиям внутренние переводы должны быть без комиссии
- Комиссия 1% взимается только для C2C-переводов на банковские карты

## Итоговый код

```csharp
public static class Commission 
{ 
    public static decimal P2P = 0.0m; 
}
