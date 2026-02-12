#!/bin/bash
# combine-secret.sh - Combine Shamir shares to reconstruct the secret
#
# Usage: ./combine-secret.sh [shares-file]
# If no file is provided, defaults to shares.json in the scripts directory

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_DIR="$SCRIPT_DIR/../src/ShamirSecretSharing.Console"
SHARES_FILE="${1:-$SCRIPT_DIR/shares.json}"

if [ ! -f "$SHARES_FILE" ]; then
    echo "Error: Shares file not found: $SHARES_FILE"
    echo "Run split-secret.sh first to generate shares."
    exit 1
fi

echo "=== Combining Shares ==="
echo "Reading from: $SHARES_FILE"
echo ""

echo "Reconstructed secret:"
dotnet run --project "$PROJECT_DIR" -- combine \
    --shares-file "$SHARES_FILE" \
    --as-text
