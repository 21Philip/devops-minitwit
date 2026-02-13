#!/bin/bash
set -e

echo "=========================================="
echo "  ITU-MiniTwit Vagrant Setup"
echo "=========================================="

# Check if DIGITALOCEAN_TOKEN is set
if [ -z "$DIGITALOCEAN_TOKEN" ]; then
    echo "ERROR: DIGITALOCEAN_TOKEN environment variable not set!"
    echo ""
    echo "To set it, run:"
    echo "  export DIGITALOCEAN_TOKEN='your_token_here'"
    echo ""
    echo "Get your token at: https://cloud.digitalocean.com/account/api/tokens"
    exit 1
fi

# Check if Vagrant is installed
if ! command -v vagrant &> /dev/null; then
    echo "ERROR: Vagrant is not installed!"
    echo "Install from: https://www.vagrantup.com/downloads"
    exit 1
fi

# Install Vagrant DigitalOcean provider
echo "Installing Vagrant DigitalOcean provider..."
vagrant plugin install vagrant-digitalocean

# Check SSH key exists
if [ ! -f ~/.ssh/id_rsa ]; then
    echo "Generating SSH key..."
    ssh-keygen -t rsa -b 4096 -f ~/.ssh/id_rsa -N ""
fi

echo ""
echo "Setup complete! To deploy, run:"
echo "  export DIGITALOCEAN_TOKEN='your_token_here'"
echo "  vagrant up"
echo ""