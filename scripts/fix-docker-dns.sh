#!/usr/bin/env bash
# Fixes Docker daemon DNS timeouts (mcr.microsoft.com / docker.io lookups via 127.0.0.53).
set -euo pipefail
sudo tee /etc/docker/daemon.json >/dev/null <<'EOF'
{
  "dns": ["8.8.8.8", "1.1.1.1"]
}
EOF
sudo systemctl restart docker
echo "Docker DNS updated. Test with: docker pull mcr.microsoft.com/dotnet/sdk:8.0"
