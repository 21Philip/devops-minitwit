#!/bin/bash
# Deploy ITU-MiniTwit Application and Simulator API
# Usage: ./deploy.sh <droplet_ip> <release_tag>

set -e

# Color output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Check arguments
if [ $# -lt 1 ]; then
    echo -e "${RED}Usage: $0 <droplet_ip> [release_tag]${NC}"
    echo "Example: $0 209.38.198.112 v1.0.0"
    exit 1
fi

DROPLET_IP=$1
RELEASE_TAG=${2:-"latest"}
SSH_KEY=${SSH_KEY:-"~/.ssh/id_rsa"}

echo -e "${YELLOW}========================================${NC}"
echo -e "${YELLOW}  ITU-MiniTwit Deployment Script${NC}"
echo -e "${YELLOW}========================================${NC}"
echo "Droplet IP: $DROPLET_IP"
echo "Release Tag: $RELEASE_TAG"
echo "SSH Key: $SSH_KEY"
echo ""

# Verify we can connect
echo -e "${YELLOW}[1/6] Verifying SSH connection...${NC}"
if ! ssh -i "$SSH_KEY" root@"$DROPLET_IP" "echo 'SSH connection OK'" > /dev/null 2>&1; then
    echo -e "${RED}Failed to connect to $DROPLET_IP via SSH${NC}"
    exit 1
fi
echo -e "${GREEN}✓ SSH connection verified${NC}"

# Stop running container
echo -e "${YELLOW}[2/6] Stopping existing container...${NC}"
ssh -i "$SSH_KEY" root@"$DROPLET_IP" "sudo docker stop itu-minitwit || true"
ssh -i "$SSH_KEY" root@"$DROPLET_IP" "sudo docker rm itu-minitwit || true"
echo -e "${GREEN}✓ Container stopped${NC}"

# Copy latest application code
echo -e "${YELLOW}[3/6] Syncing application code...${NC}"
rsync -avz -e "ssh -i $SSH_KEY" \
    --exclude='.git' \
    --exclude='.vagrant' \
    --exclude='bin' \
    --exclude='obj' \
    --exclude='.env' \
    ./ root@"$DROPLET_IP":/opt/itu-minitwit/
echo -e "${GREEN}✓ Code synced${NC}"

# Build Docker image
echo -e "${YELLOW}[4/6] Building Docker image (Release: $RELEASE_TAG)...${NC}"
ssh -i "$SSH_KEY" root@"$DROPLET_IP" "cd /opt/itu-minitwit && sudo docker build -t itu-minitwit:$RELEASE_TAG ."
echo -e "${GREEN}✓ Docker image built${NC}"

# Start container
echo -e "${YELLOW}[5/6] Starting application container...${NC}"
ssh -i "$SSH_KEY" root@"$DROPLET_IP" "sudo docker run -d \
    --name itu-minitwit \
    --restart unless-stopped \
    -p 8080:8080 \
    -e ASPNETCORE_URLS='http://+:8080' \
    itu-minitwit:$RELEASE_TAG"
echo -e "${GREEN}✓ Container started${NC}"

# Verify deployment
echo -e "${YELLOW}[6/6] Verifying deployment...${NC}"
sleep 3
if curl -s -I http://"$DROPLET_IP":8080 | grep -q "200\|301\|302\|404"; then
    echo -e "${GREEN}✓ Application is responding${NC}"
else
    echo -e "${RED}⚠ Application may not be responding properly${NC}"
    echo "Check logs with: ssh -i $SSH_KEY root@$DROPLET_IP 'sudo docker logs itu-minitwit'"
fi

echo ""
echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}  Deployment Complete!${NC}"
echo -e "${GREEN}========================================${NC}"
echo "Application URL: http://$DROPLET_IP:8080"
echo "Simulator API: http://$DROPLET_IP:8080/latest"
echo ""
echo "View logs:"
echo "  ssh -i $SSH_KEY root@$DROPLET_IP 'sudo docker logs -f itu-minitwit'"
echo ""

