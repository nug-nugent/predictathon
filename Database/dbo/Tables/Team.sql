CREATE TABLE [dbo].[Team] (
    [TeamID]         UNIQUEIDENTIFIER NOT NULL,
    [TeamName]       VARCHAR (50)     NOT NULL,
    [ShortName]      VARCHAR (20)     NOT NULL,
    [ImageName]      VARCHAR (50)     NULL,
    [ExternalApiCode] VARCHAR (10)    NULL,
    CONSTRAINT [PK_Team] PRIMARY KEY CLUSTERED ([TeamID] ASC)
);

