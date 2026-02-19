# Minitwit

## How to run locally
Requires: docker

1. Navigate to root of repository:
    ```bash
    cd path/to/repository
    ```
2. Build docker image:
    ```bash
    docker compose build
    ```
3. Run container:
    ```bash
    docker compose up
    ```
4. Reached at:
   - Api: localhost:5000. 
   - Web app: localhost:5001. 

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