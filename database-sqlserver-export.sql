-- SQL Server Database Export
-- Generated on: 2025-09-12 18:30:10
-- Database: aspnet-google_reviews
-- Source Server: (localdb)\mssqllocaldb
--
-- Instructions:
-- 1. Create a new database on your hosting server
-- 2. Run this script against the new database
-- 3. Update your connection string
--

USE [aspnet-google_reviews];
GO

-- Export all data from all tables
-- Data for table: dbo.AspNetRoles

-- Raw data for dbo.AspNetRoles (you may need to convert this to INSERT statements)
054a4725-b6a5-4ad9-8c13-41848788fe61,User,USER,NULL
c00148ad-1452-47d0-8b0d-cecbce5df982,Admin,ADMIN,NULL

(2 rows affected)

-- Data for table: dbo.AspNetUsers

-- Raw data for dbo.AspNetUsers (you may need to convert this to INSERT statements)
00088137-3d25-4beb-b56b-847b4dc780ec,admin@example.com,ADMIN@EXAMPLE.COM,admin@example.com,ADMIN@EXAMPLE.COM,1,AQAAAAIAAYagAAAAENx+pd6nCN1Gv4lZGqDQ3OzUEgqaYrKCpGkBUWjx1dNTR5aTqeemT3lfl5S6nq3BrQ==,RHTBBKZXBN6LZLZQJ3OPECWBKH3EVR3Q,038e3151-fc86-4b6c-9472-660808f81e8a,NULL,0,0,NULL,1,0

(1 rows affected)

-- Data for table: dbo.AspNetRoleClaims

-- Raw data for dbo.AspNetRoleClaims (you may need to convert this to INSERT statements)

(0 rows affected)

-- Data for table: dbo.AspNetUserClaims

-- Raw data for dbo.AspNetUserClaims (you may need to convert this to INSERT statements)

(0 rows affected)

-- Data for table: dbo.AspNetUserLogins

-- Raw data for dbo.AspNetUserLogins (you may need to convert this to INSERT statements)

(0 rows affected)

-- Data for table: dbo.AspNetUserRoles

-- Raw data for dbo.AspNetUserRoles (you may need to convert this to INSERT statements)
00088137-3d25-4beb-b56b-847b4dc780ec,c00148ad-1452-47d0-8b0d-cecbce5df982

(1 rows affected)

-- Data for table: dbo.AspNetUserTokens

-- Raw data for dbo.AspNetUserTokens (you may need to convert this to INSERT statements)

(0 rows affected)

-- Data for table: dbo.Companies

-- Raw data for dbo.Companies (you may need to convert this to INSERT statements)
410d4cd6-0201-4dc1-8171-af7ef400287d,Krögers,ChIJZfgXlGTRV0YRUIVdUWnXc1M,NULL,1,2025-08-29 12:25:43.6916154
916fdb13-78ea-4b63-a6d4-79b8797fa05f,Familjeterapeuterna Syd AB,ChIJ4WAYH4y9U0YRXe06sXvxMGo,https://www.google.com/maps/place/Familjeterapeuterna+Syd+AB/@57.701789,12.745419,739562m/data=!3m1!1e3!4m7!3m6!1s0x4653bd8c1f1860e1:0x6a30f17bb13aed5d!8m2!3d55.6071144!4d12.9992093!15sChNmYW1pbGpldGVyYXBldXRlcm5hkgEPcHN5Y2hvdGhlcmFwaXN04AEA!16s%2Fg%2F1yh9,1,2025-09-05 14:02:27.2266918
9fa9ceb4-f6f4-4db0-807e-3645421f1e99,Familjeterapeuterna Syd Södermalm Sthlm,0x465f9dc1dcaa0959,https://www.google.com/maps/place/Familjeterapeuterna+Syd+S%C3%B6dermalm+Stockholm/@57.701789,12.745419,887082m/data=!3m1!1e3!4m6!3m5!1s0x465f9dc1dcaa0959:0x4ea0cba4f202b6b0!8m2!3d59.3101675!4d18.0816643!16s%2Fg%2F11h_q_fnl4?entry=ttu&g_ep=EgoyMDI1MDgyNS4w,1,2025-09-05 13:40:20.8282934
bc386aab-736f-4376-a3d7-6efef0cc196d,Searchminds,ChIJnytgooPRV0YRTCdMAyVMLt4,NULL,1,2025-08-29 08:25:28.4751350
bf4590c1-9d25-4c22-b1be-62d119be16a7,Familjeterapeuterna syd uppsala,NULL,https://www.google.com/maps/place/Familjeterapeuterna+Syd+Uppsala/@57.701789,12.745419,887082m/data=!3m1!1e3!4m6!3m5!1s0x465fcbf4645ebf1b:0x6e46d4af10277fbe!8m2!3d59.8544159!4d17.6521015!16s%2Fg%2F11sghl559j?entry=ttu&g_ep=EgoyMDI1MDgyNS4wIKXMDSoASAFQAw%3D,1,2025-09-05 13:45:58.1484705
d278a99f-8369-445f-805b-cd3ba5e2c637,familjeterapeuterna syd lund,ChIJKdrs-aKXU0YRPgCk6_o75X0,https://www.google.com/maps/place/Familjeterapeuterna+Syd+Lund/@57.701789,12.745419,887082m/data=!3m1!1e3!4m6!3m5!1s0x465397a2f9ecda29:0x7de53bfaeba4003e!8m2!3d55.7014365!4d13.2002702!16s%2Fg%2F11pdpvp7h4?entry=ttu&g_ep=EgoyMDI1MDgyNS4wIKXMDSoASAFQAw%3D%3D,1,2025-09-05 14:04:29.2806771

