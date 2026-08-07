-- ============================================================
-- FoodExpress - Seed du catalogue (restaurants + plats + images)
-- Idempotent : exÃ©cutable plusieurs fois sans doublons.
-- Usage :  docker exec -i postgres-restaurant psql -U admin -d RestaurantDb < scripts/seed-catalogue.sql
-- ============================================================

-- >>> Restaurants <<<
INSERT INTO "Restaurants" ("Id", "Name", "Description", "Address", "City", "PhoneNumber", "Email",
  "LogoUrl", "CoverImageUrl", "Latitude", "Longitude", "OpeningTime", "ClosingTime",
  "Rating", "IsActive", "IsOpen", "OwnerId", "CreatedAt", "UpdatedAt")
SELECT 'aaaaaaaa-0000-0000-0000-0000000000a1', 'Pizza Roma', 'Pizzas italiennes au feu de bois',
  'Centre-ville', 'Casablanca', '0522-123456', 'contact@pizzaroma.ma',
  'https://images.unsplash.com/photo-1513104890138-7c749659a591?w=600&q=75',
  'https://images.unsplash.com/photo-1414235077428-338989a2e8c0?w=1200&q=75',
  33.5731, -7.5898, '11:00', '23:00', 4.6, true, true,
  'bbbbbbbb-0000-0000-0000-000000000001', now(), now()
WHERE NOT EXISTS (SELECT 1 FROM "Restaurants" WHERE "Name" = 'Pizza Roma');

INSERT INTO "Restaurants" ("Id", "Name", "Description", "Address", "City", "PhoneNumber", "Email",
  "LogoUrl", "CoverImageUrl", "Latitude", "Longitude", "OpeningTime", "ClosingTime",
  "Rating", "IsActive", "IsOpen", "OwnerId", "CreatedAt", "UpdatedAt")
SELECT 'aaaaaaaa-0000-0000-0000-0000000000a2', 'Sushi House', 'Sushis et sashimis frais prepares a la commande',
  'Centre-ville', 'Casablanca', '0522-301145', 'hello@sushihouse.ma',
  'https://images.unsplash.com/photo-1579871494447-9811cf80d66c?w=600&q=75',
  'https://images.unsplash.com/photo-1517248135467-4c7edcad34c4?w=1200&q=75',
  33.5892, -7.6114, '11:30', '22:30', 4.7, true, true,
  'bbbbbbbb-0000-0000-0000-000000000001', now(), now()
WHERE NOT EXISTS (SELECT 1 FROM "Restaurants" WHERE "Name" = 'Sushi House');

INSERT INTO "Restaurants" ("Id", "Name", "Description", "Address", "City", "PhoneNumber", "Email",
  "LogoUrl", "CoverImageUrl", "Latitude", "Longitude", "OpeningTime", "ClosingTime",
  "Rating", "IsActive", "IsOpen", "OwnerId", "CreatedAt", "UpdatedAt")
SELECT 'aaaaaaaa-0000-0000-0000-0000000000a3', 'Burger Palace', 'Burgers artisanaux, frites maison',
  'Avenue Hassan II', 'Rabat', '0537-322110', 'contact@burgerpalace.ma',
  'https://images.unsplash.com/photo-1568901346375-23c9450c58cd?w=600&q=75',
  'https://images.unsplash.com/photo-1555396273-367ea4eb4db5?w=1200&q=75',
  34.0209, -6.8416, '10:00', '23:00', 4.3, true, true,
  'bbbbbbbb-0000-0000-0000-000000000002', now(), now()
WHERE NOT EXISTS (SELECT 1 FROM "Restaurants" WHERE "Name" = 'Burger Palace');

INSERT INTO "Restaurants" ("Id", "Name", "Description", "Address", "City", "PhoneNumber", "Email",
  "LogoUrl", "CoverImageUrl", "Latitude", "Longitude", "OpeningTime", "ClosingTime",
  "Rating", "IsActive", "IsOpen", "OwnerId", "CreatedAt", "UpdatedAt")
SELECT 'aaaaaaaa-0000-0000-0000-0000000000a4', 'Tacos Locos', 'Tacos gourmands, frites maison',
  'Maarif', 'Casablanca', '0522-785412', 'hello@tacoslocos.ma',
  'https://images.unsplash.com/photo-1565299585323-38d6b0865b47?w=600&q=75',
  'https://images.unsplash.com/photo-1424847651672-bf20a4b0982b?w=1200&q=75',
  33.5731, -7.5891, '11:00', '01:00', 4.1, true, true,
  'bbbbbbbb-0000-0000-0000-000000000003', now(), now()
