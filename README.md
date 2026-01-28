# Minitwit
Tested on:
- Ubuntu 22.04.4 x86-64
- gcc 11.4.0
- python 3.12, pip 25.0.1

## How to run Minitwit
1. Navigate to root of repository:
    ```bash
    cd path/to/repository
    ```
2. Create virtual environment:
    ```bash
    python -m venv .venv
    source .venv/bin/activate
    pip install -r requirements.txt
    ```
3. Create database (if it doesn't exist):
    ```bash
    ./control.sh init
    ```
4. Start application:
    ```bash
    ./control.sh start
    ```
5. Application can be reached at localhost:5000

## How to build flag_tool.c
1. Navigate to root of repository:
    ```bash
    cd path/to/repository
    ```
2. Build:
    ```bash
    make build
    ```
