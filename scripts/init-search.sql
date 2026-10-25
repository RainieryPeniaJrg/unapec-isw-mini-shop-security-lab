-- Disposable SELECT-only principal for the intentional F1-10 search surface.
CREATE ROLE minishop_search LOGIN PASSWORD 'local-search-only';
ALTER ROLE minishop_search SET default_transaction_read_only = on;
ALTER ROLE minishop_search SET statement_timeout = '10s';
GRANT CONNECT ON DATABASE minishop TO minishop_search;
GRANT USAGE ON SCHEMA public TO minishop_search;
REVOKE CREATE ON SCHEMA public FROM PUBLIC;
