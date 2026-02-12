#!/bin/bash
# split-secret.sh - Split a secret into Shamir shares
#
# Usage: ./split-secret.sh [secret]
# If no secret is provided, defaults to "hello world"

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_DIR="$SCRIPT_DIR/../src/ShamirSecretSharing.Console"
SHARES_FILE="$SCRIPT_DIR/shares.json"

SECRET="${1:-hello world}"
SHARES=3
THRESHOLD=2

echo "=== Splitting Secret ==="
echo "Secret: $SECRET"
echo "Shares: $SHARES"
echo "Threshold: $THRESHOLD"
echo ""

dotnet run --project "$PROJECT_DIR" -- split \
    --shares "$SHARES" \
    --threshold "$THRESHOLD" \
    --secret-text "$SECRET" \
    --out "$SHARES_FILE"

echo ""
echo "Shares written to: $SHARES_FILE"
echo ""
echo "Contents:"
cat "$SHARES_FILE"
