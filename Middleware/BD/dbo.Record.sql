CREATE TABLE [dbo].[Record] (
    [Id]               INT          IDENTITY (1, 1) NOT NULL,
    [Name]             VARCHAR (50) NOT NULL,
    [Content]          TEXT         NOT NULL,
    [CreationDateTime] DATETIME2 (7) DEFAULT (sysdatetime()) NOT NULL,
    [Parent]           INT          NOT NULL,
    PRIMARY KEY CLUSTERED ([Id] ASC),
	 UNIQUE NONCLUSTERED ([Name] ASC),
    CONSTRAINT [FK_Record_Container] FOREIGN KEY ([Parent]) REFERENCES [dbo].[Container] ([Id])
);

