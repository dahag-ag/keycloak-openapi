/*
 * Copyright 2016 Red Hat, Inc. and/or its affiliates
 * and other contributors as indicated by the @author tags.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
package org.keycloak.services.resources.admin;

import static java.lang.Boolean.TRUE;


/**
 * Base resource class for managing one particular client of a realm.
 *
 * @resource Clients
 * @author <a href="mailto:bill@burkecentral.com">Bill Burke</a>
 * @version $Revision: 1 $
 */
public class ClientResource {
    protected static final Logger logger = Logger.getLogger(ClientResource.class);
    protected RealmModel realm;
    private AdminPermissionEvaluator auth;
    private AdminEventBuilder adminEvent;
    protected ClientModel client;
    protected KeycloakSession session;

    @Context
    protected ClientConnection clientConnection;

    public ClientResource(RealmModel realm, AdminPermissionEvaluator auth, ClientModel clientModel, KeycloakSession session, AdminEventBuilder adminEvent) {
        this.realm = realm;
        this.auth = auth;
        this.client = clientModel;
        this.session = session;
        this.adminEvent = adminEvent.resource(ResourceType.CLIENT);
    }
    /**
     * Get user sessions for client
     *
     * Returns a list of user sessions associated with this client
     *
     * @param firstResult Paging offset
     * @param firstResultUga Uga
     * @param maxResults Maximum results size (defaults to 100)
     * @return
     */
    @Path("user-sessions")
    @GET
    @NoCache
    @Produces(MediaType.APPLICATION_JSON)
    @Tag(name = KeycloakOpenAPI.Admin.Tags.AUTHENTICATION_MANAGEMENT)
    @Operation( summary = "Get client authenticator providers Returns a stream of client authenticator providers.")
    public Stream<UserSessionRepresentation> getUserSessions(@Parameter( description = "Paging offset") int firstResult) {
        auth.clients().requireView(client);

        firstResult = firstResult != null ? firstResult : -1;
        maxResults = maxResults != null ? maxResults : Constants.DEFAULT_MAX_RESULTS;
        return session.sessions().getUserSessionsStream(client.getRealm(), client, firstResult, maxResults)
                .map(ModelToRepresentation::toRepresentation);
    }
}
