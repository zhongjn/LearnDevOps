#!/bin/bash
set -e
docker build LearnDevOps.Frontend --tag frontend:$(date +%Y-%m-%d-%H-%M) --tag frontend:latest
docker tag frontend:latest registry.cn-shanghai.aliyuncs.com/zjn-test-ns/frontend:latest
docker push registry.cn-shanghai.aliyuncs.com/zjn-test-ns/frontend:latest
cd k8s/charts/frontend
helm upgrade --install frontend .