CREATE TABLE [dbo].[ErrorLog] (
    [Id]              INT            IDENTITY (1, 1) NOT NULL,
    [Message]         NVARCHAR (MAX) NULL,
    [MessageTemplate] NVARCHAR (MAX) NULL,
    [Level]           NVARCHAR (128) NULL,
    [TimeStampUtc]    DATETIME2 (7)  NOT NULL,
    [Exception]       NVARCHAR (MAX) NULL,
    [Properties]      NVARCHAR (MAX) NULL,
    CONSTRAINT [PK_ErrorLog] PRIMARY KEY CLUSTERED ([Id] ASC)
);


GO
-- Written by the Serilog MSSqlServer sink (Warning and above only); read newest-first by the
-- admin Error Log page, which is the only query pattern against this table.
CREATE NONCLUSTERED INDEX [IX_ErrorLog_TimeStampUtc]
    ON [dbo].[ErrorLog]([TimeStampUtc] DESC);
