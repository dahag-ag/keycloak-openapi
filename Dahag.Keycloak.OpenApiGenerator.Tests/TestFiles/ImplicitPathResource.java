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

import org.jboss.logging.Logger;
import org.jboss.resteasy.annotations.cache.NoCache;
import org.jboss.resteasy.spi.ResteasyProviderFactory;
import org.keycloak.authorization.admin.AuthorizationService;
import org.keycloak.common.Profile;
import org.keycloak.events.Errors;
import org.keycloak.events.admin.OperationType;
import org.keycloak.events.admin.ResourceType;
import org.keycloak.models.ClientModel;
import org.keycloak.models.KeycloakSession;
import org.keycloak.models.ModelDuplicateException;
import org.keycloak.models.RealmModel;
import org.keycloak.models.UserModel;
import org.keycloak.models.utils.ModelToRepresentation;
import org.keycloak.representations.idm.ClientRepresentation;
import org.keycloak.representations.idm.authorization.ResourceServerRepresentation;
import org.keycloak.services.ErrorResponse;
import org.keycloak.services.ErrorResponseException;
import org.keycloak.services.ForbiddenException;
import org.keycloak.services.clientpolicy.ClientPolicyException;
import org.keycloak.services.clientpolicy.context.AdminClientRegisterContext;
import org.keycloak.services.clientpolicy.context.AdminClientRegisteredContext;
import org.keycloak.services.managers.ClientManager;
import org.keycloak.services.managers.RealmManager;
import org.keycloak.services.resources.admin.permissions.AdminPermissionEvaluator;
import org.keycloak.utils.SearchQueryUtils;
import org.keycloak.validation.ValidationUtil;

import javax.ws.rs.Consumes;
import javax.ws.rs.DefaultValue;
import javax.ws.rs.GET;
import javax.ws.rs.NotFoundException;
import javax.ws.rs.POST;
import javax.ws.rs.Path;
import javax.ws.rs.PathParam;
import javax.ws.rs.Produces;
import javax.ws.rs.QueryParam;
import javax.ws.rs.core.Context;
import javax.ws.rs.core.MediaType;
import javax.ws.rs.core.Response;
import java.util.Map;
import java.util.stream.Stream;

import static java.lang.Boolean.TRUE;
import static org.keycloak.utils.StreamsUtil.paginatedStream;

/**
 * Base resource class for managing a realm's clients.
 *
 * @resource Clients
 * @author <a href="mailto:bill@burkecentral.com">Bill Burke</a>
 * @version $Revision: 1 $
 */
public class ClientsResource {
    protected static final Logger logger = Logger.getLogger(ClientsResource.class);
    protected RealmModel realm;
    private AdminPermissionEvaluator auth;
    private AdminEventBuilder adminEvent;

    @Context
    protected KeycloakSession session;

    public ClientsResource(RealmModel realm, AdminPermissionEvaluator auth, AdminEventBuilder adminEvent) {
        this.realm = realm;
        this.auth = auth;
        this.adminEvent = adminEvent.resource(ResourceType.CLIENT);

    }
    
    @GET
    public Stream<ClientRepresentation> getClients() {
        auth.clients().requireList();

        boolean canView = auth.clients().canView();
        Stream<ClientModel> clientModels = Stream.empty();

        if (searchQuery != null) {
            Map<String, String> attributes = SearchQueryUtils.getFields(searchQuery);
            clientModels = canView
                    ? realm.searchClientByAttributes(attributes, firstResult, maxResults)
                    : realm.searchClientByAttributes(attributes, -1, -1);
        } else if (clientId == null || clientId.trim().equals("")) {
            clientModels = canView
                    ? realm.getClientsStream(firstResult, maxResults)
                    : realm.getClientsStream();
        } else if (search) {
            clientModels = canView
                    ? realm.searchClientByClientIdStream(clientId, firstResult, maxResults)
                    : realm.searchClientByClientIdStream(clientId, -1, -1);
        } else {
            ClientModel client = realm.getClientByClientId(clientId);
            if (client != null) {
                clientModels = Stream.of(client);
            }
        }

        Stream<ClientRepresentation> s = ModelToRepresentation.filterValidRepresentations(clientModels,
                c -> {
                    ClientRepresentation representation = null;
                    if (canView || auth.clients().canView(c)) {
                        representation = ModelToRepresentation.toRepresentation(c, session);
                        representation.setAccess(auth.clients().getAccess(c));
                    } else if (!viewableOnly && auth.clients().canView(c)) {
                        representation = new ClientRepresentation();
                        representation.setId(c.getId());
                        representation.setClientId(c.getClientId());
                        representation.setDescription(c.getDescription());
                    }

                    return representation;
                });

        if (!canView) {
            s = paginatedStream(s, firstResult, maxResults);
        }

        return s;
    }
}
