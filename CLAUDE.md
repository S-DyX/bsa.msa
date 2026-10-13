# Bsa.Msa

Библиотека для построения микросервисов поверх брокера RabbitMQ: шина сообщений, подписчики-обработчики, периодические команды и хостинг сервисов через DI (Autofac или Microsoft.Extensions.DependencyInjection). Пакеты публикуются в NuGet.

## Проекты

| Проект | Папка | Назначение |
|---|---|---|
| `Bsa.Msa.RabbitMq.Core` | `Bsa.Msa.RabbitMq.Core/` | Ядро: шина, подключение к RabbitMQ, подписчики, команды, настройки |
| `Bsa.Msa.Autofac` | `Bsa.Msa.Autofac/` | Интеграция с Autofac (`AutofacContainerInstaller`, `ServiceHost`) |
| `Bsa.Msa.SC` | `Bsa.Msa.Ms/` | Интеграция с `IServiceCollection` (пространство имён `Bsa.Msa.DependencyInjection`) |
| `Bsa.Msa.Example.Host` | `Hosts/Bsa.Msa.Example.Host/` | Пример хоста: обработчики, команды, `services.json`, `appsettings.json` |

## Платформа

- Библиотеки — **.NET 10** (`net10.0`), версии пакетов `10.0.x`. Пример хоста — `net8.0`.
- `Bsa.Msa.Autofac` и `Bsa.Msa.SC` ссылаются на ядро **как на NuGet-пакет** (`Bsa.Msa.RabbitMq.Core`), а не через `ProjectReference`. Изменения в ядре попадают в них только после публикации пакета и обновления версии.
- `AllowUnsafeBlocks` в ядре включён.
- Меняя публичный API пакета — поднять `<Version>` и заполнить `<PackageReleaseNotes>` в `.csproj`.

## Сборка

```
dotnet build Bsa.Msa.sln
```

Тестовых проектов нет. Для проверки — пример хоста (нужен запущенный RabbitMQ, настройки в `appsettings.json`, секция `rabbitMq`).

## Что не читать и не трогать

- `Bsa.Msa.RabbitMq.Core/RabbitMQ.Client/` — встроенные исходники клиента RabbitMQ (VMware, Apache 2.0 / MPL 2.0). Не изменять без задачи, правила оформления на них не распространяются.
- `Bsa.Msa.RabbitMq.Core/Old/` — устаревшие версии (код закомментирован). Не править и не использовать как образец.
- `*.Backup.tmp`, `Bsa - Backup.*.csproj` — резервные копии.
- Бинарники и служебное: `bin/`, `obj/`, `.vs/`, `*.dll`, `*.nupkg`.

## Архитектура ядра

- **Шина.** `IBusManager` / `BusManager` — основной API для отправки (`Send`), публикации (`Publish`), RPC (`Respond`), удаления и очистки очередей. `ISingleRmqBus` — обёртка над `IBusManager`. `ISimpleBus` / `SimpleBus` — работа с каналом RabbitMQ, `ISimpleConnection` / `SimpleConnection` — подключение и переподключение. Имена очередей и exchange — `ISimpleBusNaming` (`DefaultSimpleBusNaming`).
- **Локальная шина.** `ILocalBus` / `LocalBus`: если обработчик очереди зарегистрирован в текущем процессе, сообщение доставляется ему напрямую, минуя брокер.
- **Обработчики.** `IMessageHandler<TMessage>` (`Handle`) и `IMessageHandlerAsync<TMessage>` (`HandleAsync`). Создаются через `IMessageHandlerFactory` по имени типа из `services.json`. Подписка — `SubscriberBase<TMessage>` (`ISubscriber`), фабрика — `SubscriberFactory`.
- **Команды.** `ICommand` (`Execute`) — периодические задачи; запускаются повторителями из `Common/Repeaters` по `period` / `start` из `services.json`.
- **Сервисы.** `ServiceUnitManager` (`IServiceUnitManager`) читает `services.json` (`IServicesSettings`): список сервисов, у каждого — `commands` и `handlers` (`subscriptionEndpoint`, `degreeOfParallelism`, `useExchange`, `retry`, `retryCount`, `ttl`, `clearAfterStart`). Запуск и остановка — `ServiceHost` (`IHostedService`) в проектах интеграции.
- **Настройки.** `SettingsBase` и `RabbitMqSettings` (секция `rabbitMq` в `appsettings.json`; несколько хостов через `;`).
- Пространства имён ядра: `Bsa.Msa.RabbitMq.Core.*` и `Bsa.Msa.Common.*` (общие сервисы, обработчики, команды).

## Оформление кода

Главное правило — единообразие во всём проекте. Новый и изменённый код оформляется по правилам ниже,
даже если соседний код написан иначе. Массово переформатировать чужой код без задачи не нужно.

### Особенности проекта

