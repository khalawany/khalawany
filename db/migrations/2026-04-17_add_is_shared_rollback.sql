-- SQLite rollback for IsShared column migration
-- SQLite doesn't support DROP COLUMN directly in older versions,
-- so this rollback recreates the table without IsShared.

BEGIN TRANSACTION;

CREATE TABLE MediaClips_backup (
    Id INTEGER NOT NULL CONSTRAINT PK_MediaClips PRIMARY KEY AUTOINCREMENT,
    Title TEXT NOT NULL,
    Description TEXT NULL,
    MediaType TEXT NOT NULL,
    FilePath TEXT NOT NULL,
    OwnerId TEXT NOT NULL,
    CreatedAtUtc TEXT NOT NULL
);

INSERT INTO MediaClips_backup (Id, Title, Description, MediaType, FilePath, OwnerId, CreatedAtUtc)
SELECT Id, Title, Description, MediaType, FilePath, OwnerId, CreatedAtUtc
FROM MediaClips;

DROP TABLE MediaClips;
ALTER TABLE MediaClips_backup RENAME TO MediaClips;

COMMIT;