WHERE NOT EXISTS (SELECT 1 FROM "Restaurants" WHERE "Name" = 'Tacos Locos');

INSERT INTO "Restaurants" ("Id", "Name", "Description", "Address", "City", "PhoneNumber", "Email",
  "LogoUrl", "CoverImageUrl", "Latitude", "Longitude", "OpeningTime", "ClosingTime",
  "Rating", "IsActive", "IsOpen", "OwnerId", "CreatedAt", "UpdatedAt")
SELECT 'aaaaaaaa-0000-0000-0000-0000000000a5', 'Douceur & Co', 'Patisserie fine et desserts gourmands',
  'Gauthier', 'Casablanca', '0522-998877', 'bonjour@douceurco.ma',
  'https://images.unsplash.com/photo-1464349095431-e9a21285b5f3?w=600&q=75',
  'https://images.unsplash.com/photo-1495474472287-4d71bcdd2085?w=1200&q=75',
  33.6012, -7.6324, '09:00', '22:00', 4.6, true, true,
  'bbbbbbbb-0000-0000-0000-000000000004', now(), now()
WHERE NOT EXISTS (SELECT 1 FROM "Restaurants" WHERE "Name" = 'Douceur & Co');

INSERT INTO "Restaurants" ("Id", "Name", "Description", "Address", "City", "PhoneNumber", "Email",
  "LogoUrl", "CoverImageUrl", "Latitude", "Longitude", "OpeningTime", "ClosingTime",
  "Rating", "IsActive", "IsOpen", "OwnerId", "CreatedAt", "UpdatedAt")
SELECT 'aaaaaaaa-0000-0000-0000-0000000000a6', 'Salad Bar Bio', 'Salades composees et bowls healthy',
  'Agdal', 'Rabat', '0537-665544', 'bio@saladbar.ma',
  'https://images.unsplash.com/photo-1546069901-ba9599a7e63c?w=600&q=75',
  'https://images.unsplash.com/photo-1512058564366-18510be2db19?w=1200&q=75',
  33.9716, -6.8498, '10:00', '21:00', 4.5, true, true,
  'bbbbbbbb-0000-0000-0000-000000000005', now(), now()
WHERE NOT EXISTS (SELECT 1 FROM "Restaurants" WHERE "Name" = 'Salad Bar Bio');

-- ============================================================
-- Plats (catÃ©gories seedÃ©es par la migration : Pizza=111..1, Burger=222..2,
-- Sushi=333..3, Tacos=444..4, Salades=555..5, Desserts=666..6)
-- ============================================================
INSERT INTO "Dishes" ("Id", "Name", "Description", "Price", "ImageUrl", "Stock", "IsAvailable",
  "IsVegetarian", "IsSpicy", "PreparationTimeMinutes", "RestaurantId", "CategoryId", "CreatedAt", "UpdatedAt")
SELECT 'b1b1b1b1-0000-0000-0000-000000000001', 'Pizza Margherita', 'Pizza tomate, mozzarella, basilic',
  75, 'https://images.unsplash.com/photo-1574071318508-1cdbab80d002?w=600&q=70', 47, true,
  true, false, 20, 'aaaaaaaa-0000-0000-0000-0000000000a1', '11111111-1111-1111-1111-111111111111', now(), now()
WHERE NOT EXISTS (SELECT 1 FROM "Dishes" WHERE "Name" = 'Pizza Margherita');

INSERT INTO "Dishes" ("Id", "Name", "Description", "Price", "ImageUrl", "Stock", "IsAvailable",
  "IsVegetarian", "IsSpicy", "PreparationTimeMinutes", "RestaurantId", "CategoryId", "CreatedAt", "UpdatedAt")
SELECT 'b2b2b2b2-0000-0000-0000-000000000001', 'Salmon Sashimi', '6 tranches de saumon superieur, wasabi, gingembre',
  95, 'https://images.unsplash.com/photo-1553621042-f6e147245754?w=600&q=70', 30, true, false, false, 20,
  'aaaaaaaa-0000-0000-0000-0000000000a2', '33333333-3333-3333-3333-333333333333', now(), now()
WHERE NOT EXISTS (SELECT 1 FROM "Dishes" WHERE "Name" = 'Salmon Sashimi');

INSERT INTO "Dishes" ("Id", "Name", "Description", "Price", "ImageUrl", "Stock", "IsAvailable",
  "IsVegetarian", "IsSpicy", "PreparationTimeMinutes", "RestaurantId", "CategoryId", "CreatedAt", "UpdatedAt")
