# Minitwit
Requires:
- docker

## How to run Minitwit
1. Navigate to root of repository:
    ```bash
    cd path/to/repository
    ```
2. Build docker image:
    ```bash
    docker build -t <name> .
    ```
3. Run container:
    ```bash
    docker run -p 5273:5273 <name>:latest
    ```
4. Application can be reached at localhost:5273



# ITU-MiniTwit Deployment Guide

## Prerequisites

1. **DigitalOcean Account** with GitHub Education credits applied
2. **Vagrant** installed: https://www.vagrantup.com/downloads
3. **SSH Key** generated: `ssh-keygen -t rsa -b 4096 -f ~/.ssh/id_rsa -N ""`
4. **SSH Key added to DigitalOcean**: https://cloud.digitalocean.com/account/security/keys

## Setup Steps

### 1. Get DigitalOcean API Token

1. Go to: https://cloud.digitalocean.com/account/api/tokens
2. Click "Generate New Token"
3. Name: `itu-minitwit-vagrant`
4. Select "Read and write" permissions
5. Copy token (save it safely!)

### 2. Install Vagrant Plugins

```bash
./setup-vagrant.sh
