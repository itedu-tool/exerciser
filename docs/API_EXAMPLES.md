# 📚 API Examples

Примеры использования Exerciser API с реальными запросами и ответами.

## Оглавление

1. [Health Check](#health-check)
2. [Импорт Экзамена](#импорт-экзамена)
3. [Валидация](#валидация)
4. [Rate Limiting](#rate-limiting)
5. [Ошибки](#ошибки)

---

## Health Check

### Запрос (Legacy)

```http
GET /health HTTP/1.1
Host: localhost:5257
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
Host: localhost:5257
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
  ]
}
```

### Запрос

```http
POST /api/v1/exams/import HTTP/1.1
Host: localhost:5257
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
- Неверные значения параметров

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
curl -X GET http://localhost:5257/api/v1/health \
  -H "Accept: application/json"
```

### Импорт экзамена

```bash
curl -X POST http://localhost:5257/api/v1/exams/import \
  -F "file=@exam.json"
```

### С сохранением ID

```bash
EXAM_ID=$(curl -s -X POST http://localhost:5257/api/v1/exams/import \
  -F "file=@exam.json" | jq -r '.id')

echo "Exam ID: $EXAM_ID"
```

---

**Версия:** 1.0.0  
**Последнее обновление:** 2024-06-05  
**API Version:** v1
