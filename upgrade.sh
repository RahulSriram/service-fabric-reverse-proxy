#!/bin/bash
cd `dirname $0`
sfctl application upload --path ./src/ReverseProxyApplication/pkg/ReverseProxyApplicationPkg --show-progress
sfctl application provision --application-type-build-path ReverseProxyApplicationPkg
sfctl application upgrade --app-id fabric:/ReverseProxyApplication --app-version $1 --parameters "{}" --mode Monitored
cd -