- Файлы `.cs`: UTF-8 **с BOM**, переводы строк **CRLF**, отступ — табы. При правке сохранять кодировку и концы строк.
- Сообщения исключений и логов — на английском.
- Публичный API пакетов не ломать: меняя поведение, сохранять сигнатуры `IBusManager`, `ISimpleBus`, `IMessageHandler*`, `ICommand` и публичных классов.
- Diff без «шума» CRLF: `git -c core.autocrlf=true diff`.

### Классы и члены

- Класс без наследников помечается `sealed`.
- Порядок в классе: сначала приватные поля, затем константы, конструкторы, свойства, методы.
- Приватные поля начинаются с подчёркивания: `_camelCase`, по возможности `readonly`.
- Зависимости передаются через конструктор.
- Больше 5 параметров в методе — создать DTO и передавать его.
- Метод не длиннее экрана, примерно 50 строк. Длинный метод разбить на приватные методы.
- Не дублировать логику: одно действие делается одним способом, общий код выносится в хелпер/сервис.

### Именование

- PascalCase: типы (классы, интерфейсы, структуры, enum), пространства имён, методы, свойства, события.
- Константы (поля и локальные) — тоже PascalCase: `private const int MaxBatchSize = 500;`
- Интерфейсы начинаются с `I`: `IUserRepository`.
- В имени свойства не повторять имя класса: `User.Name`, а не `User.UserName`.
- Без венгерской нотации: `name`, а не `strName`; `users`, а не `lstUsers`.
- Имена точно отражают назначение; без непонятных сокращений.
- Async-методы — с суффиксом `Async`, `CancellationToken` последним параметром, `ConfigureAwait(false)` в библиотеках.
- Имя файла совпадает с именем класса: `UserService.cs`. Один публичный тип — один файл.

### Форматирование

- Отступ — 1 таб на уровень вложенности. Переводы строк — CRLF.
- Фигурные скобки в стиле Allman: открывающая и закрывающая — каждая на своей строке.
- `if` (IDE0011): либо целиком в одну строку, если помещается, либо с фигурными скобками.
  Тело `if` на следующей строке без скобок — запрещено.
- Пробел после запятой в аргументах и параметрах; бинарные операторы окружены пробелами: `x = y + z`.
- Длина строки — стремиться к 65 символам. Длинные вызовы переносить по аргументам.
- Между методами и свойствами — минимум одна пустая строка.
- `using` в начале файла: сначала `System.*`, затем остальные. Неиспользуемые `using` удалять.
- Пространства имён блочные: `namespace X { … }`.

### Комментарии

- Все комментарии в коде (`//`, `/* */`) и XML-документация (`///`, `<summary>`, `<param>`, `<returns>`) — **на английском**.
- XML-документация обязательна для всех публичных типов и членов (классы, конструкторы, свойства, методы, поля, enum).
- Реализация интерфейса или переопределение — `/// <inheritdoc/>`
  или `/// <inheritdoc cref="IXxx"/>`, если источник нужно указать явно. Текст не дублировать.
- `//` — короткие пояснения в коде. `/* */` — только для больших многострочных блоков текста.
- Комментарий объясняет «почему», а не пересказывает код.

### Пример

```csharp
using System;
using System.Threading.Tasks;
using Bsa.Msa.Common;
using Bsa.Msa.Common.Services.MessageHandling;
using Bsa.Msa.RabbitMq.Core.Interfaces;

namespace Bsa.Msa.Example.Host.Handlers
{
	/// <summary>
	/// Forwards order messages to the billing queue.
	/// </summary>
	public sealed class OrderMessageHandler : IMessageHandlerAsync<OrderMessage>
	{
		private readonly IBusManager _busManager;

		private readonly ILocalLogger _logger;

		private const string BillingQueue = "billing.orders";

		/// <summary>
		/// Creates an instance of <see cref="OrderMessageHandler"/>.
		/// </summary>
		/// <param name="busManager">Message bus.</param>
		/// <param name="logger">Logger.</param>
		public OrderMessageHandler(
			IBusManager busManager,
			ILocalLogger logger)
		{
			_busManager = busManager;
			_logger = logger;
		}

		/// <inheritdoc/>
		public Task HandleAsync(OrderMessage message)
		{
			ArgumentNullException.ThrowIfNull(message);

			if (message.OrderId <= 0) return Task.CompletedTask;

			if (message.Items.Count == 0)
			{
				_logger.Info($"Order {message.OrderId} has no items");
				return Task.CompletedTask;
			}

			_busManager.Send(BillingQueue, message);
			return Task.CompletedTask;
		}
	}
}
```

## Многопоточность

- Обработчики с `degreeOfParallelism > 1` вызываются параллельно — состояние обработчика должно быть потокобезопасным.
- `Dictionary`/`List` не читать без lock, если их кто-то меняет параллельно; для общих реестров использовать `ConcurrentDictionary` (как в `LocalBus`).
