# Keycloak OpenApi Generator

Generates an OpenApi definition for the keycloak admin rest API by parsing the Java 
source code because I couldn't be bothered to patch the actual keycloak source code or wait.

The definitions have been manually skimmed over and the small subset of endpoints we use work (client, user and role endpoints)

## Generated OpenApi Definitions

For Keycloak 16, 17 and 18 can be found under: [OpenApiDefinitions](`./OpenApiDefinitions`)
