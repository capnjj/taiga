BEGIN;

ALTER TABLE land ADD COLUMN OtherCleanTime INTEGER NOT NULL default 0;
ALTER TABLE land ADD COLUMN Dwell INTEGER NOT NULL default 0;

COMMIT;
