# MTCG

Welcome to my Monster Trading Cards Game! This document provides instructions on how to set up and run the project. Please follow the steps below to get started.

## Setting Up the Database

Before running the project, you need to create and configure the database. Here's how to do it using Docker and PostgreSQL:

1. **Create a Docker Volume**:

```bash
docker volume create mtcg-postgres-data
```
2. **Run a PostgreSQL Container**:
```bash
docker run --name mtcg-postgres -e POSTGRES_USER=mtcguser -e POSTGRES_PASSWORD=mtcgpassword -e POSTGRES_DB=mtcgdb -p 5433:5432 -v mtcg-postgres-data:/var/lib/postgresql/data -d postgres
```
3. **Start the PostgreSQL Container:**
```bash
docker start mtcg-postgres
```
4. **Access PostgreSQL**:

Run the following command to connect to the database:

```bash
psql -h localhost -p 5433 -U mtcguser -d mtcgdb
```

5. **Execute the SQL Script**:

In the psql terminal, you can execute the `database.sql` script to create the necessary tables and schema. Use the following command to run the script:

```bash
\i path/to/database.sql
```

## Running the Project

1. **Navigate to MTCG folder**:
```bash
cd MTCG
```
2. **Build and run the project**:
```bash
dotnet build
dotnet run
```

## Project protocol

For detailed information about the project's protocol, please refer to the `MTCG-Protocol.md` file attached to this repository.
