#!/bin/bash
set -e

export ASPNETCORE_URLS=http://+:80

start_workers() {
    local num_workers=${1:-3}
    local worker_pids=""
    
    cd /app/worker
    echo "Iniciando $num_workers instu00e2ncias do Worker..."
    
    for (( i=1; i<=$num_workers; i++ ))
    do
       echo "Iniciando Worker #$i"
       dotnet CashFlowTransactions.Worker.dll &
       worker_pids="$worker_pids $!"
    done
    
    echo "$num_workers Workers iniciados com sucesso."
    echo $worker_pids
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
    echo "Iniciando 10 instu00e2ncias do Worker..."
    worker_pids=$(start_workers 10)
    
    trap "kill_processes $worker_pids; exit" SIGINT SIGTERM
    
    wait
elif [ "$APP_TYPE" = "all" ]; then
    echo "Iniciando API e 10 Workers..."
    
    worker_pids=$(start_workers 10)
    
    cd /app/api
    dotnet CashFlowTransactions.API.dll &
    api_pid=$!
    
    trap "kill_processes $api_pid $worker_pids; exit" SIGINT SIGTERM
    
    wait
else
    echo "Variu00e1vel APP_TYPE nu00e3o configurada corretamente. Use 'api', 'worker' ou 'all'"
    exit 1
fi 