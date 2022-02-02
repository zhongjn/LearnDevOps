#!/bin/bash

echo "1. setup istio"
istioctl install

echo "2. setup istio observability tools"
kubectl apply -f istio

echo "3. setup elasticsearch"
kubectl create -f es/crds.yaml
sleep 1
kubectl apply -f es/operator.yaml
kubectl apply -f es/node.yaml
kubectl apply -f es/kibana.yaml

echo "4. setup istio application network"
kubectl apply -f istio-app