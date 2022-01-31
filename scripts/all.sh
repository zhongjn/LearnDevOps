#!/bin/bash
set -e
tag=$(date +%Y-%m-%d-%H-%M)
docker build LearnDevOps.Frontend --tag frontend:$tag --tag frontend:latest
docker tag frontend:latest registry.cn-shanghai.aliyuncs.com/zjn-test-ns/frontend:latest
docker tag frontend:latest registry.cn-shanghai.aliyuncs.com/zjn-test-ns/frontend:$tag
docker push registry.cn-shanghai.aliyuncs.com/zjn-test-ns/frontend:latest
docker push registry.cn-shanghai.aliyuncs.com/zjn-test-ns/frontend:$tag
cd k8s/charts/frontend
helm upgrade --install frontend .