(6 rows affected)

-- Data for table: dbo.Reviews

-- Raw data for dbo.Reviews (you may need to convert this to INSERT statements)
410d4cd6-0201-4dc1-8171-af7ef400287d_places/ChIJZfgXlGTRV0YRUIVdUWnXc1M/reviews/ChdDSUhNMG9nS0VJQ0FnSUNhemRpLV9BRRAB,410d4cd6-0201-4dc1-8171-af7ef400287d,Rosemarie Geiss,5,Being a vegetarian I ordered their falafel with salad, I was on my way back home to Malmö and needed something quick and nourishing to eat and was amazed by their take of the Malmö falafel: fresh, great red cabbage salad and condiments: quick and friendly ,2021-08-06 15:03:58.1014060,https://www.google.com/maps/contrib/105170387927306934739/reviews,https://lh3.googleusercontent.com/a-/ALV-UjWsMQvPXp33hqJaB-K5Tl6-KCyHXykuoGUyRdKwceOjXuajZYE=s128-c0x00000000-cc-rp-mo-ba3
410d4cd6-0201-4dc1-8171-af7ef400287d_places/ChIJZfgXlGTRV0YRUIVdUWnXc1M/reviews/ChdDSUhNMG9nS0VJQ0FnSURBeF8zOHd3RRAB,410d4cd6-0201-4dc1-8171-af7ef400287d,Micha Gijsbers,4,Ate lunch here during beginning of Juli with a lot of tourists around. As custom in Sweden a special lunch deal was available. I chose to have the salad with ranch dressing, avocado, roast beef, eggs and more for 99SEK (?10,-), water and coffee with cookie,2017-07-12 15:20:34.1100000,https://www.google.com/maps/contrib/106442563734153256052/reviews,https://lh3.googleusercontent.com/a-/ALV-UjVeAPvgQd0DX3UPjaLRNV5WC1oETrDGsxpLi5-NHOmCGGV36qpL=s128-c0x00000000-cc-rp-mo-ba5
410d4cd6-0201-4dc1-8171-af7ef400287d_places/ChIJZfgXlGTRV0YRUIVdUWnXc1M/reviews/ChdDSUhNMG9nS0VJQ0FnSURMel9HR19nRRAB,410d4cd6-0201-4dc1-8171-af7ef400287d,Ancharin vidpattum,2,I've ordered the most expensive dish on the menu it cost 365 kr. But it has Bearnaise sauce on top! why not come separately instead, Destroy the taste completely. Otherwise it's a nice place to hangout with friends which in the centre of Kalmar.,2024-07-02 13:22:31.1759470,https://www.google.com/maps/contrib/101549440605593628902/reviews,https://lh3.googleusercontent.com/a/ACg8ocKy5khbPVU-rVXU-YFsYpix0_3EbSttzBhqaC1aJU1qxtx96g=s128-c0x00000000-cc-rp-mo
410d4cd6-0201-4dc1-8171-af7ef400287d_places/ChIJZfgXlGTRV0YRUIVdUWnXc1M/reviews/ChdDSUhNMG9nS0VJQ0FnSURwOXRHTTV3RRAB,410d4cd6-0201-4dc1-8171-af7ef400287d,Simon Upton (Simon G Upton),2,Casual restaurant with lots of seating options and good range of drinks at the bar and good ambience outside in particular. Service was friendly but too casual, but biggest letdown was the food. Starter was ok, but my sirloin steak was poor, a tough low qu,2023-08-21 22:37:31.9487650,https://www.google.com/maps/contrib/102740405836040961590/reviews,https://lh3.googleusercontent.com/a-/ALV-UjUkTpL4pEKSjjfxl9Z7Z10aIxQGm8ymUJtTRocABXIJ2JlXOz8K7Q=s128-c0x00000000-cc-rp-mo-ba6
410d4cd6-0201-4dc1-8171-af7ef400287d_places/ChIJZfgXlGTRV0YRUIVdUWnXc1M/reviews/ChdDSUhNMG9nS0VJQ0FnTURJdWNuX2lRRRAB,410d4cd6-0201-4dc1-8171-af7ef400287d,Alex Dela,5,Great burgers, service and music! We sat outside in the sun. Would recommend 100%! Thank you.,2025-04-11 13:40:59.5563810,https://www.google.com/maps/contrib/103090126005043310538/reviews,https://lh3.googleusercontent.com/a-/ALV-UjXCsaLXOlmgifLj8gxurU314ri0ftO43lw2XNllkf_xCxjYBkcR=s128-c0x00000000-cc-rp-mo-ba5
916fdb13-78ea-4b63-a6d4-79b8797fa05f_places/ChIJ4WAYH4y9U0YRXe06sXvxMGo/reviews/ChdDSUhNMG9nS0VJQ0FnSURUNVBxc29nRRAB,916fdb13-78ea-4b63-a6d4-79b8797fa05f,Pia Karlsson,5,I have received a lot of help there, and if I hadn't come there, I wouldn't have felt as well as I do now. I don't regret getting that contact. ????,2024-05-22 21:18:10.9406110,https://www.google.com/maps/contrib/113392451499095694108/reviews,https://lh3.googleusercontent.com/a/ACg8ocIjjKAHmK5Fp0eQIDMz0cg4yttjUMCaVA_InvTJuPZ9IxNAhQ=s128-c0x00000000-cc-rp-mo
916fdb13-78ea-4b63-a6d4-79b8797fa05f_places/ChIJ4WAYH4y9U0YRXe06sXvxMGo/reviews/ChdDSUhNMG9nS0VJQ0FnSURYa18taHdRRRAB,916fdb13-78ea-4b63-a6d4-79b8797fa05f,First Class PT Malmö,5,Serious and reliable operator with competent staff. Wide range of services that can be used by both "ordinary" people as well as managers and leaders. Highly recommended by our staff in Malmö.,2024-10-30 22:34:59.8912170,https://www.google.com/maps/contrib/101009391882717207445/reviews,https://lh3.googleusercontent.com/a-/ALV-UjU-vfiWTUOvpm4VHiijtfmwTyNsvcKXNTBG0FLaJfLiFxPpImw_=s128-c0x00000000-cc-rp-mo-ba3
916fdb13-78ea-4b63-a6d4-79b8797fa05f_places/ChIJ4WAYH4y9U0YRXe06sXvxMGo/reviews/ChZDSUhNMG9nS0VJQ0FnSUNncXBTYUp3EAE,916fdb13-78ea-4b63-a6d4-79b8797fa05f,Elisabeth Skoog,1,Charged for time when meeting was 20 minutes late,2025-04-13 02:10:00.9524180,https://www.google.com/maps/contrib/109393659859451177737/reviews,https://lh3.googleusercontent.com/a/ACg8ocLZOKOaeLsn3M7atxiREXMc-8-K9gM_1j81Ikn55FxSIxUBDGg=s128-c0x00000000-cc-rp-mo-ba3
916fdb13-78ea-4b63-a6d4-79b8797fa05f_places/ChIJ4WAYH4y9U0YRXe06sXvxMGo/reviews/ChZDSUhNMG9nS0VJQ0FnSUNYMTZxNlRREAE,916fdb13-78ea-4b63-a6d4-79b8797fa05f,Andreas B,1,Impossible to contact, the psychologists do not respond to emails and avoid their patients,2024-10-21 22:50:38.1013990,https://www.google.com/maps/contrib/103618369536827089535/reviews,https://lh3.googleusercontent.com/a/ACg8ocK43iSRQItSf43QFXCSk5UR2CE9yJZs-zjWcN4TRFrdIDVu1Q=s128-c0x00000000-cc-rp-mo
916fdb13-78ea-4b63-a6d4-79b8797fa05f_places/ChIJ4WAYH4y9U0YRXe06sXvxMGo/reviews/Ci9DQUlRQUNvZENodHljRjlvT2sxcFNFVkVjMmc0YUV4T1prbExWMTh0YkhSNFkwRRAB,916fdb13-78ea-4b63-a6d4-79b8797fa05f,Shadowglove,5,They helped me a lot and gave me all the tools I needed to help myself. I recommend them to others too!,2025-08-05 10:57:29.8264432,https://www.google.com/maps/contrib/112788044427857652754/reviews,https://lh3.googleusercontent.com/a-/ALV-UjUevpIK3bk41FcGI_WOaOCbdX1x3UoqwnlKyxZJLOeW4wMy-FbP=s128-c0x00000000-cc-rp-mo
bc386aab-736f-4376-a3d7-6efef0cc196d_places/ChIJnytgooPRV0YRTCdMAyVMLt4/reviews/ChdDSUhNMG9nS0VJQ0FnSUNqd2ZLVHNnRRAB,bc386aab-736f-4376-a3d7-6efef0cc196d,Henrik Sorensen (Henrik),5,Searchminds has increased the number of visitors to our store beyond what we had expected. In addition, they have significantly improved our position in Google's dynamic search field. We have also experienced a very lively dialogue with Searchmind, which c,2024-04-22 14:34:51.6622720,https://www.google.com/maps/contrib/102071748862996156296/reviews,https://lh3.googleusercontent.com/a/ACg8ocLOZvJhfuggMeg-149Z1TqQEiRgGVszJMPG3cKkcNMEr0r0Iw=s128-c0x00000000-cc-rp-mo
bc386aab-736f-4376-a3d7-6efef0cc196d_places/ChIJnytgooPRV0YRTCdMAyVMLt4/reviews/ChdDSUhNMG9nS0VJQ0FnSUR4dHFtemt3RRAB,bc386aab-736f-4376-a3d7-6efef0cc196d,Gino Hashim,5,There are many agencies that make a lot of empty promises, but few that offer quality and service like Searchminds!

