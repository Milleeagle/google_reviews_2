-- Database Export for Web Hosting
-- Generated on: 2025-09-11 13:11:47
-- Database: aspnet_google_reviews
-- 
-- This file is optimized for web hosting import
--

SET SQL_MODE = "NO_AUTO_VALUE_ON_ZERO";
SET AUTOCOMMIT = 0;
START TRANSACTION;
SET time_zone = "+00:00";

--
-- Database structure and data
--

-- MySQL dump 10.13  Distrib 8.0.43, for Win64 (x86_64)
--
-- Host: localhost    Database: aspnet_google_reviews
-- ------------------------------------------------------
-- Server version	8.0.43

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!50503 SET NAMES utf8 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;

--
-- Table structure for table `__efmigrationshistory`
--

DROP TABLE IF EXISTS `__efmigrationshistory`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `__efmigrationshistory` (
  `MigrationId` varchar(150) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `ProductVersion` varchar(32) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  PRIMARY KEY (`MigrationId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `__efmigrationshistory`
--

LOCK TABLES `__efmigrationshistory` WRITE;
/*!40000 ALTER TABLE `__efmigrationshistory` DISABLE KEYS */;
INSERT INTO `__efmigrationshistory` VALUES ('20250910185537_InitialMySqlMigration','8.0.10');
/*!40000 ALTER TABLE `__efmigrationshistory` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `aspnetroleclaims`
--

DROP TABLE IF EXISTS `aspnetroleclaims`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `aspnetroleclaims` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `RoleId` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `ClaimType` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `ClaimValue` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  PRIMARY KEY (`Id`),
  KEY `IX_AspNetRoleClaims_RoleId` (`RoleId`),
  CONSTRAINT `FK_AspNetRoleClaims_AspNetRoles_RoleId` FOREIGN KEY (`RoleId`) REFERENCES `aspnetroles` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `aspnetroleclaims`
--

LOCK TABLES `aspnetroleclaims` WRITE;
/*!40000 ALTER TABLE `aspnetroleclaims` DISABLE KEYS */;
/*!40000 ALTER TABLE `aspnetroleclaims` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `aspnetroles`
--

DROP TABLE IF EXISTS `aspnetroles`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `aspnetroles` (
  `Id` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Name` varchar(256) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `NormalizedName` varchar(256) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `ConcurrencyStamp` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `RoleNameIndex` (`NormalizedName`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `aspnetroles`
--

LOCK TABLES `aspnetroles` WRITE;
/*!40000 ALTER TABLE `aspnetroles` DISABLE KEYS */;
INSERT INTO `aspnetroles` VALUES ('05ee02dd-43d1-4b74-a025-07ac6b801889','User','USER',NULL),('7db25d25-2565-4af2-8aff-1d7ab340f5c0','Admin','ADMIN',NULL);
/*!40000 ALTER TABLE `aspnetroles` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `aspnetuserclaims`
--

DROP TABLE IF EXISTS `aspnetuserclaims`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `aspnetuserclaims` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `UserId` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `ClaimType` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `ClaimValue` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  PRIMARY KEY (`Id`),
  KEY `IX_AspNetUserClaims_UserId` (`UserId`),
  CONSTRAINT `FK_AspNetUserClaims_AspNetUsers_UserId` FOREIGN KEY (`UserId`) REFERENCES `aspnetusers` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `aspnetuserclaims`
--

LOCK TABLES `aspnetuserclaims` WRITE;
/*!40000 ALTER TABLE `aspnetuserclaims` DISABLE KEYS */;
/*!40000 ALTER TABLE `aspnetuserclaims` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `aspnetuserlogins`
--

DROP TABLE IF EXISTS `aspnetuserlogins`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `aspnetuserlogins` (
  `LoginProvider` varchar(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `ProviderKey` varchar(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `ProviderDisplayName` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `UserId` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  PRIMARY KEY (`LoginProvider`,`ProviderKey`),
  KEY `IX_AspNetUserLogins_UserId` (`UserId`),
  CONSTRAINT `FK_AspNetUserLogins_AspNetUsers_UserId` FOREIGN KEY (`UserId`) REFERENCES `aspnetusers` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `aspnetuserlogins`
--

LOCK TABLES `aspnetuserlogins` WRITE;
/*!40000 ALTER TABLE `aspnetuserlogins` DISABLE KEYS */;
/*!40000 ALTER TABLE `aspnetuserlogins` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `aspnetuserroles`
--

DROP TABLE IF EXISTS `aspnetuserroles`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `aspnetuserroles` (
  `UserId` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `RoleId` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  PRIMARY KEY (`UserId`,`RoleId`),
  KEY `IX_AspNetUserRoles_RoleId` (`RoleId`),
  CONSTRAINT `FK_AspNetUserRoles_AspNetRoles_RoleId` FOREIGN KEY (`RoleId`) REFERENCES `aspnetroles` (`Id`) ON DELETE CASCADE,
  CONSTRAINT `FK_AspNetUserRoles_AspNetUsers_UserId` FOREIGN KEY (`UserId`) REFERENCES `aspnetusers` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `aspnetuserroles`
--

LOCK TABLES `aspnetuserroles` WRITE;
/*!40000 ALTER TABLE `aspnetuserroles` DISABLE KEYS */;
INSERT INTO `aspnetuserroles` VALUES ('b769bc4f-51d3-42b7-a23c-534f17ce4547','7db25d25-2565-4af2-8aff-1d7ab340f5c0');
/*!40000 ALTER TABLE `aspnetuserroles` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `aspnetusers`
--

DROP TABLE IF EXISTS `aspnetusers`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `aspnetusers` (
  `Id` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `UserName` varchar(256) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `NormalizedUserName` varchar(256) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `Email` varchar(256) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `NormalizedEmail` varchar(256) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `EmailConfirmed` tinyint(1) NOT NULL,
  `PasswordHash` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `SecurityStamp` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `ConcurrencyStamp` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `PhoneNumber` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `PhoneNumberConfirmed` tinyint(1) NOT NULL,
  `TwoFactorEnabled` tinyint(1) NOT NULL,
  `LockoutEnd` datetime(6) DEFAULT NULL,
  `LockoutEnabled` tinyint(1) NOT NULL,
  `AccessFailedCount` int NOT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `UserNameIndex` (`NormalizedUserName`),
  KEY `EmailIndex` (`NormalizedEmail`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `aspnetusers`
--

LOCK TABLES `aspnetusers` WRITE;
/*!40000 ALTER TABLE `aspnetusers` DISABLE KEYS */;
INSERT INTO `aspnetusers` VALUES ('b769bc4f-51d3-42b7-a23c-534f17ce4547','admin@example.com','ADMIN@EXAMPLE.COM','admin@example.com','ADMIN@EXAMPLE.COM',1,'AQAAAAIAAYagAAAAEP4Au3fuM7FjM31gmYZJagUf49FYsQKWg0pkuhDcpGUnGCZlRcsAX9iCjWd102TZDg==','2YJTHW2PZMOGTAMRA3C7K2JQTCY5TK6M','a18975fa-a811-402a-9d4e-2fa2e3bd2dc8',NULL,0,0,NULL,1,0);
/*!40000 ALTER TABLE `aspnetusers` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `aspnetusertokens`
--

DROP TABLE IF EXISTS `aspnetusertokens`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `aspnetusertokens` (
  `UserId` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `LoginProvider` varchar(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Name` varchar(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Value` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  PRIMARY KEY (`UserId`,`LoginProvider`,`Name`),
  CONSTRAINT `FK_AspNetUserTokens_AspNetUsers_UserId` FOREIGN KEY (`UserId`) REFERENCES `aspnetusers` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `aspnetusertokens`
--

LOCK TABLES `aspnetusertokens` WRITE;
/*!40000 ALTER TABLE `aspnetusertokens` DISABLE KEYS */;
/*!40000 ALTER TABLE `aspnetusertokens` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `companies`
--

DROP TABLE IF EXISTS `companies`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `companies` (
  `Id` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Name` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `PlaceId` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `GoogleMapsUrl` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `IsActive` tinyint(1) NOT NULL,
  `LastUpdated` datetime(6) NOT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `IX_Companies_PlaceId` (`PlaceId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `companies`
--

LOCK TABLES `companies` WRITE;
/*!40000 ALTER TABLE `companies` DISABLE KEYS */;
INSERT INTO `companies` VALUES ('22e11de8-2ab5-42d5-b93f-a724cf038598','Familjeterapeuterna Syd AB','ChIJ4WAYH4y9U0YRXe06sXvxMGo','https://www.google.com/maps/place/Familjeterapeuterna+Syd+AB/@57.701789,12.745419,739562m/data=!3m1!1e3!4m7!3m6!1s0x4653bd8c1f1860e1:0x6a30f17bb13aed5d!8m2!3d55.6071144!4d12.9992093!15sChNmYW1pbGpldGVyYXBldXRlcm5hkgEPcHN5Y2hvdGhlcmFwaXN04AEA!16s%2Fg%2F1yh9twyvg?entry=tts&g_ep=EgoyMDI1MDgyNC4wIPu8ASoASAFQAw%3D%3D&skid=67953520-49ff-4610-807c-b26b39dfa606',1,'2025-09-10 19:07:09.679522');
/*!40000 ALTER TABLE `companies` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `reviews`
--

DROP TABLE IF EXISTS `reviews`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `reviews` (
  `Id` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `CompanyId` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `AuthorName` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Rating` int NOT NULL,
  `Text` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `Time` datetime(6) NOT NULL,
  `AuthorUrl` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `ProfilePhotoUrl` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  PRIMARY KEY (`Id`),
  KEY `IX_Reviews_CompanyId` (`CompanyId`),
  KEY `IX_Reviews_Time` (`Time`),
  CONSTRAINT `FK_Reviews_Companies_CompanyId` FOREIGN KEY (`CompanyId`) REFERENCES `companies` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `reviews`
--

LOCK TABLES `reviews` WRITE;
/*!40000 ALTER TABLE `reviews` DISABLE KEYS */;
INSERT INTO `reviews` VALUES ('22e11de8-2ab5-42d5-b93f-a724cf038598_places/ChIJ4WAYH4y9U0YRXe06sXvxMGo/reviews/ChdDSUhNMG9nS0VJQ0FnSURUNVBxc29nRRAB','22e11de8-2ab5-42d5-b93f-a724cf038598','Pia Karlsson',5,'I have received a lot of help there, and if I hadn\'t come there, I wouldn\'t have felt as well as I do now. I don\'t regret getting that contact. ??','2024-05-22 21:18:10.940611','https://www.google.com/maps/contrib/113392451499095694108/reviews','https://lh3.googleusercontent.com/a/ACg8ocIjjKAHmK5Fp0eQIDMz0cg4yttjUMCaVA_InvTJuPZ9IxNAhQ=s128-c0x00000000-cc-rp-mo'),('22e11de8-2ab5-42d5-b93f-a724cf038598_places/ChIJ4WAYH4y9U0YRXe06sXvxMGo/reviews/ChdDSUhNMG9nS0VJQ0FnSURYa18taHdRRRAB','22e11de8-2ab5-42d5-b93f-a724cf038598','First Class PT Malmö',5,'Serious and reliable operator with competent staff. Wide range of services that can be used by both \"ordinary\" people as well as managers and leaders. Highly recommended by our staff in Malmö.','2024-10-30 22:34:59.891217','https://www.google.com/maps/contrib/101009391882717207445/reviews','https://lh3.googleusercontent.com/a-/ALV-UjU-vfiWTUOvpm4VHiijtfmwTyNsvcKXNTBG0FLaJfLiFxPpImw_=s128-c0x00000000-cc-rp-mo-ba3'),('22e11de8-2ab5-42d5-b93f-a724cf038598_places/ChIJ4WAYH4y9U0YRXe06sXvxMGo/reviews/ChZDSUhNMG9nS0VJQ0FnSUNncXBTYUp3EAE','22e11de8-2ab5-42d5-b93f-a724cf038598','Elisabeth Skoog',1,'Charged for time when meeting was 20 minutes late','2025-04-13 02:10:00.952418','https://www.google.com/maps/contrib/109393659859451177737/reviews','https://lh3.googleusercontent.com/a/ACg8ocLZOKOaeLsn3M7atxiREXMc-8-K9gM_1j81Ikn55FxSIxUBDGg=s128-c0x00000000-cc-rp-mo-ba3'),('22e11de8-2ab5-42d5-b93f-a724cf038598_places/ChIJ4WAYH4y9U0YRXe06sXvxMGo/reviews/ChZDSUhNMG9nS0VJQ0FnSUNYMTZxNlRREAE','22e11de8-2ab5-42d5-b93f-a724cf038598','Andreas B',1,'Impossible to contact, the psychologists do not respond to emails and avoid their patients','2024-10-21 22:50:38.101399','https://www.google.com/maps/contrib/103618369536827089535/reviews','https://lh3.googleusercontent.com/a/ACg8ocK43iSRQItSf43QFXCSk5UR2CE9yJZs-zjWcN4TRFrdIDVu1Q=s128-c0x00000000-cc-rp-mo'),('22e11de8-2ab5-42d5-b93f-a724cf038598_places/ChIJ4WAYH4y9U0YRXe06sXvxMGo/reviews/Ci9DQUlRQUNvZENodHljRjlvT2sxcFNFVkVjMmc0YUV4T1prbExWMTh0YkhSNFkwRRAB','22e11de8-2ab5-42d5-b93f-a724cf038598','Shadowglove',5,'They helped me a lot and gave me all the tools I needed to help myself. I recommend them to others too!','2025-08-05 10:57:29.826443','https://www.google.com/maps/contrib/112788044427857652754/reviews','https://lh3.googleusercontent.com/a-/ALV-UjUevpIK3bk41FcGI_WOaOCbdX1x3UoqwnlKyxZJLOeW4wMy-FbP=s128-c0x00000000-cc-rp-mo');
/*!40000 ALTER TABLE `reviews` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `scheduledmonitorcompanies`
--

DROP TABLE IF EXISTS `scheduledmonitorcompanies`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `scheduledmonitorcompanies` (
  `Id` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `ScheduledReviewMonitorId` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `CompanyId` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_ScheduledMonitorCompanies_CompanyId` (`CompanyId`),
  KEY `IX_ScheduledMonitorCompanies_ScheduledReviewMonitorId` (`ScheduledReviewMonitorId`),
  CONSTRAINT `FK_ScheduledMonitorCompanies_Companies_CompanyId` FOREIGN KEY (`CompanyId`) REFERENCES `companies` (`Id`) ON DELETE CASCADE,
  CONSTRAINT `FK_ScheduledMonitorCompanies_ScheduledReviewMonitors_ScheduledR~` FOREIGN KEY (`ScheduledReviewMonitorId`) REFERENCES `scheduledreviewmonitors` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `scheduledmonitorcompanies`
--

LOCK TABLES `scheduledmonitorcompanies` WRITE;
/*!40000 ALTER TABLE `scheduledmonitorcompanies` DISABLE KEYS */;
/*!40000 ALTER TABLE `scheduledmonitorcompanies` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `scheduledmonitorexecutions`
--

DROP TABLE IF EXISTS `scheduledmonitorexecutions`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `scheduledmonitorexecutions` (
  `Id` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `ScheduledReviewMonitorId` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `ExecutedAt` datetime(6) NOT NULL,
  `PeriodStart` datetime(6) NOT NULL,
  `PeriodEnd` datetime(6) NOT NULL,
  `CompaniesChecked` int NOT NULL,
  `CompaniesWithIssues` int NOT NULL,
  `TotalBadReviews` int NOT NULL,
  `EmailSent` tinyint(1) NOT NULL,
  `EmailError` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `Status` int NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_ScheduledMonitorExecutions_ExecutedAt` (`ExecutedAt`),
  KEY `IX_ScheduledMonitorExecutions_ScheduledReviewMonitorId` (`ScheduledReviewMonitorId`),
  CONSTRAINT `FK_ScheduledMonitorExecutions_ScheduledReviewMonitors_Scheduled~` FOREIGN KEY (`ScheduledReviewMonitorId`) REFERENCES `scheduledreviewmonitors` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `scheduledmonitorexecutions`
--

LOCK TABLES `scheduledmonitorexecutions` WRITE;
/*!40000 ALTER TABLE `scheduledmonitorexecutions` DISABLE KEYS */;
/*!40000 ALTER TABLE `scheduledmonitorexecutions` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `scheduledreviewmonitors`
--

DROP TABLE IF EXISTS `scheduledreviewmonitors`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `scheduledreviewmonitors` (
  `Id` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Name` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Description` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `EmailAddress` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `ScheduleType` int NOT NULL,
  `ScheduleTime` time(6) NOT NULL,
  `DayOfWeek` int DEFAULT NULL,
  `DayOfMonth` int DEFAULT NULL,
  `MaxRating` int NOT NULL,
  `ReviewPeriodDays` int NOT NULL,
  `IsActive` tinyint(1) NOT NULL,
  `IncludeAllCompanies` tinyint(1) NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `LastRunAt` datetime(6) NOT NULL,
  `NextRunAt` datetime(6) NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_ScheduledReviewMonitors_IsActive` (`IsActive`),
  KEY `IX_ScheduledReviewMonitors_NextRunAt` (`NextRunAt`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `scheduledreviewmonitors`
--

LOCK TABLES `scheduledreviewmonitors` WRITE;
/*!40000 ALTER TABLE `scheduledreviewmonitors` DISABLE KEYS */;
/*!40000 ALTER TABLE `scheduledreviewmonitors` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Dumping routines for database 'aspnet_google_reviews'
--
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2025-09-11 13:11:47


COMMIT;