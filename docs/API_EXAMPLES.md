# 📚 API Examples

Примеры использования Exerciser API с реальными запросами и ответами.

## Оглавление

1. [Health Check](#health-check)
2. [Импорт Экзамена](#импорт-экзамена)
3. [Список экзаменов](#список-экзаменов)
4. [Управление группами и студентами](#управление-группами-и-студентами)
5. [Студенческая сессия и попытки](#студенческая-сессия-и-попытки)
6. [Валидация](#валидация)
7. [Rate Limiting](#rate-limiting)
8. [Ошибки](#ошибки)

---

## Health Check

### Запрос (Legacy)

```http
GET /health HTTP/1.1
Host: localhost:8080
Accept: application/json
```

### Ответ (200 OK)

```json
{
  "status": "healthy",
  "timestamp": "2024-06-05T12:14:57.123",
  "timestampUtc": "2024-06-05T12:14:57.123Z",
  "timeZone": "UTC",
  "offset": "00:00:00"
}
```

### Запрос (v1)

```http
GET /api/v1/health HTTP/1.1
Host: localhost:8080
Accept: application/json
```

### Ответ (200 OK)

```json
{
  "status": "healthy",
  "timestamp": "2024-06-05T12:14:57.123",
  "timestampUtc": "2024-06-05T12:14:57.123Z",
  "timeZone": "UTC",
  "offset": "00:00:00",
  "apiVersion": "v1"
}
```

---

## Импорт Экзамена

### JSON файл для импорта

```json
{
  "title": "Основы C#",
  "description": "Проверка знаний по основам языка C#",
  "questions": [
    {
      "text": "Что такое CLR?",
      "type": "TextInput",
      "correctAnswers": [
        "Common Language Runtime"
      ]
    },
    {
      "text": "Какой модификатор доступа является самым закрытым?",
      "type": "SingleChoice",
      "options": [
        "public",
        "private",
        "protected",
        "internal"
      ],
      "correctAnswers": [
        "private"
      ]
    },
    {
      "text": "Какие из типов являются значимыми (value types)?",
      "type": "MultipleChoice",
      "options": [
        "int",
        "string",
        "bool",
        "object",
        "struct"
      ],
      "correctAnswers": [
        "int",
        "bool",
        "struct"
      ]
    }
  ],
  "singleChoiceToShow": 0,
  "multipleChoiceToShow": 0,
  "textInputToShow": 0
}
```

> **Примечание:** Поля `singleChoiceToShow`, `multipleChoiceToShow`, `textInputToShow` являются опциональными. Если не
> указаны (или равны 0), студенту будут показаны все вопросы соответствующего типа.

### Запрос

```http
POST /api/v1/exams/import HTTP/1.1
Host: localhost:8080
Content-Type: multipart/form-data; boundary=----WebKitFormBoundary

------WebKitFormBoundary
Content-Disposition: form-data; name="file"; filename="exam.json"
Content-Type: application/json

{
  "title": "Основы C#",
  "description": "...",
  "questions": [...]
}
------WebKitFormBoundary--
```

### Ответ (201 Created)

```json
{
  "id": "507f1f77bcf86cd799439011",
  "title": "Основы C#",
  "questionsCount": 3
}
```

**Заголовки ответа:**

```
Location: /api/v1/exams/507f1f77bcf86cd799439011
Content-Type: application/json
```

---

## Список экзаменов

### Запрос

```http
GET /api/v1/exams HTTP/1.1
Host: localhost:8080
Accept: application/json
```

### Ответ (200 OK)

```json
[
  {
    "id": "507f1f77bcf86cd799439011",
    "title": "Основы C#",
    "description": "Проверка знаний по основам языка C#",
    "questionsCount": 3,
    "singleChoiceCount": 1,
    "multipleChoiceCount": 1,
    "textInputCount": 1,
    "singleChoiceToShow": 0,
    "multipleChoiceToShow": 0,
    "textInputToShow": 0,
    "createdAt": "2026-06-10T10:30:00Z"
  }
]
```

---

## Управление группами и студентами

### Создание группы

**Запрос:**

```http
POST /api/v1/groups HTTP/1.1
Host: localhost:8080
Content-Type: application/json

{
  "name": "Группа 1"
}
```

**Ответ (201 Created):**

```json
{
  "id": "019eabf3-12fe-7542-8285-ccc4d5a260ec",
  "name": "Группа 1",
  "students": []
}
```

### Импорт группы из JSON

**Файл `group.json`:**

```json
{
  "name": "Группа 1",
  "students": [
    { "lastName": "Иванов", "firstName": "Иван", "patronymic": "Иванович" },
    { "lastName": "Петрова", "firstName": "Мария" }
  ]
}
```

**Запрос:**

```bash
curl -X POST http://localhost:8080/api/v1/groups/import -F "file=@group.json"
```

**Ответ (201 Created):**

```json
{
  "id": "019eabf3-12fe-7542-8285-ccc4d5a260ec",
  "name": "Группа 1",
  "students": [
    { "id": "...", "fullName": "Иванов Иван Иванович" },
    { "id": "...", "fullName": "Петрова Мария" }
  ]
}
```

### Получить список групп

```http
GET /api/v1/groups HTTP/1.1
Host: localhost:8080
```

### Добавить студента в группу

```bash
curl -X POST http://localhost:8080/api/v1/groups/019eabf3-12fe-7542-8285-ccc4d5a260ec/students \
  -H "Content-Type: application/json" \
  -d '{"lastName":"Сидоров","firstName":"Петр"}'
```

---

## Студенческая сессия и попытки

### Начало сессии (логин)

**Запрос:**

```http
POST /api/v1/sessions/start HTTP/1.1
Host: localhost:8080
Content-Type: application/json

{
  "groupId": "019eabf3-12fe-7542-8285-ccc4d5a260ec",
  "studentId": "019eabf3-12fe-7542-8285-ccc4d5a260ed"
}
```

**Ответ (200 OK):**

```json
{
  "sessionId": "019eabf3-12fe-7542-8285-ccc4d5a260ee"
}
```

### Начало попытки

**Заголовок:** `X-Session-Id: <sessionId>`

**Запрос:**

```http
POST /api/v1/attempts/start HTTP/1.1
Host: localhost:8080
X-Session-Id: 019eabf3-12fe-7542-8285-ccc4d5a260ee
Content-Type: application/json

{
  "examId": "019eabf3-12fe-7542-8285-ccc4d5a260eb"
}
```

**Ответ (200 OK):**

```json
{
  "attemptId": "019eabf3-12fe-7542-8285-ccc4d5a260ef",
  "exam": {
    "id": "019eabf3-12fe-7542-8285-ccc4d5a260eb",
    "title": "Основы C#",
    "description": "Проверка знаний по основам языка C#",
    "questions": [
      {
        "id": "q1",
        "text": "Что такое CLR?",
        "type": "TextInput",
        "options": [],
        "correctAnswers": ["Common Language Runtime"]
      }
    ]
  }
}
```

### Завершение попытки

```http
POST /api/v1/attempts/019eabf3-12fe-7542-8285-ccc4d5a260ef/finish HTTP/1.1
Host: localhost:8080
X-Session-Id: 019eabf3-12fe-7542-8285-ccc4d5a260ee
Content-Type: application/json

{
  "totalScore": 5,
  "finishedAt": "2026-06-15T12:00:00Z",
  "answers": [
    {
      "questionId": "q1",
      "answer": "Common Language Runtime",
      "score": 3
    }
  ]
}
```

**Ответ (200 OK):**

```json
{
  "success": true
}
```

### Получение результата попытки

```http
GET /api/v1/attempts/019eabf3-12fe-7542-8285-ccc4d5a260ef/result HTTP/1.1
Host: localhost:8080
X-Session-Id: 019eabf3-12fe-7542-8285-ccc4d5a260ee
```

**Ответ (200 OK):**

```json
{
  "attemptId": "019eabf3-12fe-7542-8285-ccc4d5a260ef",
  "examTitle": "Основы C#",
  "studentFullName": "Иванов Иван Иванович",
  "groupName": "Группа 1",
  "startedAt": "2026-06-15T11:30:00Z",
  "finishedAt": "2026-06-15T12:00:00Z",
  "totalScore": 5,
  "maxPossibleScore": 5,
  "questions": [
    {
      "text": "Что такое CLR?",
      "type": "TextInput",
      "options": [],
      "correctAnswers": ["Common Language Runtime"],
      "userAnswer": "Common Language Runtime",
      "score": 3,
      "maxScore": 3
    }
  ]
}
```

---

## Валидация

### Пример 1: Пустой файл

**Запрос:** POST с пустым телом

**Ответ (400 Bad Request):**

```json
{
  "error": "Файл не загружен"
}
```

### Пример 2: Неверный формат файла

**Запрос:** POST с файлом `.txt` вместо `.json`

**Ответ (400 Bad Request):**

```json
{
  "error": "Файл должен быть в формате JSON"
}
```

### Пример 3: Неверный JSON формат

**JSON:**

```json
{
  "title": "Test",
  "questions": [{
    "text": "Q1",
    "type": "TextInput",
    "correctAnswers": ["A"
    // ❌ Закрывающая скобка отсутствует
}
```

**Ответ (400 Bad Request):**

```json
{
  "error": "Неверный формат JSON: Unexpected end of JSON input"
}
```

### Пример 4: Отсутствует обязательное поле

**JSON:**

```json
{
  "description": "Test",
  // ❌ Отсутствует "title"
  "questions": []
}
```

**Ответ (400 Bad Request):**

```json
{
  "error": "Название экзамена не может быть пустым"
}
```

### Пример 5: Пустой список вопросов

**JSON:**

```json
{
  "title": "Test",
  "questions": []
  // ❌ Нет вопросов
}
```

**Ответ (400 Bad Request):**

```json
{
  "error": "Экзамен должен содержать хотя бы один вопрос"
}
```

### Пример 6: Неверный тип вопроса

**JSON:**

```json
{
  "text": "Q1",
  "type": "InvalidType",  // ❌ Допустимы: SingleChoice, MultipleChoice, TextInput
  "correctAnswers": ["A"]
}
```

**Ответ (400 Bad Request):**

```json
{
  "error": "Вопрос #1: недопустимый тип 'InvalidType'. Допустимые типы: SingleChoice, MultipleChoice, TextInput"
}
```

### Пример 7: SingleChoice с несколькими ответами

**JSON:**

```json
{
  "text": "Q1",
  "type": "SingleChoice",
  "options": ["A", "B", "C"],
  "correctAnswers": ["A", "B"]  // ❌ SingleChoice должен иметь только один правильный ответ
}
```

**Ответ (400 Bad Request):**

```json
{
  "error": "Вопрос #1 (SingleChoice): допускается только один правильный ответ"
}
```

### Пример 8: Правильный ответ отсутствует в вариантах

**JSON:**

```json
{
  "text": "Q1",
  "type": "SingleChoice",
  "options": ["A", "B", "C"],
  "correctAnswers": ["D"]  // ❌ "D" нет в options
}
```

**Ответ (400 Bad Request):**

```json
{
  "error": "Вопрос #1: правильные ответы [D] отсутствуют в вариантах ответов"
}
```

### Пример 9: Количество вопросов для показа превышает доступное

**JSON:**

```json
{
  "title": "Test",
  "questions": [...],
  "singleChoiceToShow": 100,  // ❌ В экзамене только 10 вопросов SingleChoice
  "multipleChoiceToShow": 0,
  "textInputToShow": 0
}
```

**Ответ (400 Bad Request):**

```json
{
  "error": "SingleChoiceToShow (100) превышает доступное количество (10)"
}
```

---

## Rate Limiting

### Пример 1: Превышение лимита (Import endpoint)

**Лимит:** 10 запросов в час

**Запрос #11:**

```http
POST /api/v1/exams/import HTTP/1.1
```

**Ответ (429 Too Many Requests):**

```json
{
  "error": "Too many requests"
}
```

**Заголовки ответа:**

```
Retry-After: 3600
X-Rate-Limit-Limit: 10
X-Rate-Limit-Remaining: 0
X-Rate-Limit-Reset: 1717574157
```

### Пример 2: Превышение лимита (General endpoint)

**Лимит:** 100 запросов в минуту

**Запрос #101:**

```http
GET /api/v1/health HTTP/1.1
```

**Ответ (429 Too Many Requests):**

```json
{
  "error": "Too many requests"
}
```

---

## Ошибки

### 400 Bad Request

Причины:

- Пустой файл
- Неверный формат файла (не JSON)
- Невалидный JSON
- Отсутствуют обязательные поля
- Неверные типы вопросов
- Неверные значения параметров (например, количество вопросов для показа превышает доступное)
- И другие ошибки валидации

**Ответ:**

```json
{
  "error": "Описание ошибки"
}
```

### 429 Too Many Requests

Причина: Превышен лимит запросов

**Ответ:**

```json
{
  "error": "Too many requests"
}
```

**Заголовки:**

```
Retry-After: <секунды до сброса лимита>
```

### 500 Internal Server Error

Причина: Внутренняя ошибка сервера

**Ответ:**

```json
{
  "error": "Внутренняя ошибка сервера. Попробуйте позже."
}
```

---

## Curl примеры

### Health Check

```bash
curl -X GET http://localhost:8080/api/v1/health \
  -H "Accept: application/json"
```

### Импорт экзамена

```bash
curl -X POST http://localhost:8080/api/v1/exams/import \
  -F "file=@exam.json"
```

### С сохранением ID

```bash
EXAM_ID=$(curl -s -X POST http://localhost:8080/api/v1/exams/import \
  -F "file=@exam.json" | jq -r '.id')

echo "Exam ID: $EXAM_ID"
```

### Начать сессию и попытку (полный цикл)

```bash
# 1. Получить группы
GROUPS=$(curl -s http://localhost:8080/api/v1/groups)
GROUP_ID=$(echo $GROUPS | jq -r '.[0].id')
STUDENT_ID=$(echo $GROUPS | jq -r '.[0].students[0].id')

# 2. Начать сессию
SESSION_ID=$(curl -s -X POST http://localhost:8080/api/v1/sessions/start \
  -H "Content-Type: application/json" \
  -d "{\"groupId\":\"$GROUP_ID\",\"studentId\":\"$STUDENT_ID\"}" | jq -r '.sessionId')

# 3. Начать попытку
EXAM_ID="..."
ATTEMPT_ID=$(curl -s -X POST http://localhost:8080/api/v1/attempts/start \
  -H "X-Session-Id: $SESSION_ID" \
  -H "Content-Type: application/json" \
  -d "{\"examId\":\"$EXAM_ID\"}" | jq -r '.attemptId')

# 4. Завершить попытку
curl -X POST http://localhost:8080/api/v1/attempts/$ATTEMPT_ID/finish \
  -H "X-Session-Id: $SESSION_ID" \
  -H "Content-Type: application/json" \
  -d '{"totalScore":0,"finishedAt":"2026-06-15T12:00:00Z","answers":[]}'

# 5. Получить результат
curl http://localhost:8080/api/v1/attempts/$ATTEMPT_ID/result \
  -H "X-Session-Id: $SESSION_ID"
```

---

**Версия:** 1.2.0  
**Последнее обновление:** 2026-06-15  
**API Version:** v1