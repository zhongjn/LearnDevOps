#!/bin/bash
docker build . --tag test-image:$(date +%Y-%m-%d-%H-%M) --tag test-image:latest