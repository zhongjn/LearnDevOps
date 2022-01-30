#!/bin/bash
docker tag test-image:latest registry.cn-shanghai.aliyuncs.com/zjn-test-ns/test-image:latest
docker push registry.cn-shanghai.aliyuncs.com/zjn-test-ns/test-image:latest