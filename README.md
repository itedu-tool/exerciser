# 📚 Exerciser – Система тестирования студентов

**Exerciser** – это веб-приложение для проведения тестирования студентов. Преподаватели загружают вопросы в формате JSON, система сохраняет их в MongoDB. Студенты могут проходить тесты через удобный веб-интерфейс (разрабатывается отдельно). Административная панель позволяет импортировать, просматривать и удалять экзамены.

---

## 🚀 Быстрый старт

### Требования

- [Docker](https://docs.docker.com/get-docker/) и [Docker Compose](https://docs.docker.com/compose/install/) (рекомендуется)
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (для локальной разработки)
- [Node.js](https://nodejs.org/) (для сборки фронтенда)

### Запуск с Docker Compose (рекомендованный способ)

```bash
# Клонируйте репозиторий
git clone https://github.com/itedu-tool/exerciser.git
cd exerciser

# Создайте файл .env из примера (опционально)
cp .env.example .env

# Запустите все сервисы (MongoDB, Redis, API)
docker compose up -d

# Проверьте статус
docker compose ps

# Просмотр логов API
docker compose logs -f webapi

# Откройте в браузере:
# - Административная панель: http://localhost:3000
# - API документация: http://localhost:8080/scalar/v1
```

### Остановка

```bash
docker compose down          # Остановить контейнеры
docker compose down -v       # Остановить и удалить тома с данными
```

---

## 📋 API Endpoints (v1)

Базовый URL: `http://localhost:8080/api/v1`

| Метод | Эндпоинт | Описание |
|-------|----------|-----------|
| `GET` | `/health` | Проверка здоровья (legacy, без версии) |
| `GET` | `/api/v1/health` | Проверка здоровья (v1) |
| `POST` | `/api/v1/exams/import` | Импорт экзамена из JSON-файла |
| `GET` | `/api/v1/exams` | Получить список всех экзаменов (только метаданные) |
| `GET` | `/api/v1/exams/{id}` | Получить полный экзамен (включая вопросы и ответы) |
| `PUT` | `/api/v1/exams/{id}` | Полное обновление экзамена |
| `DELETE` | `/api/v1/exams/{id}` | Удалить экзамен |

### Импорт экзамена (пример)

**Файл `exam.json`:**
```json
{
  "title": "Основы C#",
  "description": "Проверка знаний по основам языка C#",
  "questions": [
    {
      "text": "Что такое CLR?",
      "type": "TextInput",
      "correctAnswers": ["Common Language Runtime"]
    },
    {
      "text": "Какой модификатор доступа является самым закрытым?",
      "type": "SingleChoice",
      "options": ["public", "private", "protected", "internal"],
      "correctAnswers": ["private"]
    },
    {
      "text": "Какие из типов являются значимыми?",
      "type": "MultipleChoice",
      "options": ["int", "string", "bool", "object", "struct"],
      "correctAnswers": ["int", "bool", "struct"]
    }
  ]
}
```

**Запрос:**
```bash
curl -X POST http://localhost:8080/api/v1/exams/import \
  -F "file=@exam.json"
```

**Ответ (201 Created):**
```json
{
  "id": "507f1f77bcf86cd799439011",
  "title": "Основы C#",
  "questionsCount": 3
}
```

### Получить список экзаменов

```bash
curl http://localhost:8080/api/v1/exams | jq
```

Пример ответа:
```json
[
  {
    "id": "507f1f77bcf86cd799439011",
    "title": "Основы C#",
    "description": "Проверка знаний по основам языка C#",
    "questionsCount": 3,
    "createdAt": "2026-06-10T10:30:00Z"
  }
]
```

### Получить экзамен по ID

```bash
curl http://localhost:8080/api/v1/exams/507f1f77bcf86cd799439011 | jq
```

### Обновить экзамен (PUT)

```bash
curl -X PUT http://localhost:8080/api/v1/exams/507f1f77bcf86cd799439011 \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Обновлённый заголовок",
    "description": "Новое описание",
    "questions": [...]
  }'
```

### Удалить экзамен

```bash
curl -X DELETE http://localhost:8080/api/v1/exams/507f1f77bcf86cd799439011
```

---

## 🏗️ Архитектура решения

### Общая схема

```
Client (Browser) → Admin Panel (Pug/Bootstrap) → Web API (ASP.NET Core) → MongoDB / Redis
                                                         ↓
                                              GitHub Actions → Docker Hub
```

### Структура проекта

```
exerciser/
├── .github/workflows/           # CI/CD (GitHub Actions)
├── .postman/                    # Postman коллекции и окружения
├── docs/                        # Дополнительная документация (API examples, Postman setup)
├── Exerciser.WebApi/            # Бэкенд (ASP.NET Core)
│   ├── Controllers/ (Minimal API в Program.cs)
│   ├── Models/                  # Сущности Exam, Question, настройки
│   ├── DTOs/                    # Объекты передачи данных
│   ├── Repositories/            # Репозиторий для MongoDB
│   ├── Services/                # Миграции, кеширование
│   ├── Validators/              # Валидатор импорта
│   ├── Exceptions/              # Кастомные исключения
│   ├── Extensions/              # Методы расширения для DI
│   └── Program.cs               # Конфигурация и endpoints
├── Exerciser.FrontEnd/          # Монорепозиторий фронтенда
│   ├── Admin/                   # Административная панель
│   │   ├── scripts/             # JavaScript модули
│   │   ├── views/               # Pug шаблоны
│   │   ├── public/              # Скомпилированные файлы (генерируется)
│   │   ├── package.json
│   │   └── eslint.config.js
│   ├── Student/                 # (будет добавлено позже)
│   └── .gitignore, .editorconfig
├── docker-compose.yml
├── .env.example
├── README.md
├── CHANGELOG.md
└── LICENSE
```

### Технологический стек

| Компонент          | Технология                | Версия     |
|--------------------|---------------------------|------------|
| **Backend**        | .NET 10 (ASP.NET Core)    | 10.0       |
| **База данных**    | MongoDB                    | 8.3 (Docker)|
| **Кеширование**    | Redis + MemoryCache        | 8.6.4      |
| **API документация**| OpenAPI + Scalar UI       | 1.2.5      |
| **Логирование**    | NLog                       | 5.2.8      |
| **Rate Limiting**  | Встроенный в ASP.NET Core | 10.0       |
| **Фронтенд (Admin)**| Pug + Bootstrap 5 (Bootswatch) | 3.x / 5.3 |
| **Контейнеризация**| Docker + Docker Compose    | latest     |
| **CI/CD**          | GitHub Actions             |            |

---

## 🔧 Конфигурация

### Переменные окружения (файл `.env`)

Создайте файл `.env` в корне проекта (можно скопировать `.env.example`):

```ini
# MongoDB
MONGO_PORT=27017
MONGO_ROOT_USERNAME=admin
MONGO_ROOT_PASSWORD=changeme
MONGO_DATABASE=exerciser_db

# Redis
REDIS_HOST_PORT=6379
REDIS_PASSWORD=

# API
ASPNETCORE_ENVIRONMENT=Production
API_HTTP_PORT=8080
API_HTTPS_PORT=8081

# CORS (comma‑separated)
CORS_ALLOWED_ORIGINS=http://localhost:3000,http://localhost:5000

# Логирование
LOG_LEVEL=Info
```

### Настройка CORS

По умолчанию разрешены источники из `CORS_ALLOWED_ORIGINS`. Для продакшена укажите домены вашего фронтенда.

### MongoDB Connection String

Строка подключения формируется автоматически в `Program.cs` из следующих переменных:

- `MongoDbSettings__Host` (по умолчанию `mongodb`)
- `MongoDbSettings__Port` (по умолчанию `27017`)
- `MongoDbSettings__Username` (опционально)
- `MongoDbSettings__Password` (опционально)
- `MongoDbSettings__DatabaseName` (по умолчанию `exerciser_db`)

В `docker-compose.yml` аутентификация MongoDB отключена для упрощения разработки. Для продакшена раскомментируйте переменные `MONGO_INITDB_ROOT_USERNAME/PASSWORD` и передайте их в контейнер.

### Rate Limiting

- **Фиксированное окно (`fixed`)** – 100 запросов в минуту, очередь 10.
  Применяется ко всем эндпоинтам, кроме импорта.
- **Скользящее окно (`import-sliding`)** – 10 запросов в час, очередь 5.
  Применяется только к `POST /api/v1/exams/import`.

При превышении лимита возвращается код `429 Too Many Requests` с заголовком `Retry-After`.

### Логирование (NLog)

- Вывод в консоль с цветовой дифференциацией.
- Файлы JSON и текст в папке `logs/` (ротация каждый день, хранение 30 дней).
- Отдельный файл для ошибок `app-errors-*.log`.

В `docker-compose.yml` папка `logs/` монтируется с хоста, поэтому логи сохраняются между перезапусками.

---

## 🔒 Безопасность

- **Non-root пользователь** в Docker‑образе (UID 1001).
- **Alpine Linux** – минимальный базовый образ.
- **Rate limiting** – защита от DDoS и брутфорса.
- **CORS** – настраивается только разрешённые источники.
- **Валидация размера файла** – максимум 10 MB при импорте.
- **Сериализация GUID** – использование стандартного представления (`GuidRepresentation.Standard`).

> ⚠️ **TODO:** В текущей версии отсутствует аутентификация и авторизация. Для продакшена необходимо добавить хотя бы базовую аутентификацию (JWT, API‑ключи) и настроить HTTPS.

---

## 🎨 Административная панель

Административная панель предназначена для преподавателей. Она позволяет:

- Импортировать экзамены из JSON‑файлов.
- Просматривать список экзаменов (название, описание, дата создания, количество вопросов).
- Просматривать детали экзамена (полный список вопросов с вариантами и правильными ответами) в модальном окне.
- Редактировать экзамены – на отдельной странице `edit.html` доступны:
  - Изменение названия и описания.
  - Добавление, удаление и изменение текста вопросов.
  - Выбор типа вопроса (один вариант / несколько вариантов / ввод текста).
  - Добавление и удаление вариантов ответов.
  - Отметка правильных ответов – для каждого варианта слева располагается чекбокс/радио.
  - Кнопки перемещения вопросов вверх/вниз для изменения порядка.
  - Копирование вопроса (дублирование с пометкой "(копия)").
  - Предпросмотр экзамена (модальное окно в режиме только для чтения).
- Удалять экзамены.
- Проверять доступность API через встроенную диагностику.

**Запуск панели в режиме разработки:**

```bash
cd Exerciser.FrontEnd/Admin
npm install
npm start
# Откроется http://localhost:3000
```

**Сборка статических файлов:**

```bash
npm run build
# Результат в папке public/
```

Панель использует общий модуль `utils.js`, который содержит функции для работы с API, отображения сообщений, экранирования HTML и т.д.

### Структура фронтенда

```
Admin/
├── scripts/
│   ├── utils.js           # Общие утилиты (apiRequest, showMessage и др.)
│   ├── health.js          # Страница диагностики API
│   └── teacher.js         # Управление экзаменами (список, импорт, удаление, просмотр)
├── views/
│   ├── layout.pug         # Базовый шаблон
│   ├── index.pug          # Главная страница
│   ├── health.pug         # Диагностика
│   ├── teacher.pug        # Управление экзаменами
│   └── includes/
│       └── config.pug     # Глобальные переменные (API_BASE, темы)
├── public/                # Скомпилированные файлы (не в git)
├── package.json
└── eslint.config.js
```

---

## 🐳 Docker и Docker Compose

### Сервисы

- **mongodb** – база данных (образ `mongo:8.3`), тома `mongodb_data`, `mongodb_config`.
- **redis** – кеш (образ `redis:8.6.4`), том `redis_data`.
- **webapi** – ASP.NET Core приложение (собирается из `Exerciser.WebApi/Dockerfile`).

Все сервисы имеют healthcheck и логирование (`json-file` с ротацией).

### Сборка образа вручную

```bash
docker build -f Exerciser.WebApi/Dockerfile -t exerciser-webapi:latest .
```

### Переменные для Docker Compose

В `docker-compose.yml` используются переменные из `.env`. Основные:

- `MONGO_HOST_PORT` – публичный порт MongoDB (по умолчанию 27017)
- `REDIS_HOST_PORT` – публичный порт Redis (по умолчанию 6379)
- `API_HOST_PORT` – публичный порт API (по умолчанию 8080)
- `ASPNETCORE_ENVIRONMENT` – `Development` или `Production`
- `CORS_ALLOWED_ORIGINS` – через запятую

### Health Check

- MongoDB: `mongosh --eval "db.adminCommand('ping')"`
- Redis: `redis-cli --raw incr _ping`
- WebAPI: `curl -f http://localhost:8080/health`

Зависимости: `webapi` ожидает `service_healthy` от MongoDB и Redis.

---

## 🧪 CI/CD (GitHub Actions)

Файл `.github/workflows/ci_cd.yml` автоматически собирает и публикует Docker‑образ `anstfoto/exerciser-webapi:latest` при пуше в ветку `master` нового тега (новой версии) (или по требованию `workflow_dispatch`).

**Используемые actions (обновлены для Node.js 24):**
- `actions/checkout@v6`
- `docker/login-action@v4`
- `docker/setup-buildx-action@v4`
- `docker/build-push-action@v7`
- `softprops/action-gh-release@v3`

**Что происходит:**

1. Checkout кода.
2. Логин в Docker Hub (используются `vars.DOCKER_USERNAME` и `secrets.DOCKERHUB_TOKEN`).
3. Настройка Buildx.
4. Сборка образа `webapi` через `docker compose build`.
5. Тегирование образа как `${{ vars.DOCKER_USERNAME }}/exerciser-webapi:latest`.
6. Публикация на Docker Hub.

> **TODO:** Добавить шаги для тестов (`dotnet test`), использовать кеширование слоёв, присваивать тег с git‑хешем.

---

## 📄 Документация и инструменты разработчика

- **Scalar UI** (интерактивная документация): `http://localhost:8080/scalar/v1`
- **OpenAPI спецификация (JSON)**: `http://localhost:8080/openapi/v1.json`
- **Postman коллекция**: `.postman/Exerciser.postman_collection.json`
- **Postman окружение (Development)**: `.postman/environments/development.json`
- **Примеры запросов и ответов**: `docs/API_EXAMPLES.md`
- **Настройка Postman**: `docs/POSTMAN_SETUP.md`
- **REST Client файл**: `Exerciser.WebApi/Exerciser.WebApi.http` (для VS Code)

---

## 🛠️ Разработка и тестирование

### Запуск бэкенда без Docker

1. Установите .NET 10 SDK.
2. Установите MongoDB локально (или используйте контейнер только для БД).
3. Обновите `appsettings.Development.json` (укажите `localhost` для MongoDB).
4. Выполните:

```bash
cd Exerciser.WebApi
dotnet run --launch-profile http
```

API будет доступен на `http://localhost:5257`.

### Локальная разработка фронтенда

```bash
cd Exerciser.FrontEnd/Admin
npm install
npm start   # запускает live-server и открывает браузер
```

При изменениях в `scripts/` или `views/` нужно пересобрать проект (`npm run build`). Для автоматической пересборки можно использовать `nodemon` или `pug --watch`.

### Линтинг и форматирование

```bash
npm run lint
npm run format
```

### Тестирование

На данный момент **тесты отсутствуют**. Планируется добавить:
- Unit‑тесты для валидатора и репозитория (xUnit + Moq).
- Интеграционные тесты с Testcontainers (MongoDB, Redis).
- Нагрузочное тестирование импорта.

---

## 🤝 Участие в разработке

1. Fork репозитория.
2. Создайте ветку для новой функциональности (`git checkout -b feature/amazing-feature`).
3. Соблюдайте кодстайл (`.editorconfig`). Запустите `dotnet format` и `npm run lint` перед коммитом.
4. Напишите тесты (если есть).
5. Обновите документацию (`README.md`, `CHANGELOG.md`).
6. Отправьте pull request в ветку `master`.

---

## 📌 Версионирование

Проект следует [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

| Версия | Дата | Статус | Ссылка |
|--------|------|--------|--------|
| 1.0.0 | 2026-06-10 | ✅ Стабильный релиз | [CHANGELOG.md](CHANGELOG.md) |
| 1.1.0 | 2026-06-10 | ✅ Стабильный релиз (добавлено редактирование) | [CHANGELOG.md](CHANGELOG.md) |

---

## 📝 CHANGELOG

Все заметные изменения документируются в [CHANGELOG.md](CHANGELOG.md).

---

## 📜 Лицензия и права

© 2026 Старинин Андрей Николаевич, ООО «Компьютерная Академия Топ» (ИНН 7724406449).  
Автор программы: Старинин Андрей Николаевич
GitHub: [anst-foto](https://github.com/anst-foto)  
Email: starinin-andrey@ya.ru

Данное программное обеспечение распространяется на условиях лицензии **Apache License 2.0**.  
Ниже приведён русскоязычный краткий текст лицензии. Полный официальный текст на английском языке находится в файле [LICENSE](LICENSE) и на сайте [https://www.apache.org/licenses/LICENSE-2.0](https://www.apache.org/licenses/LICENSE-2.0).

```
                                 Apache License
                           Version 2.0, January 2004
                        http://www.apache.org/licenses/

   Термины и условия использования, воспроизведения и распространения

   1. Определения.

      "Лицензия" означает настоящие условия использования, воспроизведения
      и распространения, изложенные в разделах 1–9 настоящего документа.

      "Лицензиар" означает владельца авторских прав или лицо, уполномоченное
      владельцем авторских прав, которое предоставляет Лицензию.

      "Лицензиат" означает физическое или юридическое лицо, осуществляющее
      действия, разрешённые настоящей Лицензией.

      "Произведение" (или "Программа") означает программное обеспечение,
      охраняемое авторским правом, предоставляемое по этой Лицензии.

   2. Предоставление лицензии.

      Лицензиар настоящим предоставляет Лицензиату бессрочную, всемирную,
      безвозмездную, неисключительную лицензию на использование, воспроизведение,
      подготовку производных произведений, публичное отображение, публичное
      исполнение, сублицензирование и распространение Произведения и его
      производных на условиях настоящей Лицензии.

   3. Условия распространения.

      При каждом распространении Произведения или его производной части
      Лицензиат обязан сохранять все уведомления об авторских правах,
      уведомления о патентах, уведомления об отсутствии гарантий и ссылку
      на настоящую Лицензию. Распространение должно осуществляться на тех же
      условиях, что и исходное Произведение.

   4. Ограничение ответственности.

      ПРОГРАММА ПРЕДОСТАВЛЯЕТСЯ "КАК ЕСТЬ", БЕЗ КАКИХ-ЛИБО ГАРАНТИЙ, ЯВНЫХ
      ИЛИ ПОДРАЗУМЕВАЕМЫХ, ВКЛЮЧАЯ, НО НЕ ОГРАНИЧИВАЯСЬ, ГАРАНТИЯМИ ТОВАРНОЙ
      ПРИГОДНОСТИ, ПРИГОДНОСТИ ДЛЯ ОПРЕДЕЛЁННОЙ ЦЕЛИ И ОТСУТСТВИЯ НАРУШЕНИЙ ПРАВ.
      НИ В КОЕМ СЛУЧАЕ АВТОРЫ ИЛИ ПРАВООБЛАДАТЕЛИ НЕ НЕСУТ ОТВЕТСТВЕННОСТИ
      ПО КАКИМ-ЛИБО ИСКАМ, УБЫТКАМ ИЛИ ДРУГИМ ТРЕБОВАНИЯМ.

   5. Принятие лицензии.

      Используя Программу, Лицензиат подтверждает своё согласие с условиями
      настоящей Лицензии.
```

---

## 🔗 Ссылки

- **Репозиторий:** [https://github.com/itedu-tool/exerciser](https://github.com/itedu-tool/exerciser)
- **Docker образ (реестр):** [https://hub.docker.com/repository/docker/anstfoto/exerciser-webapi/general](https://hub.docker.com/repository/docker/anstfoto/exerciser-webapi/general)
- **Автор на GitHub:** [https://github.com/anst-foto](https://github.com/anst-foto)
- **ООО «Компьютерная Академия Топ»:** [https://top-academy.ru](https://top-academy.ru)

---

## 👥 Поддержка

- **Сообщить об ошибке / предложить улучшение:** [GitHub Issues](https://github.com/itedu-tool/exerciser/issues)
- **Документация:** [README.md](README.md) и [docs/](docs/)
- **Docker образ:** [anstfoto/exerciser-webapi](https://hub.docker.com/repository/registry-1.docker.io/anstfoto/exerciser-webapi/general)
- **Электронная почта:** starinin-andrey@ya.ru (автор)

---

**Последнее обновление:** 2026-06-11
**Версия API:** 1.1.1
**.NET версия:** 10.0  
**MongoDB версия:** 8.3