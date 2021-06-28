#!/bin/bash
sfctl application delete --application-id fabric:/ReverseProxyApplication
sfctl application unprovision --application-type-name ReverseProxyApplicationType --application-type-version 1.0.0
sfctl store delete --content-path ReverseProxyApplicationPkg