This is coming from someone who has worked with SEO and created 3 websites and generally has a good foundation in marketing but when I need,2023-06-08 10:01:20.2501350,https://www.google.com/maps/contrib/106844195267455908852/reviews,https://lh3.googleusercontent.com/a/ACg8ocJQivImsj_jivGxvv3oKwTuMpC9K-WGvED11it_rhf0VTusog=s128-c0x00000000-cc-rp-mo
bc386aab-736f-4376-a3d7-6efef0cc196d_places/ChIJnytgooPRV0YRTCdMAyVMLt4/reviews/ChZDSUhNMG9nS0VJQ0FnSUMtcnRMWGZREAE,bc386aab-736f-4376-a3d7-6efef0cc196d,Simon Lardner,5,I ended up as a customer of Searchminds without really knowing anything about the company, and with the reservation that I don't have much to compare it to, I want to give the team the highest marks for professional service, nice contacts, and as far as I ,2022-11-04 12:02:28.1989070,https://www.google.com/maps/contrib/106816501219996817766/reviews,https://lh3.googleusercontent.com/a/ACg8ocI2yB3Ct90RWBL-iLqy0qvygoMEc9PlPvGL3q0jCTZfsmLHIA=s128-c0x00000000-cc-rp-mo-ba3
bc386aab-736f-4376-a3d7-6efef0cc196d_places/ChIJnytgooPRV0YRTCdMAyVMLt4/reviews/ChZDSUhNMG9nS0VJQ0FnSUNyNi1QQ1lnEAE,bc386aab-736f-4376-a3d7-6efef0cc196d,Petter Jacobsson,5,Searchminds helps me get top results in search engines. The price of this is good, but the best part is the service. Above all, I have to highlight Frida, my contact person at Searchminds. She always takes her time with my stupid questions and is always su,2024-07-11 11:27:06.4308710,https://www.google.com/maps/contrib/108767907561973590339/reviews,https://lh3.googleusercontent.com/a-/ALV-UjUmQuZu3gk8N_UdNmFnPu92ccLQYbfoPer3iYj7yDPCoqzVABcx=s128-c0x00000000-cc-rp-mo
bc386aab-736f-4376-a3d7-6efef0cc196d_places/ChIJnytgooPRV0YRTCdMAyVMLt4/reviews/Ci9DQUlRQUNvZENodHljRjlvT25sbGFtWnBaRzlhVmxWS1pFUnFRMmwxYlZSVE5IYxAB,bc386aab-736f-4376-a3d7-6efef0cc196d,Annelie Hellman,5,I have been working with Searchminds for several years and have seen a steady increase in both website traffic and visibility in search results during that time. Their work with keyword optimization and launches has really made a difference. It is reassuri,2025-07-02 09:50:04.7889345,https://www.google.com/maps/contrib/110924784434043031782/reviews,https://lh3.googleusercontent.com/a/ACg8ocI2hqL0i-0tUCu4F34hBLTDJSzZJKGIz2EbQqFpWqB2qT9qzA=s128-c0x00000000-cc-rp-mo
d278a99f-8369-445f-805b-cd3ba5e2c637_places/ChIJKdrs-aKXU0YRPgCk6_o75X0/reviews/ChdDSUhNMG9nS0VJQ0FnSUNOa0piMmlBRRAB,d278a99f-8369-445f-805b-cd3ba5e2c637,Jacob Eriksson,1,Was in line, was supposed to get an appointment. Staff quit and a lot of time lost. I would like to give 0 stars. Lousy.,2024-01-11 11:07:06.8613970,https://www.google.com/maps/contrib/113281539038239442983/reviews,https://lh3.googleusercontent.com/a/ACg8ocLZc3lYNuTv0NCKBOrOIKb44Te58prAHahgmVxVwuX8nXG_VA=s128-c0x00000000-cc-rp-mo-ba2
d278a99f-8369-445f-805b-cd3ba5e2c637_places/ChIJKdrs-aKXU0YRPgCk6_o75X0/reviews/ChdDSUhNMG9nS0VJQ0FnSURtaVotSGxBRRAB,d278a99f-8369-445f-805b-cd3ba5e2c637,Michal Golun XIII,1,If you book the wrong time so you don't have time for the call, she's happy and can send double bills with a reminder. NOTE: I DO NOT SPECIFICALLY RECOMMEND ISABELLE, you should feel good after the call, not worse than it looks now!!!
WARNING EVERYONE!!!!,2022-02-01 14:24:04.8865170,https://www.google.com/maps/contrib/115296595330597036677/reviews,https://lh3.googleusercontent.com/a-/ALV-UjVjeDPR2cctIfWcCBSMetWieS68rKVnM6_Dq8enQa04FdnLYtOT=s128-c0x00000000-cc-rp-mo-ba3

