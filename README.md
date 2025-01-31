# Beskrivning 

Den här uppgiften innebär att flytta en färdigutvecklad Razor Pages-applikation från en lokal miljö till en säker och
skalbar molnmiljö i Azure. Applikationen använder Google Inloggning (OpenID Connect) för att hantera användarkonton på ett säkert
sätt.

---
## Arbetsprocess

## 1. Skapa en Resource Group

En **Resource Group** är en behållare som samlar alla resurser som hör till samma applikation eller tjänst. 
Projektet inleddes med att skapa en sådan behållare. 


## 2. Skapa en SQL Server och Azure SQL Database

Som authentication method valdes 'SQL authentication method" på servern. 

SQL Authentication Method är en autentiseringsmetod som används för att verifiera användare i en SQL Server-databas, 
oavsett var servern är placerad (lokalt, på Azure eller andra miljöer). När SQL Authentication används, sker
autentiseringen genom att användaren loggar in med ett användarnamn och lösenord som är definierade i själva SQL Server-databasen.

I detta steg lades även en environment variabel ('production' alternativt 'development') till.

Slutligen lades även IP-adresser till under 'Networks' för att säkerställa att servern godkänner dessa.

## 3. Skapa en App Service

Nu skapades en App Service som applikationen kan köras på. 

Ett id genererades till applikationen (managed id) så att den skulle kunna kopplas till ett key vault. 

## 4. Skapa Azure Key Vault

I fjärde steget skapades en Azure Key Vault och la in databassträngen som en secret. 

I detta steg lades även applikationen till i access policyn på Key Vault. 

## 5. Användning av key vault-secret i applikationen

I detta steg lades key vault - strängen in i program.cs. 

## 6. Publicera koden till App Service 

## 7. Publicera koden till github via azure portalen 



