CREATE DATABASE SOPAL_GPI;
USE SOPAL_GPI;
-- ===================================================================
-- SCRIPT DE CR�ATION DE LA BASE DE DONN�ES - SOPAL GPI
-- ===================================================================

-- 1. Cr�ation de la table des �tats Mat�riels (Etatmat)
CREATE TABLE [Etatmat] (
    [Id] INT IDENTITY(1,1) NOT NULL,
    [Libelle] NVARCHAR(10) NOT NULL,
    CONSTRAINT [PK_Etatmat] PRIMARY KEY ([Id])
);

-- 2. Cr�ation de la table des Types de Mat�riels (Matype)
CREATE TABLE [Matype] (
    [Id] INT IDENTITY(1,1) NOT NULL,
    [Libelle] NVARCHAR(MAX) NOT NULL,
    CONSTRAINT [PK_Matype] PRIMARY KEY ([Id])
);

-- 3. Cr�ation de la table des Localisations (Localistation)
CREATE TABLE [Localistation] (
    [Id] INT IDENTITY(1,1) NOT NULL,
    [Nom] NVARCHAR(MAX) NOT NULL,
    CONSTRAINT [PK_Localistation] PRIMARY KEY ([Id])
);

-- 4. Cr�ation de la table des Employ�s (Employe)
CREATE TABLE [Employe] (
    [Matricule] INT NOT NULL, -- DatabaseGeneratedOption.None (Pas d'identit� automatique)
    [Nom] NVARCHAR(MAX) NOT NULL,
    [Prenom] NVARCHAR(MAX) NOT NULL,
    [Email] NVARCHAR(MAX) NOT NULL,
    [Service] NVARCHAR(MAX) NULL,
    [Poste] NVARCHAR(MAX) NOT NULL,
    Actif Bit,
    Role nvarchar(7) check (Role = 'Admin' Or Role = 'User'),
    CONSTRAINT [PK_Employe] PRIMARY KEY ([Matricule])
);

-- 5. Cr�ation de la table des Mat�riels (Material)
CREATE TABLE [Material] (
    [Id] INT IDENTITY(1,1) NOT NULL,
    [QrCode] NVARCHAR(MAX) NULL,
    [Libelle] NVARCHAR(100) NOT NULL,
    [Marque] NVARCHAR(50) NOT NULL,
    [Modele] NVARCHAR(50) NOT NULL,
    [NumSerie] NVARCHAR(100) NOT NULL,
    [DateAcquisition] DATE NOT NULL,
    [Actif] BIT NOT NULL,
    [commentaires] NVARCHAR(MAX) NOT NULL,
    [DateModif] DATETIME NOT NULL DEFAULT (GETDATE()),
    [EtatId] INT NOT NULL DEFAULT 1,
    [TypeMaterielId] INT NOT NULL,
    [LocalisationId] INT NOT NULL DEFAULT 6,
    [EmployeMat] INT NULL,
    CONSTRAINT [PK_Material] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Material_Etatmat_EtatId] FOREIGN KEY ([EtatId]) REFERENCES [Etatmat] ([Id]),
    CONSTRAINT [FK_Material_Matype_TypeMaterielId] FOREIGN KEY ([TypeMaterielId]) REFERENCES [Matype] ([Id]),
    CONSTRAINT [FK_Material_Localistation_LocalisationId] FOREIGN KEY ([LocalisationId]) REFERENCES [Localistation] ([Id]),
    CONSTRAINT [FK_Material_Employe_EmployeMat] FOREIGN KEY ([EmployeMat]) REFERENCES [Employe] ([Matricule]) ON DELETE SET NULL
);

-- Index unique requis par l'attribut [Index(nameof(NumSerie), IsUnique = true)]
CREATE UNIQUE INDEX [IX_Material_NumSerie] ON [Material] ([NumSerie]);

-- 6. Cr�ation de la table des Mouvements (Mouvement)
CREATE TABLE [Mouvement] (
    [Id] INT IDENTITY(1,1) NOT NULL,
    [DateMouvement] DATETIME NOT NULL DEFAULT (GETDATE()),
    [typmov] INT NOT NULL, -- Repr�sente l'enum Typmov (0 = Affectation, 1 = Retours, 2 = Transferts)
    [materielId] INT NOT NULL,
    [LocalisationId] INT NOT NULL,
    [AncienEtatmatId] INT NOT NULL,
    [NouvEtatmatId] INT NOT NULL,
    [EmployeMat] INT NULL,
    [commentaires] NVARCHAR(MAX) NULL,
    Statut nvarchar(30) NULL check (Statut='En cours' or Statut='Termin�' or Statut='Pas encore intervenu')
    CONSTRAINT [PK_Mouvement] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Mouvement_Material_materielId] FOREIGN KEY ([materielId]) REFERENCES [Material] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Mouvement_Localistation_LocalisationId] FOREIGN KEY ([LocalisationId]) REFERENCES [Localistation] ([Id]),
    CONSTRAINT [FK_Mouvement_Etatmat_NouvEtatmatId] FOREIGN KEY ([NouvEtatmatId]) REFERENCES [Etatmat] ([Id]),
    CONSTRAINT [FK_Mouvement_Employe_EmployeMat] FOREIGN KEY ([EmployeMat]) REFERENCES [Employe] ([Matricule]),
    CONSTRAINT FK_Mouvement_Typmov_TypmovId FOREIGN KEY ([Typmov]) REFERENCES TypMov([Id])
);

