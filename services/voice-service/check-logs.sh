#!/bin/bash
# Voice Service Docker Log Viewer

echo "=== Voice Service Container Logs (Last 100 lines) ==="
docker logs bisoyle-voice-service-1 --tail 100

echo ""
echo "=== Following logs (Ctrl+C to stop) ==="
echo "Run: docker logs bisoyle-voice-service-1 -f"
echo ""
echo "=== Check saved files in container ==="
echo "Run: docker exec bisoyle-voice-service-1 ls -lah /app/models/speakers/"
echo ""
echo "=== Enter container shell ==="
echo "Run: docker exec -it bisoyle-voice-service-1 /bin/bash"

