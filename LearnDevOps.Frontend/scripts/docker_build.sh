#!/bin/bash
docker build . --tag frontend:$(date +%Y-%m-%d-%H-%M) --tag frontend:latest