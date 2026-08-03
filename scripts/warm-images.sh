#!/usr/bin/env bash
set -euo pipefail

API="${1:-https://api.supershop.pt}"
CLOUD="${VITE_CLOUDINARY_CLOUD_NAME:-ylxkr66i}"
WIDTHS=(400 600 800 1200)

echo "==> Reading the catalogue from $API"

ids=$(curl -fsS "$API/api/products?pageSize=48" \
  | grep -o '"publicId":"[^"]*"' \
  | cut -d'"' -f4 \
  | sort -u)

total=$(echo "$ids" | wc -l | tr -d ' ')
echo "==> $total images, ${#WIDTHS[@]} widths each"

slow=0
for id in $ids; do
  for w in "${WIDTHS[@]}"; do
    url="https://res.cloudinary.com/$CLOUD/image/upload/f_auto,q_auto,w_$w,c_fill,ar_1:1/$id"
    ms=$(curl -fsS -o /dev/null -w '%{time_total}' "$url" | awk '{printf "%d", $1 * 1000}')
    if [ "$ms" -gt 250 ]; then
      echo "    generated  ${id}  w=${w}  ${ms}ms"
      slow=$((slow + 1))
    fi
  done
done

echo
echo "Done. $slow of $((total * ${#WIDTHS[@]})) had to be generated; the rest were already cached."
