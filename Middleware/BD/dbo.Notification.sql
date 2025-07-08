CREATE TABLE [dbo].[Notification] (
    [Id]               INT                IDENTITY (1, 1) NOT NULL,
    [Name]             VARCHAR (50)       NOT NULL,
    [CreationDateTime] DATETIME2 (7) DEFAULT (sysdatetime()) NOT NULL,
    [Parent]           INT                NOT NULL,
    [Event]            INT                NOT NULL,
    [Endpoint]         VARCHAR (255)      NOT NULL,
    [Enabled]          BIT                NOT NULL,
    PRIMARY KEY CLUSTERED ([Id] ASC),
    UNIQUE NONCLUSTERED ([Name] ASC),
    CONSTRAINT [FK_Notification_Container] FOREIGN KEY ([Parent]) REFERENCES [dbo].[Container] ([Id])
);

