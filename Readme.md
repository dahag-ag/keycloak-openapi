# Maintenance notice
Keycloak now generates openapi specs which can be accessed from the official rest api documentation https://www.keycloak.org/docs-api/latest/rest-api/index.html

# Keycloak OpenApi Generator

This was once a C# exe that parsed the keycloak source code to generate an openapi definition (definitions from 16 to 22) 
but with keycloak 23, keycloak can now generate an openapi definition by itself and uses it as a basis for their rest-api html documentation. 
Sadly the raw openapi files are not included with the html documentation yet but there is a pending PR keycloak/keycloak#22940 to rectify that~


So in the meantime this shell script will do.

## Generated OpenApi Definitions (16-23)

Can be found under: [OpenApiDefinitions](./OpenApiDefinitions)
