-- KhalawanyTube SQLite migration
-- Date: 2026-04-17
-- Purpose: add per-clip privacy control (IsShared)
--
-- Usage:
--   sqlite3 familytube.db ".read db/migrations/2026-04-17_add_is_shared.sql"

BEGIN TRANSACTION;

-- Add visibility column: 0 = private, 1 = shared
ALTER TABLE MediaClips
ADD COLUMN IsShared INTEGER NOT NULL DEFAULT 0;

-- Optional: If you want all existing clips shared by default, uncomment below.
-- UPDATE MediaClips SET IsShared = 1;

COMMIT;
