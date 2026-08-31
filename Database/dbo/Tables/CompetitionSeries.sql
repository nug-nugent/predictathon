CREATE TABLE [dbo].[CompetitionSeries] (
    [CompetitionSeriesID] UNIQUEIDENTIFIER NOT NULL DEFAULT (newid()),
    [SeriesName]          VARCHAR (50)     NOT NULL,
    [ShortName]           VARCHAR (10)     NOT NULL,
    [BadgeIcon]           VARCHAR (30)     NULL,
    [BadgeColour]         VARCHAR (7)      NULL,
    [DisplayOrder]        INT              CONSTRAINT [DF_CompetitionSeries_DisplayOrder] DEFAULT ((0)) NOT NULL,
    CONSTRAINT [PK_CompetitionSeries] PRIMARY KEY CLUSTERED ([CompetitionSeriesID] ASC)
);
