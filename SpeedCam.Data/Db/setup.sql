CREATE TABLE [dbo].[Config](
	[StartTime] [int] NOT NULL,
	[StartTimeSunrise] [int] NOT NULL,
	[EndTime] [int] NOT NULL,
	[EndTimeSunset] [int] NOT NULL,
	[LeftDistance] [decimal](5, 2) NOT NULL,
	[RightDistance] [decimal](5, 2) NOT NULL,
	[LeftStart] [int] NOT NULL,
	[RightStart] [int] NOT NULL,
	[Latitude] [decimal](18, 7) NULL,
	[Longitude] [decimal](18, 7) NULL,
	[ExportFolder] [varchar](256) NULL,
	[VideoAddress] [varchar](256) NULL,
	[VideoUser] [varchar](128) NULL,
	[VideoPassword] [varchar](128) NULL,
	[VideoChannel] [int] NULL,
	[ConvertedFolder] [varchar](256) NULL,
	[AnalyzedFolder] [varchar](256) NULL,
	[ConvertedErrorFolder] [varchar](256) NULL,
	[PhotoFolder] [varchar](256) NULL,
	[ChunkTime] [int] NULL
) ON [PRIMARY]
GO


CREATE TABLE [dbo].[DateChunk](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[StartDate] [datetime] NOT NULL,
	[ProcessingTime] [int] NOT NULL,
	[LengthMinutes] [int] NOT NULL,
	[ExportDone] [bit] NOT NULL,
	[DateProcessed] [datetime] NULL,
 CONSTRAINT [PK_DateChunk] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO


CREATE TABLE [dbo].[Entry](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[DateAdded] [datetime] NOT NULL,
	[Direction] [varchar](1) NOT NULL,
	[Speed] [decimal](18, 4) NOT NULL,
 CONSTRAINT [PK_Entry] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO


CREATE TABLE [dbo].[Log](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[DateAdded] [datetime] NOT NULL,
	[Message] [nvarchar](max) NOT NULL,
	[StackTrace] [nvarchar](max) NOT NULL,
 CONSTRAINT [PK_Log] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO


CREATE TABLE [dbo].[MakeUp](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[StartDate] [datetime] NOT NULL,
	[LengthMinutes] [int] NOT NULL,
	[InProgress] [bit] NOT NULL,
 CONSTRAINT [PK_MakeUp] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO


