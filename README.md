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
Requires: vagrant, Digital Ocean api token, Digital Ocean ssh key. 

1. Make sure `$DIGITAL_OCEAN_TOKEN` and `$SSH_KEY_NAME` are set in your current environment

2. Navigate to root of repository:
    ```bash
    cd path/to/repository
    ```
3. Deploy:
    ```bash
    vagrant up
    ```