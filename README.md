# Marking Codes TCP Server + Client

Тестовое задание: TCP Server + TCP Client на C#/.NET 10.

## Описание

Проект состоит из двух приложений:

- **EmsTcpServer** — генерирует коды маркировки в формате GS1 и отправляет их клиенту каждые 500 мс.
- **EmsTcpClient** — принимает коды и сохраняет их в файл.

Приложения взаимодействуют по TCP с использованием `async/await`.

## Проекты

- `EmsTcpServer` — TCP Server
- `EmsTcpClient` — TCP Client
- `Tests` — Unit-тесты (NUnit)

## Запуск локально

### Server
`dotnet run --project EmsTcpServer`

Сервер запускается на порту 5000.

### Client
`dotnet run --project EmsTcpClient`

Клиент подключается к серверу и сохраняет полученные коды в файл: marking-codes.txt

## Запуск через Docker

Из корневой директории проекта:

`docker compose up --build`

## Тесты

Запуск тестов:

`dotnet test`