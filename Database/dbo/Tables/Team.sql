CREATE TABLE [dbo].[Team] (
    [TeamID]    UNIQUEIDENTIFIER NOT NULL,
    [TeamName]  VARCHAR (50)     COLLATE Latin1_General_CI_AI NOT NULL,
    [ShortName] VARCHAR (20)     COLLATE Latin1_General_CI_AI NOT NULL,
    [ImageName] VARCHAR (50)     COLLATE Latin1_General_CI_AI NULL,
    CONSTRAINT [PK_Team] PRIMARY KEY CLUSTERED ([TeamID] ASC)
);

