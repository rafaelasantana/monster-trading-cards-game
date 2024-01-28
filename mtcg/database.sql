-- Create users table
CREATE TABLE users (
    id SERIAL PRIMARY KEY,
    username VARCHAR(255) NOT NULL,
    password VARCHAR(255) NOT NULL,
    coins INT CHECK (coins >= 0),
    createdAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- create packages table
CREATE TABLE packages (
    id SERIAL PRIMARY KEY,
    price INT,
    ownerId INT REFERENCES Users(Id),
    CONSTRAINT NonNegative_Price CHECK (Price >= 0),
    CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
ALTER TABLE packages
ALTER COLUMN ownerId DROP NOT NULL;
-- Updated cards table with elementType and cardType as VARCHAR
CREATE TABLE cards (
    id VARCHAR(255) PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    damage DOUBLE PRECISION NOT NULL,
    elementType VARCHAR(255),
    cardType VARCHAR(255),
    packageId INT REFERENCES packages(id),
    ownerId INT REFERENCES users(id),
    createdAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

ALTER TABLE cards
ALTER COLUMN ownerId DROP NOT NULL;

CREATE TABLE transactions (
    id SERIAL PRIMARY KEY,
    userId INT NOT NULL REFERENCES users(id),
    packageId INT NOT NULL REFERENCES packages(id),
    transactionDate TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    price INT CHECK (price >= 0)
);

CREATE TABLE deckCards (
id SERIAL PRIMARY KEY,
cardId VARCHAR(255) REFERENCES cards(id),
ownerId INT REFERENCES users(id)
);

CREATE TABLE userProfiles (
userId INT PRIMARY KEY REFERENCES users(id),
name VARCHAR(255),
bio TEXT,
image TEXT,
updatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
CREATE TABLE userStats (
userId INT PRIMARY KEY REFERENCES users(id),
eloRating INT DEFAULT 100 CHECK (eloRating >= 0),
wins INT DEFAULT 0,
losses INT DEFAULT 0,
totalGamesPlayed INT DEFAULT 0,
lastActivity TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);


CREATE OR REPLACE FUNCTION update_last_activity()
RETURNS TRIGGER AS $$
BEGIN
    NEW.lastActivity = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER user_stats_last_activity
BEFORE UPDATE ON userStats
FOR EACH ROW
EXECUTE FUNCTION update_last_activity();

CREATE OR REPLACE VIEW scoreboard AS
SELECT
 u.username,
    us.eloRating,
    us.wins,
    us.losses
FROM userStats us
JOIN users u ON us.userId = u.id;

CREATE TABLE tradings (
id VARCHAR(255) PRIMARY KEY,
ownerId INT NOT NULL REFERENCES users(id),
cardId VARCHAR(255) NOT NULL REFERENCES cards(id),
requestedType VARCHAR(255), -- 'spell' or ',onster'
minDamage INT, -- minimum damage of card to trade with
status VARCHAR(255) DEFAULT 'open', -- 'open', 'closed', 'accepted'
createdAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
updatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
CONSTRAINT chk_requested_type CHECK (requestedType IN ('spell', 'monster')),
CONSTRAINT chk_status CHECK (status IN ('open', 'closed', 'accepted'))
);
-- Function to update the updatedAt column
CREATE OR REPLACE FUNCTION update_updatedAt_column() RETURNS TRIGGER AS $$ BEGIN NEW.updatedAt = CURRENT_TIMESTAMP;     RETURN NEW; END; $$ LANGUAGE plpgsql;

-- Trigger to use the function
CREATE TRIGGER update_tradings_updatedAt BEFORE UPDATE ON tradings FOR EACH ROW EXECUTE FUNCTION update_updatedAt_column();
-- Create Battles table
CREATE TABLE battles (
    id SERIAL PRIMARY KEY,
    player1Id INT NOT NULL REFERENCES users(id),
    player2Id INT REFERENCES users(id),
    status VARCHAR(50) NOT NULL CHECK (status IN ('pending', 'ongoing', 'completed')),
    startTime TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    endTime TIMESTAMP,
    winnerId INT REFERENCES users(id)
);

-- Create BattleLog table
CREATE TABLE battleLogs (
    id SERIAL PRIMARY KEY,
    battleId INT NOT NULL REFERENCES battles(id),
    roundNumber INT NOT NULL,
    player1CardId VARCHAR(255) REFERENCES cards(id),
    player2CardId VARCHAR(255) REFERENCES cards(id),
    roundResult VARCHAR(255),
    createdAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE OR REPLACE FUNCTION clear_all_tables()
RETURNS void AS $$
BEGIN
    -- Disable triggers for foreign key constraint checks
    SET CONSTRAINTS ALL DEFERRED;

    -- Delete data from tables. Order matters due to foreign key constraints.
    DELETE FROM battleLogs;
    DELETE FROM battles;
    DELETE FROM tradings;
    DELETE FROM transactions;
    DELETE FROM deckCards;
    DELETE FROM cards;
    DELETE FROM packages;
    DELETE FROM userStats;
    DELETE FROM userProfiles;
    DELETE FROM users;

    -- Re-enable triggers for foreign key constraint checks
    SET CONSTRAINTS ALL IMMEDIATE;
END;
$$ LANGUAGE plpgsql;

SELECT clear_all_tables();CREATE OR REPLACE FUNCTION clear_all_tables()
RETURNS void AS $$
BEGIN
    -- Disable triggers for foreign key constraint checks
    SET CONSTRAINTS ALL DEFERRED;

    -- Delete data from tables. Order matters due to foreign key constraints.
    DELETE FROM battleLogs;
    DELETE FROM battles;
    DELETE FROM tradings;
    DELETE FROM transactions;
    DELETE FROM deckCards;
    DELETE FROM cards;
    DELETE FROM packages;
    DELETE FROM userStats;
    DELETE FROM userProfiles;
    DELETE FROM users;

    -- Re-enable triggers for foreign key constraint checks
    SET CONSTRAINTS ALL IMMEDIATE;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION delete_all_tables()
RETURNS void AS $$
BEGIN
 -- Disable triggers for foreign key constraint checks
    SET CONSTRAINTS ALL DEFERRED;

    -- Delete data from tables. Order matters due to foreign key constraints.
    DROP TABLE battleLogs CASCADE;
    DROP TABLE battles CASCADE;
    DROP TABLE tradings CASCADE;
    DROP TABLE transactions CASCADE;
    DROP TABLE deckCards CASCADE;
    DROP TABLE cards CASCADE;
    DROP TABLE packages CASCADE;
    DROP TABLE userStats CASCADE;
    DROP TABLE userProfiles CASCADE;
    DROP TABLE users CASCADE;

END;
$$ LANGUAGE plpgsql;
