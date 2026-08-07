# 🍔 FoodExpress - Plateforme de Livraison de Repas

Application de livraison de repas type Uber Eats, développée avec une architecture **microservices** en **ASP.NET Core (.NET 10)** et un front **React** (dépôt séparé : `FoodExpress.Web`).

## 🏗️ Architecture

- **API Gateway** (YARP) : point d'entrée unique (port 5000) — le front ne parle jamais directement aux services
- **3 microservices** indépendants : Restaurant, Order, User — une base de données chacun (PostgreSQL)
- **Communication asynchrone** : EventBus maison sur `RabbitMQ.Client` — événements de commande (OrderCreated, OrderStatusChanged, OrderDelivered) → mise à jour du **stock décrémenté** à la commande, **restauré** à l'annulation, avec **idempotence** (guard de messages traités, SQLSTATE 23505)
- **Authentification centralisée** : Keycloak (realm `foodexpress`) — chaque service valide le JWT **localement** (discovery JWKS au démarrage, 0 round-trip par requête)
- **Cache distribué** : Redis (liste des restaurants, DTO par id)
- **Stockage d'objets** : MinIO (logos de restaurants, images de plats)
- **Logs centralisés** : Serilog → Elasticsearch → Kibana
- **Seed** : 6 catégories (Pizza, Burger, Sushi, Tacos, Salades, Desserts), 6 restaurants et menus avec images

## 📂 Structure du projet

```
FoodExpress/
├── FoodExpress.Gateway/           # API Gateway (YARP + rate limiting)
├── FoodExpress.Restaurant.API/    # Restaurants, catégories, plats, images (MinIO)
├── FoodExpress.Order.API/         # Commandes, cycle de statuts, assignation livreur
├── FoodExpress.User.API/          # Utilisateurs, adresses, auth (Keycloak)
├── FoodExpress.Common/            # Code partagé
├── FoodExpress.EventBus/          # EventBus RabbitMQ + événements + handlers
├── FoodExpress.DW.Worker/         # ETL Data Warehouse (squelette, non implémenté)
├── FoodExpress.Tests/             # Tests unitaires (xUnit, 40 tests)
├── keycloak/                      # Realm foodexpress exporté (auto-import au premier démarrage)
├── scripts/seed-catalogue.sql     # Seed idempotent : 6 restaurants + 19 plats
└── docker-compose.yml             # Infrastructure : PostgreSQL×4, Redis, RabbitMQ, MinIO, Keycloak, ELK
```

## 🛠️ Technologies

### Backend
- **.NET 10** / **ASP.NET Core**, **Entity Framework Core** (Npgsql)
- **YARP** pour le reverse proxy, **JwtBearer** pour la validation JWT locale
- **RabbitMQ.Client 7** pour l'EventBus maison
- **Serilog** vers Elasticsearch

### Infrastructure
- **PostgreSQL** (4 bases : restaurant, order, user, dw)
- **Redis** (cache) · **MinIO** (stockage fichiers, console :9001)
- **Keycloak** (OIDC, :8080) · **Elasticsearch + Kibana** (logs, :9200 / :5601)
- **RabbitMQ** (console :15672) · **Docker** (conteneurisation)

## 🚀 Démarrage (pour un cloneur)

### Prérequis
- .NET SDK 10, Docker Desktop, Node.js 20+ (pour le front).

### 1. Infrastructure (1 commande)
```bash
docker compose up -d
```
**Keycloak s'auto-configure** : le realm `foodexpress`, le client `foodexpress-api`, le rôle `Customer` et les comptes de démo (`sara@test.com`, `client1@test.com`) sont importés automatiquement depuis `keycloak/` (fichiers montés en lecture seule + `--import-realm`).

### 2. Services backend
Les **migrations EF s'exécutent automatiquement** au démarrage de chaque API (`db.Database.Migrate()` dans Program.cs) — aucune commande `ef` nécessaire.

Via Visual Studio : ouvrir `FoodExpress.slnx`, « Définir les projets de démarrage » → les 4 projets API, puis **F5**.

Via CLI (depuis la racine) :
```bash
dotnet run --project FoodExpress.User.API
dotnet run --project FoodExpress.Restaurant.API
dotnet run --project FoodExpress.Order.API
dotnet run --project FoodExpress.Gateway
```

### 3. Catalogue (optionnel mais recommandé, 1 commande)
```bash
docker exec -i postgres-restaurant psql -U admin -d RestaurantDb < scripts/seed-catalogue.sql
```
Idempotent : lancement multiple sans doublons.

### 4. Frontend (dépôt `FoodExpress.Web`)
```bash
npm install && npm run dev   # http://localhost:5173 (proxy /api → Gateway 5000)
```

### 5. Comptes
Des comptes de démo sont importés automatiquement avec ce fichier `realm.json` (mot de passe **`Demo1234!`** pour tous) :

| Utilisateur | Rôle |
|---|---|
| `sara@test.com` | Customer |
| `client1@test.com` | Customer |
| `test.gateway@test.com` | Customer |
| `seed_owner@test.com` | Customer |
| `ahmed.test` | Customer |

Ou inscrire un nouveau compte via l'application.

## 📊 Ports

| Service | Port |
|---|---|
| Gateway | 5000 |
| Restaurant API 5001 · Order API 5002 · User API 5003 | |
| PostgreSQL restaurant 5432 · order 5433 · user 5434 · dw 5435 | |
| Redis 6379 · MinIO 9000 (API) / 9001 (console) | |
| RabbitMQ 5672 (AMQP) / 15672 (console) | |
| Keycloak 8080 · Elasticsearch 9200 · Kibana 5601 | |

## 🔌 Endpoints principaux (via Gateway)

- Auth : `POST /api/auth/register` · `POST /api/auth/login` (Keycloak)
- Catalogue : `GET /api/restaurants` · `GET /api/restaurants/{id}` · `GET /api/dishes/restaurant/{id}`
- Commandes : `POST /api/orders` · `GET /api/orders/customer/{id}` · `PUT /api/orders/{id}/status` · `POST /api/orders/{id}/cancel` · `POST /api/orders/{id}/assign-delivery`

Cycle de commande : `Pending → Accepted → Preparing → Ready → OnDelivery → Delivered` (± `Cancelled`, autorisé en Pending/Accepted).

## 🧪 Tests

```bash
dotnet test FoodExpress.Tests
```
**40 tests xUnit** couvrant les services (User, Restaurant, Dish, Order), les handlers de stock (OrderCreated décrémente, OrderCancelled restaure) et l'idempotence, avec EF InMemory + Moq. Le front dispose également de tests Vitest (15).

## ⚠️ Limites connues

- `POST /api/restaurants` ne vérifie pas le rôle du propriétaire (OwnerId vient du body) — un workflow de modération est à mettre en place
- `DW.Worker` non implémenté (squelette)
- Espaces livreur et admin non construits

## 👤 Auteur

Projet réalisé dans le cadre d'un projet universitaire.

## 📝 License

MIT.