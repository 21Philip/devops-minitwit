# Minitwit

A simple Twitter clone for the BSDSESM1KU course at ITU. Devops have been the main focus of this project.

By DevopsGruppeConnie (group i)

## Demos
### IaC
<div style="text-align: center; max-width: 100%;">
    <video controls style="width: 80%; height: auto;">
        <source src="report/images/iac.mp4" type="video/mp4">
    </video>
</div>

### CI-CD
<div style="text-align: center; max-width: 100%;">
    <video controls style="width: 80%; height: auto;">
        <source src="report/images/ci-cd.mp4" type="video/mp4">
    </video>
</div>

### Monitoring
<div style="text-align: center; max-width: 100%;">
    <video controls style="width: 80%; height: auto;">
        <source src="video3.mp4" type="video/mp4">
    </video>
</div>

### Logiing
<div style="text-align: center; max-width: 100%;">
    <video controls style="width: 80%; height: auto;">
        <source src="video4.mp4" type="video/mp4">
    </video>
</div>

## How to run locally
### Requires: 
- [Docker](https://www.docker.com/)

### Steps:
1. Navigate to root of repository:
```bash
    cd path/to/repository
```

2. Build and run development compose:
```bash
    docker compose -f docker-compose-db.yml -f docker-compose-app.yml -f docker-compose.dev.yml up --build
```

3. Reached at:
   - Api: localhost:5000. 
   - Web app: localhost:5001. 
   - Monitoring: localhost:3000.

## How to deploy
### Requires: 
- [Terraform](https://developer.hashicorp.com/terraform) 
- A valid terraform.tfvars is in the ./infra directory, 
- Authentication to cecilieelkjaer @ dockerhub (our artifact store)

### Steps:
1. Navigate to root of repository:
```bash
    cd path/to/repository
```

2. Build and push docker containers to artifact store
```bash
    docker compose -f docker-compose-app.yml -f docker-compose-db.yml build
    docker compose -f docker-compose-app.yml -f docker-compose-db.yml push
```

3. Navigate to the ./infra directory:
```bash
    cd ./infra
```

4. Deploy:
```bash
    terraform init
    terraform apply
```

## Create release 
Releases are only made once there has been pushed with a tag. 
Create a release like this: 
```bash
    git tag v1.0.0
    git push origin v1.0.0
```
Tag must begin with v (fx v1.2.3). Pushing with a tag will not trigger deploy. 