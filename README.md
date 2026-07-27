# 🍔 FoodExpress - Plateforme de Livraison de Repas

Application de livraison de repas type Uber Eats, développée avec une architecture **microservices** en **ASP.NET Core (.NET 10)**.

## 🏗️ Architecture

Le projet suit une architecture microservices avec :

- **API Gateway** (YARP) - Point d'entrée unique
- **3 Microservices** indépendants (Restaurant, Order, User)
- **Communication** synchrone (HTTP) et asynchrone (RabbitMQ)
- **Base de données par service** (PostgreSQL)
- **Data Warehouse** pour l'analytique
- **Cache distribué** (Redis)
- **Stockage d'objets** (MinIO)
- **Authentification centralisée** (Keycloak)
- **Logs centralisés** (ELK Stack)

## 📂 Structure du projet

\`\`\`
FoodExpress/
├── src/
│   ├── ApiGateway/
│   │   └── FoodExpress.Gateway/          # API Gateway (YARP)
│   ├── Services/
│   │   ├── FoodExpress.Restaurant.API/   # Service Restaurants
│   │   ├── FoodExpress.Order.API/        # Service Commandes
│   │   └── FoodExpress.User.API/         # Service Utilisateurs
│   ├── BuildingBlocks/
│   │   ├── FoodExpress.Common/           # Code partagé
│   │   └── FoodExpress.EventBus/         # Gestion événements
│   └── Workers/
│       └── FoodExpress.DW.Worker/        # ETL Data Warehouse
└── docker-compose.yml                    # Infrastructure Docker
\`\`\`

## 🛠️ Technologies utilisées

### Backend
- **.NET 10** / **ASP.NET Core**
- **Entity Framework Core**
- **YARP** (API Gateway)
- **MassTransit** + **RabbitMQ** (Messaging)

### Infrastructure
- **PostgreSQL** (Bases de données)
- **Redis** (Cache)
- **MinIO** (Stockage fichiers)
- **Keycloak** (Authentification)
- **Elasticsearch + Kibana** (Logs)
- **Docker** (Conteneurisation)

## 🚀 Démarrage

### Prérequis
- .NET 10 SDK
- Docker Desktop
- Visual Studio 2026 / Rider / VS Code

### Lancer l'infrastructure
\`\`\`bash
docker-compose up -d
\`\`\`

### Vérifier les services
- RabbitMQ : http://localhost:15672 (admin/admin123)
- MinIO : http://localhost:9001 (minioadmin/minioadmin123)
- Keycloak : http://localhost:8080 (admin/admin123)
- Kibana : http://localhost:5601

## 📊 Ports utilisés

| Service | Port |
|---------|------|
| Gateway | 5000 |
| Restaurant API | 5001 |
| Order API | 5002 |
| User API | 5003 |
| PostgreSQL Restaurant | 5432 |
| PostgreSQL Order | 5433 |
| PostgreSQL User | 5434 |
| PostgreSQL DW | 5435 |
| Redis | 6379 |
| MinIO | 9000/9001 |
| Keycloak | 8080 |
| RabbitMQ | 5672/15672 |
| Elasticsearch | 9200 |
| Kibana | 5601 |

## 👤 Auteur

Projet réalisé dans le cadre d'un projet universitaire.

## 📝 License

Ce projet est sous licence MIT.