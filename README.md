# ITU-MiniTwit

A Twitter-like social media application with a simulator API, deployed on DigitalOcean using Infrastructure as Code.

## Quick Start

```bash
git clone https://github.com/21Philip/devops-minitwit.git
cd devops-minitwit
vagrant up
```

That's it! Your application will be deployed and running.

## Prerequisites

Before running the quick start commands above, ensure you have:

### 1. SSH Key
```bash
ssh-keygen -t rsa -b 4096 -f ~/.ssh/id_rsa -N ""
```

### 2. DigitalOcean Account with Credits
- Create account: https://www.digitalocean.com
- Apply GitHub Education credits: https://education.github.com/pack
- Add your SSH public key: https://cloud.digitalocean.com/account/security/keys
  - Name it: `itu-minitwit-key`
  - Paste contents of: `cat ~/.ssh/id_rsa.pub`

### 3. DigitalOcean API Token
- Create token: https://cloud.digitalocean.com/account/api/tokens
- Name: `itu-minitwit-vagrant`
- Select: "Read and write" permissions
- Copy the token and save it

### 4. Vagrant and Plugins
```bash
# Install Vagrant from: https://www.vagrantup.com/downloads
# Then install the DigitalOcean plugin:
vagrant plugin install vagrant-digitalocean
```

## Deployment

### Step 1: Clone the Repository
```bash
git clone https://github.com/21Philip/devops-minitwit.git
cd devops-minitwit
```

### Step 2: Set Environment Variable
```bash
export DIGITALOCEAN_TOKEN='your_actual_token_here'
```

### Step 3: Provision and Deploy
```bash
vagrant up
```

This command will:
1. Create a DigitalOcean droplet (Ubuntu 22.04 LTS, 1vCPU, 1GB RAM)
2. Install Docker
3. Build the application Docker image
4. Start the containerized application
5. Save the droplet IP to `deployment_ip.txt`

The process takes ~5 minutes. Once complete, your application is live!

## Accessing Your Application

After `vagrant up` completes, get the droplet IP:

```bash
cat deployment_ip.txt
```

Then access:
- **Application:** `http://<droplet_ip>:8080`
- **Simulator API:** `http://<droplet_ip>:8080/latest`

### Example (Current Deployment)
- **Application:** http://209.38.198.112:8080
- **Simulator API:** http://209.38.198.112:8080/latest

## Releasing a New Version

### Create a Release Tag
```bash
./release.sh v1.0.0 "Production release description"
git push origin v1.0.0
```

### Deploy the Release
```bash
./deploy.sh 209.38.198.112 v1.0.0
```

Or deploy the latest code without specifying a version:
```bash
./deploy.sh 209.38.198.112
```

## Infrastructure as Code

All infrastructure is version-controlled and reproducible:

| File | Purpose |
|------|---------|
| `Vagrantfile` | VM provisioning on DigitalOcean |
| `Dockerfile` | Application containerization |
| `deploy.sh` | Automated deployment script |
| `release.sh` | Release tag management |

Changes to any of these files are automatically applied on subsequent `vagrant up` runs.

## Local Development

To run locally without DigitalOcean:

```bash
docker build -t itu-minitwit .
docker run -p 8080:8080 itu-minitwit:latest
```

Application will be accessible at `http://localhost:8080`

## Troubleshooting

### Container not starting
```bash
ssh -i ~/.ssh/id_rsa root@<droplet_ip> 'sudo docker logs itu-minitwit'
```

### Need to redeploy
```bash
vagrant destroy
vagrant up
```

### SSH access to droplet
```bash
ssh -i ~/.ssh/id_rsa root@<droplet_ip>
```

## More Information

For simulator API integration, see [Simulator API README](./out/itu-minitwit-sim-stub/src/Org.OpenAPITools/README.md)

## Current Status

✅ Application deployed and running  
✅ Simulator API integrated  
✅ Infrastructure as Code ready  
✅ Automated deployment scripts available