SELECT 'b2b2b2b2-0000-0000-0000-000000000002', 'California Roll', '8 pieces crabe, avocat, concombre, riz vinaigre',
  85, 'https://images.unsplash.com/photo-1585032226651-759b368d7246?w=600&q=70', 25, true, false, false, 20,
  'aaaaaaaa-0000-0000-0000-0000000000a2', '33333333-3333-3333-3333-333333333333', now(), now()
WHERE NOT EXISTS (SELECT 1 FROM "Dishes" WHERE "Name" = 'California Roll');

INSERT INTO "Dishes" ("Id", "Name", "Description", "Price", "ImageUrl", "Stock", "IsAvailable",
  "IsVegetarian", "IsSpicy", "PreparationTimeMinutes", "RestaurantId", "CategoryId", "CreatedAt", "UpdatedAt")
SELECT 'b2b2b2b2-0000-0000-0000-000000000003', 'Sushi Mixte 12 pcs', 'Assortiment saumon, thon et ebi',
  140, 'https://images.unsplash.com/photo-1579871494447-9811cf80d66c?w=600&q=70', 20, true, false, false, 25,
  'aaaaaaaa-0000-0000-0000-0000000000a2', '33333333-3333-3333-3333-333333333333', now(), now()
WHERE NOT EXISTS (SELECT 1 FROM "Dishes" WHERE "Name" = 'Sushi Mixte 12 pcs');

INSERT INTO "Dishes" ("Id", "Name", "Description", "Price", "ImageUrl", "Stock", "IsAvailable",
  "IsVegetarian", "IsSpicy", "PreparationTimeMinutes", "RestaurantId", "CategoryId", "CreatedAt", "UpdatedAt")
SELECT 'b2b2b2b2-0000-0000-0000-000000000004', 'Poke Bowl Saumon', 'Riz, saumon, edamame, avocat, sauce soja',
  115, 'https://images.unsplash.com/photo-1546069901-ba9599a7e63c?w=600&q=70', 15, true, false, false, 15,
  'aaaaaaaa-0000-0000-0000-0000000000a2', '33333333-3333-3333-3333-333333333333', now(), now()
WHERE NOT EXISTS (SELECT 1 FROM "Dishes" WHERE "Name" = 'Poke Bowl Saumon');

INSERT INTO "Dishes" ("Id", "Name", "Description", "Price", "ImageUrl", "Stock", "IsAvailable",
  "IsVegetarian", "IsSpicy", "PreparationTimeMinutes", "RestaurantId", "CategoryId", "CreatedAt", "UpdatedAt")
SELECT 'b3b3b3b3-0000-0000-0000-000000000001', 'Classic Smash Burger', 'Double boeuf smash, cheddar, pickles, sauce secrete',
  75, 'https://images.unsplash.com/photo-1550547660-d9450f859349?w=600&q=70', 40, true, false, false, 15,
  'aaaaaaaa-0000-0000-0000-0000000000a3', '22222222-2222-2222-2222-222222222222', now(), now()
WHERE NOT EXISTS (SELECT 1 FROM "Dishes" WHERE "Name" = 'Classic Smash Burger');

INSERT INTO "Dishes" ("Id", "Name", "Description", "Price", "ImageUrl", "Stock", "IsAvailable",
  "IsVegetarian", "IsSpicy", "PreparationTimeMinutes", "RestaurantId", "CategoryId", "CreatedAt", "UpdatedAt")
SELECT 'b3b3b3b3-0000-0000-0000-000000000002', 'Chicken Crispy', 'Poulet panne croustillant, coleslaw, mayo barbecue',
  65, 'https://images.unsplash.com/photo-1561758033-d89a9ad46330?w=600&q=70', 40, true, false, false, 15,
  'aaaaaaaa-0000-0000-0000-0000000000a3', '22222222-2222-2222-2222-222222222222', now(), now()
WHERE NOT EXISTS (SELECT 1 FROM "Dishes" WHERE "Name" = 'Chicken Crispy');

INSERT INTO "Dishes" ("Id", "Name", "Description", "Price", "ImageUrl", "Stock", "IsAvailable",
  "IsVegetarian", "IsSpicy", "PreparationTimeMinutes", "RestaurantId", "CategoryId", "CreatedAt", "UpdatedAt")
