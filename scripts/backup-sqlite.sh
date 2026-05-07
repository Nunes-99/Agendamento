#!/usr/bin/env bash
# =====================================================================
# Backup do banco SQLite + uploads do AgendamentoPro
#
# Uso:
#   ./backup-sqlite.sh                 # backup com timestamp em ./backups
#   BACKUP_DIR=/mnt/external ./backup-sqlite.sh
#
# Cron sugerido (a cada 6h):
#   0 */6 * * * /opt/agendamentopro/scripts/backup-sqlite.sh >> /var/log/agp-backup.log 2>&1
#
# Estratégia:
# - Usa `sqlite3 .backup` (online backup API) — não precisa parar a API.
# - Compacta com tar.gz (DB + uploads).
# - Mantém retenção de N dias (default: 14).
# =====================================================================
set -euo pipefail

# Configuração (sobrescrevível via env)
DATA_DIR="${DATA_DIR:-/var/lib/docker/volumes/agendamento_api-data/_data}"
DB_PATH="${DB_PATH:-$DATA_DIR/agendamento.db}"
UPLOADS_PATH="${UPLOADS_PATH:-$DATA_DIR/uploads}"
BACKUP_DIR="${BACKUP_DIR:-./backups}"
RETENTION_DAYS="${RETENTION_DAYS:-14}"

TIMESTAMP=$(date +%Y%m%d-%H%M%S)
TMP_DIR=$(mktemp -d -t agp-backup-XXXXXX)
trap 'rm -rf "$TMP_DIR"' EXIT

mkdir -p "$BACKUP_DIR"

# 1) Verifica DB
if [[ ! -f "$DB_PATH" ]]; then
    echo "[ERRO] DB não encontrado em $DB_PATH" >&2
    exit 1
fi

# 2) Backup online do SQLite (consistente mesmo com API rodando)
if command -v sqlite3 >/dev/null 2>&1; then
    sqlite3 "$DB_PATH" ".backup '$TMP_DIR/agendamento.db'"
else
    # Fallback: cópia simples (pode pegar momento inconsistente em escrita pesada)
    echo "[WARN] sqlite3 não instalado, usando cp (menos seguro). Instale: apt install sqlite3" >&2
    cp "$DB_PATH" "$TMP_DIR/agendamento.db"
    [[ -f "${DB_PATH}-wal" ]] && cp "${DB_PATH}-wal" "$TMP_DIR/" || true
    [[ -f "${DB_PATH}-shm" ]] && cp "${DB_PATH}-shm" "$TMP_DIR/" || true
fi

# 3) Copia uploads se existir
if [[ -d "$UPLOADS_PATH" ]]; then
    cp -r "$UPLOADS_PATH" "$TMP_DIR/uploads"
else
    echo "[INFO] Diretório uploads não encontrado em $UPLOADS_PATH (ok se nada foi enviado ainda)"
fi

# 4) Compacta
ARCHIVE="$BACKUP_DIR/agendamento-$TIMESTAMP.tar.gz"
tar -czf "$ARCHIVE" -C "$TMP_DIR" .
SIZE=$(du -h "$ARCHIVE" | cut -f1)
echo "[OK] Backup criado: $ARCHIVE ($SIZE)"

# 5) Retenção: remove backups antigos
find "$BACKUP_DIR" -name "agendamento-*.tar.gz" -mtime +"$RETENTION_DAYS" -delete
REMAINING=$(find "$BACKUP_DIR" -name "agendamento-*.tar.gz" | wc -l)
echo "[OK] Backups retidos: $REMAINING (retenção $RETENTION_DAYS dias)"
