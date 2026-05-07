#!/usr/bin/env bash
# =====================================================================
# Restore do banco SQLite + uploads
#
# Uso:
#   ./restore-sqlite.sh ./backups/agendamento-20260507-153000.tar.gz
#
# ⚠️ PARE A API ANTES (docker compose stop api) — overwrite garantido só com app parado.
# =====================================================================
set -euo pipefail

if [[ $# -ne 1 ]]; then
    echo "Uso: $0 <caminho-do-backup.tar.gz>"
    exit 1
fi

BACKUP_FILE="$1"
DATA_DIR="${DATA_DIR:-/var/lib/docker/volumes/agendamento_api-data/_data}"

if [[ ! -f "$BACKUP_FILE" ]]; then
    echo "[ERRO] Arquivo $BACKUP_FILE não encontrado" >&2
    exit 1
fi

# Backup defensivo do estado atual antes de sobrescrever
SAFETY=$(mktemp -d -t agp-restore-safe-XXXXXX)
echo "[INFO] Backup defensivo do estado atual em $SAFETY"
cp -a "$DATA_DIR" "$SAFETY/" || true

read -r -p "Restaurar de $BACKUP_FILE para $DATA_DIR? Tudo lá será sobrescrito. [s/N] " confirm
[[ "$confirm" =~ ^[sS]$ ]] || { echo "Abortado."; exit 0; }

# Extrai por cima
tar -xzf "$BACKUP_FILE" -C "$DATA_DIR"

echo "[OK] Restore concluído."
echo "[INFO] Estado anterior preservado em $SAFETY (apague depois de validar)."
echo "[INFO] Suba a API novamente: docker compose start api"
