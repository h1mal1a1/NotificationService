# 📐 ARCHITECTURE.md

# Notification Service Architecture

## Overview

`Notification Service` — это микросервис для асинхронной обработки и доставки уведомлений.

Сервис построен по event-driven модели:

* бизнес-система публикует событие;
* событие попадает в очередь (RabbitMQ);
* сервис сохраняет уведомление в базе данных;
* отдельный воркер выполняет доставку;
* применяется retry-политика при ошибках;
* все попытки логируются.

Такой подход отделяет создание уведомления от его доставки и делает систему устойчивой к сбоям.

---

## 🏗 High-level flow

```id="flow"
Business Service / API
        ↓
     RabbitMQ
        ↓
RabbitMqConsumerBackgroundService
        ↓
NotificationCreationService
        ↓
      PostgreSQL
        ↓
PendingNotificationsWorker
        ↓
NotificationDeliveryService
        ↓
    SMTP / Mailpit
```

---

## 🎯 Архитектурные цели

* асинхронная обработка
* отказоустойчивость
* retry-поведение
* наблюдаемость
* масштабируемость
* разделение ответственности

---

## 🧩 Core components

### 1. RabbitMQ Consumer

**`RabbitMqConsumerBackgroundService`**

Отвечает за получение сообщений из RabbitMQ.

Функции:

* подключение к брокеру
* подписка на очередь
* получение сообщений
* передача в обработчик
* ACK / NACK

📌 Почему важно
Consumer не отправляет email напрямую — он только принимает события.
Это защищает систему от:

* медленного SMTP
* падений внешних сервисов
* блокировки очереди

---

### 2. Message Handler

**`RabbitMqMessageHandler`**

Связывает транспорт (RabbitMQ) и бизнес-логику.

Функции:

* десериализация события
* передача в `NotificationCreationService`

📌 Зачем нужен
Позволяет:

* не перегружать consumer
* легко добавлять новые типы событий

---

### 3. Notification Creation

**`NotificationCreationService`**

Создает уведомление в базе данных.

Функции:

* проверка данных
* идемпотентность по `MessageId`
* выбор шаблона
* рендеринг письма
* сохранение в статусе `Pending`

📌 Почему сначала БД
Сервис НЕ отправляет письмо сразу.

Плюсы:

* данные не теряются
* можно делать retry
* есть история
* система управляемая

---

### 4. Pending Worker

**`PendingNotificationsWorker`**

Фоновый воркер, который обрабатывает уведомления.

Функции:

* выбор `Pending` уведомлений
* проверка `NextAttemptAtUtc`
* batch-обработка
* возврат зависших `Processing` в `Pending`

📌 Почему не `Task.Delay`

Проблемы `Task.Delay`:

* поток блокируется
* теряется при рестарте
* плохо масштабируется

Решение через БД:

* retry хранится в данных
* можно перезапускать сервис
* поведение предсказуемое

---

### 5. Notification Delivery

**`NotificationDeliveryService`**

Отвечает за отправку уведомления.

Функции:

* перевод в `Processing`
* отправка через SMTP
* запись попытки
* обработка ошибок
* расчет retry
* перевод в `Sent` или `Exhausted`

---

### 6. Email Sender

**`IEmailSender` / `SmtpEmailSender`**

Отправляет email.

Среды:

* Dev → Mailpit
* Prod → SMTP провайдер

📌 Плюсы:

* легко заменить реализацию
* удобно тестировать

---

### 7. Template Renderer

**`ITemplateRenderer`**

Подставляет значения в шаблоны.

📌 Плюсы:

* отделение контента от кода
* гибкость
* расширяемость

---

## 🗃 Data model

### notifications

Хранит текущее состояние уведомления:

* `message_id`
* `event_type`
* `channel`
* `recipient`
* `subject`, `body`
* `status`
* `retry_count`
* `next_attempt_at_utc`
* `last_attempt_at_utc`
* `processing_started_at_utc`
* `sent_at_utc`

---

### notification_attempts

Хранит историю попыток:

* `attempt_number`
* `attempted_at_utc`
* `is_success`
* `error_message`

📌 Зачем:

* анализ ошибок
* дебаг
* прозрачность системы

---

## 🔄 Notification lifecycle

* `Pending` — ожидает обработки
* `Processing` — обрабатывается
* `Sent` — отправлено
* `Exhausted` — попытки исчерпаны

---

## 🔁 Retry strategy

Используется exponential backoff:

```id="retry"
delay = FirstDelay * (BackoffMultiplier ^ attempt)
```

Ограничения:

* `MaxAttempts`
* `MaxDelaySeconds`

📌 Плюсы:

* снижает нагрузку
* устойчив к сбоям
* предсказуемое поведение

---

## ⏱ Processing timeout

Если уведомление зависло в `Processing`:

→ возвращается в `Pending`

📌 Зачем:

* защита от падений
* устойчивость к рестартам

---

## 🔐 Idempotency

Реализована через `MessageId`.

📌 Защищает от:

* дубликатов сообщений
* повторной доставки

---

## 🚀 Startup resilience

Реализован retry при старте:

* подключение к БД
* подключение к RabbitMQ

📌 Это важно в Docker:
сервисы могут стартовать не одновременно

---

## ⚙️ Configuration

Конфигурация через `.env`.

Плюсы:

* нет дублирования
* удобно в Docker
* безопасно

---

## 📊 Почему статусы — строки

Статусы хранятся как string:

Плюсы:

* читаемость БД
* отсутствие ошибок enum
* удобство отладки

---

## ⚠️ Ограничения

* только email
* нет DLQ
* нет distributed lock
* нет метрик

---

## 🚀 Возможные улучшения

* Dead Letter Queue
* HTML письма
* SMS / Push
* метрики
* distributed lock

---

## 🧠 Summary

Сервис реализует:

* асинхронную обработку
* надежную доставку
* retry-политику
* прозрачный lifecycle

Это не просто отправка email, а **управляемый pipeline доставки уведомлений**.
