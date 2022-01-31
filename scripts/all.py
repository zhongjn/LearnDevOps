import argparse
import os
from datetime import datetime

image_reg_ns="registry.cn-shanghai.aliyuncs.com/zjn-test-ns"
local_cluster=True

def make_image() -> str:
    ver = datetime.now().strftime("%Y-%m-%d-%H-%M")
    err = 0
    mode = ""
    if local_cluster:
        mode = "local"
    else:
        mode = "remote"
    err = os.system("bash ./scripts/make_image.sh {} {}".format(ver, mode))
    if err != 0:
        print("make image error")
        exit(err)
    return ver
    

def helm_upgrade(image_ver: str):
    print("upgrading image to version frontend:{}".format(image_ver))
    os.system("helm upgrade --install frontend k8s/charts/frontend --set imageVersion={}".format(image_ver))
    

def run():
    print("making image...")
    image_ver = make_image()
    print("upgrading helm...")
    helm_upgrade(image_ver)


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--local', help='deploy to local cluster (minikube)', action='store_true')
    args = parser.parse_args()
    local_cluster = args.local
    run()