-- Add IntervenantMat column (tracks which admin claimed the intervention)
ALTER TABLE [Mouvement] ADD [IntervenantMat] INT NULL;
ALTER TABLE [Mouvement] ADD CONSTRAINT [FK_Mouvement_Employe_IntervenantMat] FOREIGN KEY ([IntervenantMat]) REFERENCES [Employe] ([Matricule]);

Create Table TypMov(
[ID] int IDENTITY(1,1) NOT NULL primary key,
[Nom] nvarchar(12) NOT NULL
);

-- New Tables : Interventions (ID, Tech_guy#, MatId#,Date, Commentaires)
CREATE TABLE Interventions (
    [ID] INT IDENTITY(1,1) NOT NULL primary key,
    [Commentaires] NVARCHAR(200) NULL,
    [Date] DATE NOT NULL,
    [MatId] INT NOT NULL,
    [IntervenantId] INT NULL,
    [MaintId] INT NOT NULL,
    CONSTRAINT [FK_Interventions_Material] FOREIGN KEY ([MatId]) REFERENCES [dbo].[Material]([Id]),
    CONSTRAINT [FK_Interventions_Employe] FOREIGN KEY ([IntervenantId]) REFERENCES [Employe]([Matricule]),
    CONSTRAINT [FK_Interventions_Mouvement] FOREIGN KEY ([MaintId]) REFERENCES [Mouvement]([Id])
);

--FOREIGN KEY ([Typmov]) REFERENCES [Typmov]([Id]);

--- TypMov
INSERT INTO [Typmov] ([Nom]) VALUES ('Affectation');
INSERT INTO [Typmov] ([Nom]) VALUES ('Retour');
INSERT INTO [Typmov] ([Nom]) VALUES ('Transfert');
INSERT INTO [Typmov] ([Nom]) VALUES ('Maintenance');

DBCC CHECKIDENT ('Etatmat', RESEED, 0);

-- Etatmat
INSERT INTO [Etatmat] ([Libelle]) VALUES 
(N'En Stock'),         
(N'Affect�'),          
(N'En Maint.'),  
(N'Hors Serv.'),     
(N'Rebut');        

-- Matype 
INSERT INTO [Matype] ([Libelle]) VALUES 
(N'Ordinateur Portable'),
(N'�cran'),
(N'Imprimante'),
(N'Serveur'),
(N'Routeur/Switch');

-- Localisation
INSERT INTO [Localistation] ([Nom]) VALUES 
(N'Magasin principal'), 
(N'Bureau DSI'),        
(N'Service RH'),        
(N'Atelier Technique'), 
(N'Salle Serveur'),     
(N'Stock');

-- Mat�riel
INSERT INTO [Material] 
([QrCode], [Libelle], [Marque], [Modele], [NumSerie], [DateAcquisition], [Actif], [commentaires], [EtatId], [TypeMaterielId], [LocalisationId], [EmployeMat]) 
VALUES 
('', N'PC Portable Latitude 5420', 'Dell', 'Latitude 5420', 'SN-DELL-5420-XYZ', '2025-01-15', 1, N'PC performant pour dev', 2, 1, 2, 26001),

('', N'�cran UltraSharp 24', 'Dell', 'U2422H', 'SN-DELL-U24-ABC', '2025-02-10', 1, N'�cran de rechange', 1, 2, 1, NULL),

('', N'Imprimante LaserJet Pro', 'HP', 'M404dn', 'SN-HP-M404-123', '2024-11-01', 1, N'Probl�me de rouleau d''entra�nement', 3, 3, 4, NULL);

-- 1. Insertion d'un technicien actif (r�le 'Admin')
INSERT INTO [dbo].[Employe] 
    ([Matricule], [Nom], [Prenom], [Email], [Service], [Poste], [Actif], [Role])
VALUES 
    (26000, N'Haddad', N'Yassine', N'yassine.haddad@sopal.com', N'Informatique', N'Technicien Support', 1, N'Admin');

-- 2. Insertion d'un utilisateur standard actif
INSERT INTO [dbo].[Employe] 
    ([Matricule], [Nom], [Prenom], [Email], [Service], [Poste], [Actif], [Role])
VALUES 
    (26001, N'Mezghani', N'Amira', N'amira.mezghani@sopal.com', N'Finances', N'Comptable', 1, N'User');
GO