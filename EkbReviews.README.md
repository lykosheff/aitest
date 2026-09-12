# Екатеринбург: интересные отзывы

Бот собирает свежие отзывы заведений Екатеринбурга из **2GIS** и **Яндекс Карт**, находит интересные истории и диалоги с ответами заведений и публикует лучшие кандидаты в Telegram-канал.

## Текущий этап

Реализован полный цикл сбора отзывов:

### 2GIS
- ✅ Поиск заведений по запросу через Catalog API
- ✅ Получение отзывов через Reviews API
- ✅ Пагинация (до 50 отзывов за запрос, максимум 10 страниц)
- ✅ Сохранение ответов организации на отзывы
- ✅ Конфигурация через `TwoGis:*`

### Яндекс Карты
- ✅ Получение отзывов для организаций из базы данных
- ✅ Парсинг через неофициальный API
- ✅ Поддержка официальных ответов организаций
- ⚠️ **Важно**: используется неофициальный endpoint, для продакшена рекомендуется Яндекс.Business API

### Общий процесс
- Worker запускается каждые 30 минут
- Сначала получает организации из 2GIS
- Затем собирает отзывы из 2GIS для найденных организаций
- После собирает отзывы из Яндекс Карт для всех организаций в БД
- AI анализирует отзывы и выбирает наиболее интересные

## Архитектура

- `EkbReviews.Domain` — модели отзывов и кандидатов
- `EkbReviews.Application` — контракты источников, анализатора и пайплайна
- `EkbReviews.Infrastructure` — интеграции:
  - `TwoGisCatalogClient` — поиск организаций в 2GIS
  - `TwoGisReviewClient` — получение отзывов из 2GIS
  - `YandexMapsReviewClient` — получение отзывов из Яндекс Карт
  - `OllamaReviewAnalyzer` — AI анализ через Ollama
- `EkbReviews.Worker` — фоновый процесс `ReviewCollectionWorker`

## Конфигурация

### 2GIS API Key

Получите ключ на [dev.2gis.ru](https://dev.2gis.ru/) и задайте через переменную окружения:

```bash
TwoGis__ApiKey=ваш_ключ
```

### Яндекс Карты

Настройка поисковых запросов в `appsettings.json`:

```json
{
  "YandexMaps": {
    "Enabled": true,
    "SearchQueries": [
      "ресторан Екатеринбург",
      "кафе Екатеринбург"
    ]
  }
}
```

### База данных

```json
{
  "ConnectionStrings": {
    "EkbReviews": "Host=localhost;Port=5432;Database=ekb_reviews;Username=postgres;Password=postgres"
  }
}
```

### AI (Ollama)

```json
{
  "Ai": {
    "Provider": "Ollama",
    "Model": "gpt-oss:20b",
    "BaseUrl": "http://localhost:11434",
    "TimeoutSeconds": 120
  }
}
```

Ключи API в git не коммитим.

## Запуск

```bash
dotnet run --project src/EkbReviews.Worker
```

Или через Docker:

```bash
docker-compose up -d
```

## Структура данных

### ReviewSource
- `TwoGis` — отзывы из 2GIS
- `YandexMaps` — отзывы из Яндекс Карт

### Review
- `Id` — уникальный идентификатор
- `Source` — источник
- `OrganizationId/Name/Address` — данные организации
- `Rating` — оценка
- `AuthorName` — автор
- `PublishedAt` — дата публикации
- `Text` — текст отзыва
- `Photos` — фотографии
- `SourceUrl` — ссылка на оригинал
- `Replies` — ответы организации
- `CollectedAt` — дата сбора

## Ограничения

### 2GIS
- Требуется API ключ
- Лимиты зависят от тарифа API
- Максимум 500 отзывов за организацию (10 страниц по 50)

### Яндекс Карты
- Неофициальный API может меняться без предупреждения
- Может потребоваться авторизация/YaCookie
- Для стабильной работы используйте Яндекс.Business API

## Добавление новых источников

1. Создайте клиент в `EkbReviews.Infrastructure`
2. Добавьте регистрацию в `DependencyInjection.cs`
3. Обновите `ReviewCollectionWorker`
4. Добавьте `ReviewSource` enum если нужно
