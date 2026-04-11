# 📬 Notification Service

Микросервис для отправки уведомлений (email) с поддержкой очередей, retry-политики и логирования попыток доставки.

Проект построен на **.NET 8**, использует **RabbitMQ**, **PostgreSQL** и полностью готов к запуску в Docker.

---

## 🚀 Возможности

* 📩 Отправка email-уведомлений
* 📨 Обработка событий через RabbitMQ
* 🔁 Retry с exponential backoff
* 🗃 Хранение истории попыток отправки
* ⚙️ Конфигурация через environment variables
* 🐳 Полная docker-инфраструктура
* 📬 SMTP тестирование через Mailpit

---

## 🏗 Архитектура

```
Client / API
     ↓
RabbitMQ (event)
     ↓
Notification Service
     ↓
PostgreSQL (логирование)
     ↓
SMTP (Mailpit / реальный SMTP)
```

---

## 🔄 Как работает сервис

1. В систему поступает событие (например, `PasswordResetRequested`)
2. Создается запись в БД со статусом `Pending`
3. Background worker выбирает уведомление для обработки
4. Статус меняется на `Processing`
5. Выполняется отправка через SMTP
6. Результат:

    * `Sent` — успешно
    * `Failed` — ошибка
7. Каждая попытка фиксируется в `notification_attempts`

---

## 🔁 Retry механизм

При ошибке отправки:

* увеличивается номер попытки
* рассчитывается задержка
* назначается `NextAttemptAtUtc`

Формула:

```
delay = FirstDelay * (BackoffMultiplier ^ attempt)
```

Ограничения:

* `MaxAttempts`
* `MaxDelaySeconds`

Если попытки закончились:

```
Status = Exhausted
```

---

## 🧩 Технологии

* .NET 8 / ASP.NET Core
* Entity Framework Core
* PostgreSQL
* RabbitMQ
* Mailpit
* Docker / Docker Compose

---

## ⚙️ Конфигурация

Все настройки задаются через `.env` файл.

Пример:

```
PostgreSql__ConnectionString=Host=db;Port=5432;Database=notification_service_db;Username=postgres;Password=postgres

RabbitMq__Host=rabbitmq
RabbitMq__Port=5672
RabbitMq__UserName=guest
RabbitMq__Password=guest

Smtp__Host=mailpit
Smtp__Port=1025

Retry__MaxAttempts=3
Retry__FirstDelaySeconds=5
Retry__BackoffMultiplier=2.0
```

---

## 🐳 Запуск

```
docker compose up --build
```

---

## 🌐 Доступные сервисы

| Сервис      | URL                           |
| ----------- | ----------------------------- |
| API         | http://localhost:8081         |
| Swagger     | http://localhost:8081/swagger |
| Mailpit UI  | http://localhost:8025         |
| RabbitMQ UI | http://localhost:15673        |

---

## 🧪 Пример запроса

```
{
  "messageId": "11111111-1111-4111-8111-111111111111",
  "email": "test@email.com",
  "userName": "test",
  "resetLink": "http://link.com",
  "expirationMinutes": 20
}
```

---

## 📬 Тестирование email

Открой:

http://localhost:8025

Там отображаются все отправленные письма.

---

## 🧠 Особенности

* retry при старте (БД и RabbitMQ)
* отказоустойчивость при отправке
* разделение логики:

    * создание уведомлений
    * обработка
    * доставка

---

## 📈 Возможные улучшения

* Dead Letter Queue (DLQ)
* HTML-шаблоны писем
* Метрики (Prometheus / Grafana)
* Поддержка SMS / Push
* Distributed lock

---

## 👨‍💻 Автор

Backend pet-project с фокусом на архитектуру и надежность.
