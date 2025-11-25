-- Fix Place ID corruption via EF
-- Execute using: dotnet ef database update --connection "Server=81.95.105.76;Database=e003918;User Id=e003918a;Password=Searchminds123!;TrustServerCertificate=true;ConnectRetryCount=3;"

-- First, check current state
SELECT Name, PlaceId FROM Companies WHERE Name LIKE '%Krögers%' OR Name LIKE '%Searchminds%' OR Name LIKE '%Familj%';

-- Fix the corrupted Place IDs
UPDATE Companies SET PlaceId = 'ChIJZfgXlGTRV0YRUIVdUWnXc1M' WHERE Name = 'Krögers';
UPDATE Companies SET PlaceId = 'ChIJ4WAYH4y9U0YRXe06sXvxMGo' WHERE Name = 'Familjeterapeuterna Syd AB';
UPDATE Companies SET PlaceId = '0x465f9dc1dcaa0959' WHERE Name = 'Familjeterapeuterna Syd Södermalm Sthlm';
UPDATE Companies SET PlaceId = 'ChIJnytgooPRV0YRTCdMAyVMLt4' WHERE Name = 'Searchminds';
UPDATE Companies SET PlaceId = 'ChIJKdrs-aKXU0YRPgCk6_o75X0' WHERE Name = 'familjeterapeuterna syd lund';

-- Clean up any other corrupted entries
UPDATE Companies SET PlaceId = NULL WHERE PlaceId LIKE '%No reviews available%';
UPDATE Companies SET PlaceId = NULL WHERE PlaceId LIKE '%Last updated%';
UPDATE Companies SET PlaceId = NULL WHERE PlaceId LIKE '%Place ID:%';
UPDATE Companies SET PlaceId = NULL WHERE PlaceId LIKE '%B&B%';
UPDATE Companies SET PlaceId = NULL WHERE LEN(PlaceId) > 100;

-- Verify the fix
SELECT Name, PlaceId FROM Companies ORDER BY Name;