SELECT 'b3b3b3b3-0000-0000-0000-000000000003', 'Bacon Cheese', 'Boeuf, bacon grille, double cheddar, oignons fondants',
  88, 'https://images.unsplash.com/photo-1571091718767-18b5b1457add?w=600&q=70', 35, true, false, false, 18,
  'aaaaaaaa-0000-0000-0000-0000000000a3', '22222222-2222-2222-2222-222222222222', now(), now()
WHERE NOT EXISTS (SELECT 1 FROM "Dishes" WHERE "Name" = 'Bacon Cheese');

INSERT INTO "Dishes" ("Id", "Name", "Description", "Price", "ImageUrl", "Stock", "IsAvailable",
  "IsVegetarian", "IsSpicy", "PreparationTimeMinutes", "RestaurantId", "CategoryId", "CreatedAt", "UpdatedAt")
SELECT 'b3b3b3b3-0000-0000-0000-000000000004', 'Veggie Burger', 'Galette de legumes, avocat, sauce au yaourt',
  62, 'https://images.unsplash.com/photo-1512621776951-a57141f2eefd?w=600&q=70', 25, true, true, false, 15,
  'aaaaaaaa-0000-0000-0000-0000000000a3', '22222222-2222-2222-2222-222222222222', now(), now()
WHERE NOT EXISTS (SELECT 1 FROM "Dishes" WHERE "Name" = 'Veggie Burger');

INSERT INTO "Dishes" ("Id", "Name", "Description", "Price", "ImageUrl", "Stock", "IsAvailable",
  "IsVegetarian", "IsSpicy", "PreparationTimeMinutes", "RestaurantId", "CategoryId", "CreatedAt", "UpdatedAt")
SELECT 'b4b4b4b4-0000-0000-0000-000000000001', 'Tacos Viande Hachee', 'Viande hachee, frites, fromage, sauce tacos',
  55, 'https://images.unsplash.com/photo-1552332386-f8dd00dc2f85?w=600&q=70', 50, true, false, true, 10,
  'aaaaaaaa-0000-0000-0000-0000000000a4', '44444444-4444-4444-4444-444444444444', now(), now()
WHERE NOT EXISTS (SELECT 1 FROM "Dishes" WHERE "Name" = 'Tacos Viande Hachee');

INSERT INTO "Dishes" ("Id", "Name", "Description", "Price", "ImageUrl", "Stock", "IsAvailable",
  "IsVegetarian", "IsSpicy", "PreparationTimeMinutes", "RestaurantId", "CategoryId", "CreatedAt", "UpdatedAt")
SELECT 'b4b4b4b4-0000-0000-0000-000000000002', 'Tacos Poulet', 'Poulet marine, frites, cheddar, sauce blanche',
  50, 'https://images.unsplash.com/photo-1551218808-94e220e084d2?w=600&q=70', 50, true, false, false, 10,
  'aaaaaaaa-0000-0000-0000-0000000000a4', '44444444-4444-4444-4444-444444444444', now(), now()
WHERE NOT EXISTS (SELECT 1 FROM "Dishes" WHERE "Name" = 'Tacos Poulet');

INSERT INTO "Dishes" ("Id", "Name", "Description", "Price", "ImageUrl", "Stock", "IsAvailable",
  "IsVegetarian", "IsSpicy", "PreparationTimeMinutes", "RestaurantId", "CategoryId", "CreatedAt", "UpdatedAt")
SELECT 'b4b4b4b4-0000-0000-0000-000000000003', 'Tacos Mixte', 'Viande + poulet, double fromage, sauce samurai',
  68, 'https://images.unsplash.com/photo-1529692236671-f1f6cf9683ba?w=600&q=70', 45, true, false, true, 12,
  'aaaaaaaa-0000-0000-0000-0000000000a4', '44444444-4444-4444-4444-444444444444', now(), now()
WHERE NOT EXISTS (SELECT 1 FROM "Dishes" WHERE "Name" = 'Tacos Mixte');

INSERT INTO "Dishes" ("Id", "Name", "Description", "Price", "ImageUrl", "Stock", "IsAvailable",
  "IsVegetarian", "IsSpicy", "PreparationTimeMinutes", "RestaurantId", "CategoryId", "CreatedAt", "UpdatedAt")
SELECT 'b5b5b5b5-0000-0000-0000-000000000001', 'Cheesecake Framboise', 'Cheesecake cuit, coulis de framboise',
  60, 'https://images.unsplash.com/photo-1565958011703-44f9829ba187?w=600&q=70', 20, true, true, false, 25,
  'aaaaaaaa-0000-0000-0000-0000000000a5', '66666666-6666-6666-6666-666666666666', now(), now()
