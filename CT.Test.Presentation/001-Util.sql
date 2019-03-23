
CREATE OR ALTER PROCEDURE dbo.CreateTable 
    @tableName NVARCHAR(MAX),
	@parentTableName NVARCHAR(MAX) = '',
	@baseTableName NVARCHAR(MAX) = ''
AS
	DECLARE @sql NVARCHAR(MAX)

	SET @sql = 'IF NOT EXISTS (SELECT * FROM sys.tables WHERE Name = ''' + @tableName + ''')
		CREATE TABLE "' + @tableName + '" ( [Id] INT NOT NULL PRIMARY KEY IDENTITY '
	IF @parentTableName = ''
		SET @sql = @sql + ')'
	ELSE
		SET @sql = @sql + ', [' + @parentTableName + 'Id] INT NOT NULL,
			CONSTRAINT [FK_' + @tableName + '_To_' + @parentTableName + '] FOREIGN KEY ([' + @parentTableName + 'Id]) REFERENCES ['  + @parentTableName + ']([Id]))'
	PRINT @sql
	EXEC sp_executesql @sql

GO

CREATE OR ALTER PROCEDURE dbo.DropTable 
    @tableName NVARCHAR(MAX) 
AS
	DECLARE @sql NVARCHAR(MAX)

	SET @sql = 'IF EXISTS (SELECT * FROM sys.tables WHERE Name = ''' + @tableName + ''')
		DROP TABLE "' + @tableName + '"' 

	PRINT @sql	
	EXEC sp_executesql @sql

GO

CREATE OR ALTER PROCEDURE dbo.CreateOrModifyColumn
	@tableName NVARCHAR(MAX),
	@columnName NVARCHAR(MAX),
	@columnType NVARCHAR(MAX)
AS
	DECLARE @sql NVARCHAR(MAX)

	SET @sql = 'IF COL_LENGTH(''' + @tableName + ''',''' + @columnName + ''') IS NULL
					ALTER TABLE "' + @tableName + '" ADD ' + @columnName + ' ' + @columnType + 
				' ELSE 
					ALTER TABLE "' + @tableName + '" ALTER COLUMN "' + @columnName + '" ' + @columnType 
	PRINT @sql
	EXEC sp_executesql @sql

GO

CREATE OR ALTER PROCEDURE dbo.DropColumn
	@tableName NVARCHAR(MAX),
	@columnName NVARCHAR(MAX)
AS
	DECLARE @sql NVARCHAR(MAX)

	SET @sql = 'IF COL_LENGTH(''' + @tableName + ''',''' + @columnName + ''') IS NOT NULL
					ALTER TABLE "' + @tableName + '" DROP COLUMN ' + @columnName  
	PRINT @sql
	EXEC sp_executesql @sql


GO

CREATE OR ALTER PROCEDURE dbo.CreateIndex
	@tableName NVARCHAR(MAX),
	@columnName NVARCHAR(MAX)
AS
	DECLARE @sql NVARCHAR(MAX)
	DECLARE @indexName NVARCHAR(MAX)

	SET @indexName = 'IDX_' + @tableName + @columnName

	SET @sql = 'IF NOT EXISTS(SELECT * FROM sys.indexes WHERE name = ''' + @indexName + ''' AND object_id = OBJECT_ID(''' + @tableName + '''))
		BEGIN
			CREATE INDEX ' + @indexName + ' ON ' + @tableName + ' (' + @columnName + ')
		END'

	PRINT @sql
	EXEC sp_executesql @sql
		
GO

CREATE OR ALTER PROCEDURE dbo.DropIndex
	@tableName NVARCHAR(MAX),
	@columnName NVARCHAR(MAX)
AS
	DECLARE @sql NVARCHAR(MAX)
	DECLARE @indexName NVARCHAR(MAX)

	SET @indexName = 'IDX_' + @tableName + @columnName

	SET @sql = 'IF EXISTS(SELECT * FROM sys.indexes WHERE name = ''' + @indexName + ''' AND object_id = OBJECT_ID(''' + @tableName + '''))
		BEGIN
			DROP INDEX ' + @indexName + ' ON ' + @tableName + '
		END'

	PRINT @sql
	EXEC sp_executesql @sql
		
GO
