FROM eclipse-temurin:21-jammy
RUN apt-get update
RUN apt install -y git
RUN apt install -y maven
COPY generate-keycloak-openapi.sh .
RUN chmod a+x /generate-keycloak-openapi.sh
CMD "/generate-keycloak-openapi.sh"