# Руководство для участников

Спасибо, что решили помочь проекту Exerciser!

---

## Процесс разработки

1. **Заведите issue** — опишите баг или предложение.
2. **Создайте ветку** от `develop`:
   - `feature/<описание>` — для нового функционала.
   - `fix/<описание>` — для исправлений.
   - `hotfix/<описание>` — для срочных правок в `master`.
3. **Реализуйте и протестируйте** изменения.
4. **Создайте Pull Request** в `develop`.

---

## Требования к коду

- **C#**: следуйте [C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions).
- **Vue 3**: Composition API, `<script setup>`.
- **PR проходит CI** (сборка и тесты должны быть зелёными).
- **Новый функционал** должен быть покрыт тестами.

---

## Оформление коммитов

Проект использует **Conventional Commits**. Подробно — [COMMIT_CONVENTION.md](./docs/COMMIT_CONVENTION.md).

Кратко:

```
<тип>[(<область>)]: <описание>
```

Примеры: `feat(security): …`, `fix(api): …`, `test: …`.

---

## Оформление Pull Request

1. Название PR — как заголовок коммита (Conventional Commits).
2. В описании укажите:
   - Что сделано.
   - Номер issue (если есть).
   - Как протестировать.
3. Запросите Code Review.

---

## Запуск проекта

```bash
# Бэкенд (требуется MongoDB + Redis)
docker compose up -d mongodb redis
dotnet run --project Exerciser.WebApi

# Студенческий фронтенд
cd Exerciser.FrontEnd/Student
npm install
npm run dev

# Админ-панель — открыть Exerciser.FrontEnd/Admin/index.html в браузере
# (или через любой статический сервер)
```

---

## Связь

- Issues и PR через GitHub.
- Обсуждения — в соответствующем разделе репозитория.