(17 rows affected)

-- Data for table: dbo.ScheduledReviewMonitors

-- Raw data for dbo.ScheduledReviewMonitors (you may need to convert this to INSERT statements)
99981b3e-8ec0-4f37-ac21-6289392f8681,Varje minut,NULL,milleeagle@gmail.com,0,21:30:00.0000000,NULL,NULL,3,7,1,1,2025-09-05 19:29:34.4675509,2025-09-10 05:00:56.9359443,2025-09-10 21:30:00.0000000

(1 rows affected)

-- Data for table: dbo.ScheduledMonitorCompanies

-- Raw data for dbo.ScheduledMonitorCompanies (you may need to convert this to INSERT statements)

(0 rows affected)

-- Data for table: dbo.ScheduledMonitorExecutions

-- Raw data for dbo.ScheduledMonitorExecutions (you may need to convert this to INSERT statements)
020ba64b-9b6b-4ca9-87d3-99e41b59b46f,99981b3e-8ec0-4f37-ac21-6289392f8681,2025-09-08 15:34:00.0789717,2025-09-01 15:34:00.0789719,2025-09-08 15:34:00.0789718,5,0,0,0,Failed to send email report,2
a68f2f33-38c8-45af-9ef1-63d81d1e1f0a,99981b3e-8ec0-4f37-ac21-6289392f8681,2025-09-07 06:17:48.4505783,2025-08-31 06:17:48.4506792,2025-09-07 06:17:48.4506325,5,0,0,0,Failed to send email report,2
ba906b4b-e441-40f6-bdd1-fed4c1e45640,99981b3e-8ec0-4f37-ac21-6289392f8681,2025-09-06 06:30:31.0823298,2025-08-30 06:30:31.0824582,2025-09-06 06:30:31.0823916,5,0,0,1,NULL,0
fa1bba42-b1d7-4a99-aa40-7dbc6ce1b49e,99981b3e-8ec0-4f37-ac21-6289392f8681,2025-09-10 05:00:54.4174400,2025-09-03 05:00:54.4175112,2025-09-10 05:00:54.4174788,5,0,0,1,NULL,0

(4 rows affected)

-- Data for table: (12 rows affected)

-- Raw data for (12 rows affected) (you may need to convert this to INSERT statements)
Msg 102, Level 15, State 1, Server DESKTOP-CUL6J8G\LOCALDB#9AB5119E, Line 1
Incorrect syntax near '12'.

