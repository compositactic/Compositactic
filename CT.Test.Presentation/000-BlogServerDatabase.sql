﻿--alter database BlogDb set single_user with rollback immediate
--drop database BlogDb

IF (db_id(N'BlogDb') IS NULL)
	CREATE DATABASE "BlogDb"