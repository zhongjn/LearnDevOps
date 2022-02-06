#!/bin/bash

origin=$1 
echo $origin
name_version=`echo "$origin" | awk '{n=split($0,a,"/"); print a[n]}'`
echo $name_version

read -p "Continue (y/n)?" choice
case "$choice" in 
  y|Y )
    echo "yes"
    docker tag $origin registry.cn-shanghai.aliyuncs.com/zjn-test-ns/$name_version
    docker push registry.cn-shanghai.aliyuncs.com/zjn-test-ns/$name_version
  ;;
  n|N )
    echo "no"
  ;;
  * ) echo "invalid";;
esac