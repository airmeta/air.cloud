-- Converts existing PostgreSQL timestamp with time zone columns in the public schema
-- to timestamp without time zone for the Air.Cloud demo database.
-- Run this once against an existing database if EF or an earlier schema created timestamptz columns.

SET TIME ZONE 'Asia/Shanghai';

DO $$
DECLARE
  column_item record;
BEGIN
  FOR column_item IN
    SELECT table_schema, table_name, column_name
    FROM information_schema.columns
    WHERE table_schema = 'public'
      AND data_type = 'timestamp with time zone'
    ORDER BY table_name, ordinal_position
  LOOP
    EXECUTE format(
      'ALTER TABLE %I.%I ALTER COLUMN %I TYPE timestamp without time zone USING %I::timestamp without time zone',
      column_item.table_schema,
      column_item.table_name,
      column_item.column_name,
      column_item.column_name
    );
  END LOOP;
END $$;
