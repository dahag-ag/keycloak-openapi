#!/usr/bin/env bash
mkdir /output
mkdir /keycloak
git clone -c core.symlinks=true https://github.com/keycloak/keycloak.git
cd /keycloak
git fetch --all --tags
git checkout tags/23.0.0
./mvnw -pl quarkus/deployment,quarkus/dist -am -DskipTests clean install
cd services
mvn -s ../maven-settings.xml -Pjboss-release -DskipTests clean package
cp -R ./target/apidocs-rest/swagger/apidocs /output