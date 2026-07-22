#!/usr/bin/env bash

set -u

PRINTER_IP="${1:-192.168.1.150}"
PRINT_FILE="${2:-out_file.txt}"
PRINTER_PORT="${3:-9100}"

if ! command -v nc >/dev/null 2>&1; then
    echo "Erreur : la commande 'nc' (netcat) n'est pas installée." >&2
    echo "Debian/Ubuntu : sudo apt install netcat-openbsd" >&2
    echo "Alpine        : apk add netcat-openbsd" >&2
    exit 1
fi

if [ ! -f "$PRINT_FILE" ]; then
    echo "Erreur : fichier introuvable : $PRINT_FILE" >&2
    exit 2
fi

echo "Test de $PRINTER_IP:$PRINTER_PORT..."

if ! nc -z -w 3 "$PRINTER_IP" "$PRINTER_PORT"; then
    echo "Erreur : le port $PRINTER_IP:$PRINTER_PORT est inaccessible." >&2
    exit 3
fi

echo "Port accessible. Envoi de $PRINT_FILE..."

if ! nc -q 1 "$PRINTER_IP" "$PRINTER_PORT" < "$PRINT_FILE"; then
    echo "Erreur pendant l'envoi de la commande d'impression." >&2
    exit 4
fi

echo "Commande transmise à $PRINTER_IP:$PRINTER_PORT."
