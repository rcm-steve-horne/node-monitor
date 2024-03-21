az acr login -n crrcmdefault
docker build . -t crrcmdefault.azurecr.io/rokos/node-monitor:latest
docker push crrcmdefault.azurecr.io/rokos/node-monitor:latest