WHERE NOT EXISTS (SELECT 1 FROM "Dishes" WHERE "Name" = 'Cheesecake Framboise');

INSERT INTO "Dishes" ("Id", "Name", "Description", "Price", "ImageUrl", "Stock", "IsAvailable",
  "IsVegetarian", "IsSpicy", "PreparationTimeMinutes", "RestaurantId", "CategoryId", "CreatedAt", "UpdatedAt")
SELECT 'b5b5b5b5-0000-0000-0000-000000000002', 'Tiramisu Italien', 'Mascarpone, cafe, cacao',
  55, 'https://images.unsplash.com/photo-1495474472287-4d71bcdd2085?w=600&q=70', 20, true, true, false, 15,
  'aaaaaaaa-0000-0000-0000-0000000000a5', '66666666-6666-6666-6666-666666666666', now(), now()
WHERE NOT EXISTS (SELECT 1 FROM "Dishes" WHERE "Name" = 'Tiramisu Italien');

INSERT INTO "Dishes" ("Id", "Name", "Description", "Price", "ImageUrl", "Stock", "IsAvailable",
  "IsVegetarian", "IsSpicy", "PreparationTimeMinutes", "RestaurantId", "CategoryId", "CreatedAt", "UpdatedAt")
SELECT 'b5b5b5b5-0000-0000-0000-000000000003', 'Pancakes Nutella', '4 pancakes moelleux, Nutella, banane',
  65, 'https://images.unsplash.com/photo-1567620905732-2d1ec7ab7445?w=600&q=70', 25, true, true, false, 15,
  'aaaaaaaa-0000-0000-0000-0000000000a5', '66666666-6666-6666-6666-666666666666', now(), now()
WHERE NOT EXISTS (SELECT 1 FROM "Dishes" WHERE "Name" = 'Pancakes Nutella');

INSERT INTO "Dishes" ("Id", "Name", "Description", "Price", "ImageUrl", "Stock", "IsAvailable",
  "IsVegetarian", "IsSpicy", "PreparationTimeMinutes", "RestaurantId", "CategoryId", "CreatedAt", "UpdatedAt")
SELECT 'b6b6b6b6-0000-0000-0000-000000000001', 'Cesar Bowl', 'Poulet grille, romaine, parmesan, crocant',
  65, 'https://images.unsplash.com/photo-1540420773420-3366772f4999?w=600&q=70', 20, true, false, false, 10,
  'aaaaaaaa-0000-0000-0000-0000000000a6', '55555555-5555-5555-5555-555555555555', now(), now()
WHERE NOT EXISTS (SELECT 1 FROM "Dishes" WHERE "Name" = 'Cesar Bowl');

INSERT INTO "Dishes" ("Id", "Name", "Description", "Price", "ImageUrl", "Stock", "IsAvailable",
  "IsVegetarian", "IsSpicy", "PreparationTimeMinutes", "RestaurantId", "CategoryId", "CreatedAt", "UpdatedAt")
SELECT 'b6b6b6b6-0000-0000-0000-000000000002', 'Avocado Bowl', 'Avocat, quinoa, legumes grilles, vinaigrette citron',
  70, 'https://images.unsplash.com/photo-1505253716366-afaea1d3d1af?w=600&q=70', 20, true, true, false, 10,
  'aaaaaaaa-0000-0000-0000-0000000000a6', '55555555-5555-5555-5555-555555555555', now(), now()
WHERE NOT EXISTS (SELECT 1 FROM "Dishes" WHERE "Name" = 'Avocado Bowl');

INSERT INTO "Dishes" ("Id", "Name", "Description", "Price", "ImageUrl", "Stock", "IsAvailable",
  "IsVegetarian", "IsSpicy", "PreparationTimeMinutes", "RestaurantId", "CategoryId", "CreatedAt", "UpdatedAt")
SELECT 'b6b6b6b6-0000-0000-0000-000000000003', 'Salade Thon', 'Thon, oeuf, mais, tomates, roquette',
  58, 'https://images.unsplash.com/photo-1533089860892-a7c6f0a88666?w=600&q=70', 25, true, false, false, 10,
  'aaaaaaaa-0000-0000-0000-0000000000a6', '55555555-5555-5555-5555-555555555555', now(), now()
WHERE NOT EXISTS (SELECT 1 FROM "Dishes" WHERE "Name" = 'Salade Thon');

SELECT count(*) AS "Restaurants" FROM "Restaurants";
SELECT count(*) AS "Dishes" FROM "Dishes";
