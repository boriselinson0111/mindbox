CREATE TABLE [dbo].[Client](
	[Id] [int]  NOT NULL,
	[Name] [nvarchar](256) NOT NULL,
	[CreationDateUtc] [datetime] NOT NULL,
 CONSTRAINT [PK_Insured] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
))

CREATE TABLE [dbo].[Order](
	[Id] [int] NOT NULL,
	[ClientId] [int] NOT NULL,
	[OrderPlacedDateUtc] [datetime] NOT NULL,
 CONSTRAINT [PK_Orders] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
))

ALTER TABLE [dbo].[Order]  WITH CHECK ADD  CONSTRAINT [FK_Order_Client] FOREIGN KEY([ClientId])
REFERENCES [dbo].[Client] ([Id])

INSERT INTO [dbo].[Client] VALUES (1, 'Tom', '2020-05-24 22:24:01'),
								  (2, 'Bill', '2020-05-21 22:21:01')


INSERT INTO [dbo].[Order] VALUES (1,1, '2020-05-26 22:25:02'),
								 (2,1, '2020-05-29 22:25:02'),
								 (3,1, '2020-05-30 22:25:02'),
								 (4,2, '2020-06-01 22:24:02')

SELECT COUNT(DISTINCT cl.Id) as NumberOfClients FROM [dbo].[Client] cl JOIN [dbo].[Order] Ord
		 ON cl.Id = Ord.ClientId
		 WHERE DATEDIFF(DAY, cl.CreationDateUtc, Ord.OrderPlacedDateUtc) <= 5
		 
		 
		 http://sqlfiddle.com/#!18/bba9a7/1