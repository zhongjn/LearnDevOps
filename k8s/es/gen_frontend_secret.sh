#!/bin/bash
URI=http://quickstart-es-http:9200
USERNAME=elastic
PASSWORD=$(kubectl get secret quickstart-es-elastic-user -o go-template='{{.data.elastic | base64decode}}')
kubectl create secret generic frontend-es-cred \
    --from-literal=ES_URI=$URI \
    --from-literal=ES_USERNAME=$USERNAME \
    --from-literal=ES_PASSWORD=$PASSWORD