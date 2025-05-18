#!/bin/bash
set -e

export ASPNETCORE_URLS=http://+:80

# Detectar ambiente
if [ -d "/app" ]; then
    # Ambiente Docker
    WORKER_DIR="/app/worker"
    API_DIR="/app/api"
    echo "Executando em ambiente Docker"
else
    # Ambiente Windows ou outro
    WORKER_DIR="./CashFlowTransactions.Worker"
    API_DIR="./CashFlowTransactions.API"
    echo "Executando em ambiente não-Docker (possivelmente Windows)"
fi

start_workers() {
    local num_workers=${1:-3}
    local worker_pids=""
    
    echo "Tentando acessar o diretório $WORKER_DIR..."
    cd "$WORKER_DIR" || { echo "ERRO: Diretório $WORKER_DIR não encontrado"; return 1; }
    echo "Diretório $WORKER_DIR acessado com sucesso"
    
    echo "Iniciando $num_workers instâncias do Worker..."
    
    for (( i=1; i<=$num_workers; i++ ))
    do
       echo "Tentando iniciar Worker #$i"
       if [ -f "CashFlowTransactions.Worker.dll" ]; then
           echo "Arquivo CashFlowTransactions.Worker.dll encontrado"
       else
           echo "ERRO: Arquivo CashFlowTransactions.Worker.dll não encontrado"
           return 1
       fi
       
       dotnet CashFlowTransactions.Worker.dll &
       local worker_pid=$!
       echo "Worker #$i iniciado com PID: $worker_pid"
       worker_pids="$worker_pids $worker_pid"
    done
    
    echo "$num_workers Workers iniciados com sucesso."
    echo "PIDs dos workers: $worker_pids"
    echo "$worker_pids"  # Esta linha é importante para retornar os PIDs
}

kill_processes() {
    echo "Encerrando processos..."
    for pid in $@; do
        if ps -p $pid > /dev/null; then
            echo "Encerrando processo $pid"
            kill $pid 2>/dev/null || true
        fi
    done
}

if [ "$APP_TYPE" = "api" ]; then
    echo "Iniciando a API..."
    cd /app/api
    exec dotnet CashFlowTransactions.API.dll
elif [ "$APP_TYPE" = "worker" ]; then
    worker_pids=$(start_workers 10)
    
    trap "kill_processes $worker_pids; exit" SIGINT SIGTERM
    
    wait
elif [ "$APP_TYPE" = "all" ]; then
    echo "Iniciando API e 10 Workers..."
    
    # Iniciar workers primeiro de forma que os logs sejam visíveis
    echo "Chamando função start_workers()..."
    # Não capture a saída para permitir que os logs apareçam
    start_workers 10
    # Capture os PIDs do último echo da função
    worker_pids=$?
    
    echo "Start_workers completou. Verificando resultado..."
    
    # Iniciar API em primeiro plano
    echo "Tentando acessar o diretório $API_DIR..."
    cd "$API_DIR" || { echo "ERRO: Diretório $API_DIR não encontrado"; exit 1; }
    echo "Diretório $API_DIR acessado com sucesso"
    
    if [ -f "CashFlowTransactions.API.dll" ]; then
        echo "Arquivo CashFlowTransactions.API.dll encontrado"
    else
        echo "ERRO: Arquivo CashFlowTransactions.API.dll não encontrado"
        exit 1
    fi
    
    echo "Iniciando API..."
    exec dotnet CashFlowTransactions.API.dll
else
    echo "Variável APP_TYPE não configurada corretamente. Use 'api', 'worker' ou 'all'"
    exit 1
fi 