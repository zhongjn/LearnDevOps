#/bin/bash
set -e
tag=$1
mode=$2
if [ "local" == "$mode" ]; then
    eval $(minikube docker-env)
fi

docker build LearnDevOps.Frontend --tag frontend:$tag --tag frontend:latest
docker tag frontend:latest registry.cn-shanghai.aliyuncs.com/zjn-test-ns/frontend:latest
docker tag frontend:latest registry.cn-shanghai.aliyuncs.com/zjn-test-ns/frontend:$tag

if [ "local" != "$mode" ]; then
    echo "pushing image to remote registry..."
    docker push registry.cn-shanghai.aliyuncs.com/zjn-test-ns/frontend:latest
    docker push registry.cn-shanghai.aliyuncs.com/zjn-test-ns/frontend:$tag
fi