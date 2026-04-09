docker --version
docker info# Minitwit

## How to run locally
Requires: docker

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
Requires: Terraform

1. Make sure a valid terraform.tfvars is in the ./infra directory

2. Navigate to the ./infra directory:
    ```bash
    cd path/to/repository/infra
    ```
3. Deploy:
    ```bash
    terraform init
    terraform plan
    terraform apply
    ```

Deploy will only run automatically when there's been pushed directly to main. 

## Create release 
Releases are only made once there has been pushed with a tag. 
Create a release like this: 
```bash
    git tag v1.0.0
    git push origin v1.0.0
```
Tag must begin with v (fx v1.2.3). Pushing with a tag will not trigger deploy. 