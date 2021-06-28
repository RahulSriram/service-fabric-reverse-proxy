#!/bin/bash
echo "Cleaning build directory:"
for i in $(rm -rfv src/ReverseProxyApplication/pkg); do
  echo "Removing $i"
done

printf "\nRestoring sfproj:\n"
nuget restore -PackagesDirectory packages src/ReverseProxyApplication/ReverseProxyApplication.sfproj
printf "\nRestoring solution:\n"
dotnet restore -r linux-x64 ReverseProxyApplication.sln
printf "\nBuilding solution:\n"
dotnet msbuild /p:Deterministic=true /p:DebugType=full /p:DebugSymbols=true /p:RuntimeIdentifier=linux-x64 /p:ImportByWildcardBeforeSolution=false ReverseProxyApplication.sln
printf "\nBuilding sfproj:\n"
dotnet msbuild /t:Package /p:PackageLocation=pkg/ReverseProxyApplicationPkg /p:Deterministic=true /p:RuntimeIdentifier=linux-x64 src/ReverseProxyApplication/ReverseProxyApplication.sfproj

for i in $(find src/ReverseProxyApplication/pkg/ReverseProxyApplicationPkg -iname 'ServiceManifest.xml'); do
  echo "Updating $i"
  service_name=$(echo $i | sed 's|src/ReverseProxyApplication/pkg/ReverseProxyApplicationPkg/||' | sed 's|Pkg/ServiceManifest.xml||')
  service_pkg_dir=$(dirname $i)
  sed -i '' -e 's|<Program>.*</Program>|<Program>entryPoint.sh</Program>|g' $i
  echo "Adding $service_pkg_dir/Code/dotnet-include.sh"
  cp scripts/dotnet-include.sh $service_pkg_dir/Code
  echo "Adding $service_pkg_dir/Code/entryPoint.sh"
  cp scripts/entryPoint.sh $service_pkg_dir/Code
  sed -i '' -e "s|<ServiceName>.dll|$service_name.dll|g" $service_pkg_dir/Code/entryPoint.sh
done