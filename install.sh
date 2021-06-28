#!/bin/bash

uninstall () {
  echo "Uninstalling App as installation failed... Please try installation again."
  ./uninstall.sh
  exit
}

waitForProvisioning () {
  while true; do
    provision_status=$(sfctl application type --application-type-name ReverseProxyApplicationType)
    if [ $(echo $provision_status | jq -r '.items[0].status') = "Available" ]; then
      echo $(echo $provision_status | jq -r '.items[0].status')
      break
    fi

    echo $(echo $provision_status | jq -r '.items[0].status'): $(echo $provision_status | jq -r '.items[0].statusDetails')
    sleep 2
  done
}

./build.sh

while true; do
  echo "Waiting for cluster ready."
  if sfctl cluster select; then
    break
  fi
  sleep 2
done

if ! sfctl application upload --compress --path ./src/ReverseProxyApplication/pkg/ReverseProxyApplicationPkg --show-progress --timeout 3000; then
  uninstall
fi

if ! sfctl application provision --application-type-build-path ReverseProxyApplicationPkg --no-wait; then
  uninstall
fi
waitForProvisioning

if ! sfctl application create --app-name fabric:/ReverseProxyApplication --app-type ReverseProxyApplicationType --app-version 1.0.0; then
  uninstall
fi