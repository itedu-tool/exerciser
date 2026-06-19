# 📖 Постман Setup Guide

## Оглавление

1. [Установка Postman](#установка-postman)
2. [Импорт Collection](#импорт-collection)
3. [Импорт Environment](#импорт-environment)
4. [Использование](#использование)
5. [Тестирование](#тестирование)
6. [Rate Limiting](#rate-limiting)
7. [Troubleshooting](#troubleshooting)

---

## Установка Postman

### Вариант 1: Скачать приложение

1. Перейти на https://www.postman.com/downloads/
2. Выбрать свою ОС (Windows, macOS, Linux)
3. Скачать и установить

### Вариант 2: Использовать веб-версию

1. Зайти на https://web.postman.co/
2. Создать аккаунт или войти

### Вариант 3: Docker

```bash
docker run -d \
  --name postman \
  -p 3000:3000 \
  postman/postman-api-runtime:latest
```

---

## Импорт Collection

### Шаг 1: Открыть Postman

### Шаг 2: Импортировать Collection

**Способ 1: Через UI**

1. Нажать **File** → **Import**
2. Выбрать файл `.postman/Exerciser.postman_collection.json`
3. Нажать **Import**

**Способ 2: Через drag & drop**

1. Просто перетащить файл `.postman/Exerciser.postman_collection.json` в левую панель Postman

**Способ 3: Через URL (если размещено на GitHub)**

1. **File** → **Import** → **Link**
2. Вставить URL:
   `https://raw.githubusercontent.com/itedu-tool/exerciser/main/.postman/Exerciser.postman_collection.json`
3. Нажать **Import**

### Шаг 3: Проверка импорта

В левой панели должна появиться папка **"Exerciser API"** с подпапками:

- ✅ **Health** – проверка работоспособности API
- ✅ **Exams (Admin)** – управление экзаменами (импорт, получение, обновление, удаление)
- ✅ **Groups & Students** – управление группами и студентами
- ✅ **Student Session & Attempts** – студенческая сессия и попытки
- ✅ **API Information** – документация и информация об API

---

## Импорт Environment

### Шаг 1: Импортировать Development Environment

1. Нажать **Environments** (иконка в левой панели)
2. Нажать **Import**
3. Выбрать файл `.postman/environments/development.json`
4. Нажать **Import**

### Шаг 2: Выбрать Environment

В верхнем правом углу должен быть dropdown с окружениями. Выбрать **"Exerciser - Development"**

![Environment Selector](./screenshots/environment-selector.png)

### Шаг 3: Проверка переменных

Нажать на иконку Environment рядом с именем. Должны быть переменные:

- `base_url` = `http://localhost:8080`
- `exam_file_path` = `./exam.json`
- `group_file_path` = `./group.json`
- `exam_id`, `group_id`, `student_id`, `session_id`, `attempt_id` – будут заполняться автоматически во время выполнения
  запросов

---

## Использование

### Пример 1: Health Check

1. В левой панели открыть **Health** → **Health Check (Legacy)**
2. Нажать **Send**
3. Внизу должен появиться ответ **200 OK** с JSON:

```json
{
  "status": "healthy",
  "timestamp": "2026-06-15T12:14:57.123",
  "timestampUtc": "2026-06-15T12:14:57.123Z",
  "timeZone": "UTC",
  "offset": "00:00:00"
}
```

### Пример 2: Импорт Экзамена

1. Открыть **Exams (Admin)** → **Import Exam - Success**
2. Убедиться, что файл `exam.json` находится в том же каталоге
3. Нажать **Send**
4. Должен получиться ответ **201 Created**:

```json
{
  "id": "507f1f77bcf86cd799439011",
  "title": "Основы C#",
  "questionsCount": 3
}
```

> **Примечание:** ID экзамена автоматически сохранится в переменную `exam_id` для использования в следующих запросах.

### Пример 3: Управление группами и студентами

1. Открыть **Groups & Students** → **Get All Groups** – получить список групп
2. Открыть **Create Group** – создать новую группу
3. Открыть **Import Group from JSON** – импортировать группу из файла `group.json`
4. Открыть **Add Student to Group** – добавить студента в существующую группу (ID группы берётся из переменной
   `group_id`)

### Пример 4: Студенческая сессия и попытки (полный цикл)

1. **Start Session** – создать сессию для студента (укажите `groupId` и `studentId` из предыдущих шагов). `sessionId`
   сохранится автоматически.
2. **Start Attempt** – начать попытку для выбранного экзамена (заголовок `X-Session-Id` подставится автоматически).
   `attemptId` сохранится.
3. **Finish Attempt** – завершить попытку, отправив ответы.
4. **Get Attempt Result** – получить результат завершённой попытки.

### Пример 5: Переменные в запросе

Заметьте в URL:

```
{{base_url}}/api/v1/exams/import
```

Это означает:

- `{{base_url}}` → `http://localhost:8080` (из Environment)

Итоговый URL: `http://localhost:8080/api/v1/exams/import`

---

## Тестирование

### Встроенные Tests

Каждый endpoint имеет встроенные тесты. После отправки запроса:

1. Нажать вкладку **Tests** (внизу)
2. Должны видны результаты тестов:

```
✓ Status code is 200
✓ Response time < 500ms
✓ Response has healthy status
✓ Response has timestamp
```

### Запуск всех тестов Collection

1. Открыть **Collection** (левая панель)
2. Нажать **...** → **Run collection**
3. Выбрать Environment
4. Нажать **Run Exerciser API**

Postman откроет **Collection Runner** и запустит все запросы подряд.

---

## Rate Limiting

### Общие пределы

| Endpoint                                  | Лимит | Период   |
|-------------------------------------------|-------|----------|
| General (Health, Exams, Groups, Sessions) | 100   | 1 минута |
| Import Exam                               | 10    | 1 час    |

### Тестирование Rate Limiting

1. Открыть **Exams (Admin)** → **Import Exam - Success**
2. Отправить запрос 11 раз подряд (используя **Ctrl+Enter**)
3. На 11-м запросе должен получиться ответ **429 Too Many Requests**

### Как узнать оставшиеся запросы?

Смотреть в **Response Headers** (вкладка **Headers** внизу):

- `X-Rate-Limit-Limit` = максимум запросов
- `X-Rate-Limit-Remaining` = осталось запросов
- `X-Rate-Limit-Reset` = время сброса (в Unix timestamp)

---

## Troubleshooting

### Проблема 1: "Не могу подключиться к базе"

**Причина:** API не запущен или использован неправильный URL

**Решение:**

```bash
# Проверить, запущен ли Docker
docker compose ps

# Запустить, если не запущен
docker compose up -d

# Проверить, доступен ли API
curl http://localhost:8080/health
```

### Проблема 2: "Invalid file path"

**Причина:** Путь к `exam.json` неправильный

**Решение:**

1. Открыть **Exams (Admin)** → **Import Exam - Success**
2. Нажать **Body** → **form-data**
3. В поле `file` нажать на иконку файла
4. Выбрать файл `exam.json` из вашей папки проекта

### Проблема 3: "Environment variables не подставляются"

**Причина:** Environment не выбран

**Решение:**

1. В верхнем правом углу найти dropdown окружений
2. Выбрать **"Exerciser - Development"**
3. Должны видны значения переменных справа

### Проблема 4: "Response time очень долгий"

**Причина:** Сервер перегружен или MongoDB недоступен

**Решение:**

```bash
# Проверить логи
docker compose logs webapi

# Перезапустить
docker compose restart webapi
```

### Проблема 5: "Tests не запускаются"

**Причина:** Вкладка Tests скрыта или неверный формат

**Решение:**

1. Внизу окна должна быть вкладка **Tests**
2. Если не видна, нажать **...** → **Show Tests**
3. Убедиться, что Script не пустой

---

## Советы и трюки

### Совет 1: Сохранение переменных из ответа

В скрипте Tests можно сохранить данные из ответа в переменную:

```javascript
// Сохранить ID экзамена для использования в следующем запросе
var jsonData = pm.response.json();
pm.environment.set('exam_id', jsonData.id);
```

Потом использовать в следующем запросе как `{{exam_id}}`

### Совет 2: Условные запросы

Использовать **Pre-request Script** для логики:

```javascript
// Запустить запрос только если переменная существует
if (!pm.environment.get('exam_id')) {
    console.error('exam_id not set! Run import first.');
}
```

### Совет 3: Экспорт результатов

После запуска Collection Runner:

1. Нажать **Export Results**
2. Выбрать формат (JSON, CSV, HTML)
3. Сохранить отчет

### Совет 4: Синхронизация с командой

1. Нажать **Collection** → **Share**
2. Выбрать способ:
    - **Postman Link** (облако)
    - **Export** (JSON файл)
3. Поделиться ссылкой или файлом с командой

---

## Дополнительные ресурсы

- [Postman Documentation](https://learning.postman.com/)
- [API Documentation in Postman](https://learning.postman.com/docs/publishing-your-api/authoring-your-documentation/)
- [Postman Scripting](https://learning.postman.com/docs/writing-scripts/intro-to-scripts/)
- [Collection Runner](https://learning.postman.com/docs/running-collections/intro-to-runs/)

---

**Версия:** 1.2.0  
**Последнее обновление:** 2026-06-15  
**API Version:** v1