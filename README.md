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
