-- Fix Place ID corruption
-- Restore correct Place IDs from backup data

UPDATE Companies SET PlaceId = 'ChIJZfgXlGTRV0YRUIVdUWnXc1M' WHERE Name = 'Krögers';
UPDATE Companies SET PlaceId = 'ChIJ4WAYH4y9U0YRXe06sXvxMGo' WHERE Name = 'Familjeterapeuterna Syd AB';
UPDATE Companies SET PlaceId = '0x465f9dc1dcaa0959' WHERE Name = 'Familjeterapeuterna Syd Södermalm Sthlm';
UPDATE Companies SET PlaceId = 'ChIJnytgooPRV0YRTCdMAyVMLt4' WHERE Name = 'Searchminds';
UPDATE Companies SET PlaceId = NULL WHERE Name = 'Familjeterapeuterna syd uppsala' AND PlaceId IS NOT NULL;
UPDATE Companies SET PlaceId = 'ChIJKdrs-aKXU0YRPgCk6_o75X0' WHERE Name = 'familjeterapeuterna syd lund';

-- Also check for any other corrupted Place IDs that might contain company names or review text
UPDATE Companies SET PlaceId = NULL WHERE PlaceId LIKE '%No reviews available%';
UPDATE Companies SET PlaceId = NULL WHERE PlaceId LIKE '%Last updated%';
UPDATE Companies SET PlaceId = NULL WHERE PlaceId LIKE '%Place ID:%';
UPDATE Companies SET PlaceId = NULL WHERE PlaceId LIKE '%B&B%';
UPDATE Companies SET PlaceId = NULL WHERE LEN(PlaceId) > 100; -- Place IDs should be much shorter

-- Show results
SELECT Id, Name, PlaceId FROM Companies ORDER BY Name;