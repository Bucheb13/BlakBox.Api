-- Atualizacao para bancos Supabase ja existentes.
-- Faça backup e confirme o schema antes de executar.
BEGIN;

ALTER TABLE "CredencialInstalacao"
    ADD COLUMN IF NOT EXISTS "ChaveRecuperacaoProtegida" text;

ALTER TABLE "CredencialInstalacao"
    ADD COLUMN IF NOT EXISTS "RecuperacaoExpiraEm" timestamp without time zone;

COMMIT;
