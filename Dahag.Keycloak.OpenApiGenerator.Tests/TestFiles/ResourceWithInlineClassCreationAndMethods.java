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

import static org.keycloak.utils.LockObjectsForModification.lockUserSessionsForModification;
import static org.keycloak.util.JsonSerialization.readValue;

import java.security.cert.X509Certificate;
import java.text.ParseException;
import java.text.SimpleDateFormat;
import java.util.Arrays;
import java.util.Date;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.stream.Collectors;
import java.util.stream.Stream;

import javax.ws.rs.BadRequestException;
import javax.ws.rs.Consumes;
import javax.ws.rs.DELETE;
import javax.ws.rs.FormParam;
import javax.ws.rs.GET;
import javax.ws.rs.NotFoundException;
import javax.ws.rs.POST;
import javax.ws.rs.PUT;
import javax.ws.rs.Path;
import javax.ws.rs.PathParam;
import javax.ws.rs.Produces;
import javax.ws.rs.QueryParam;
import javax.ws.rs.core.Context;
import javax.ws.rs.core.HttpHeaders;
import javax.ws.rs.core.MediaType;
import javax.ws.rs.core.Response;
import javax.ws.rs.core.Response.Status;
import javax.ws.rs.core.StreamingOutput;

import com.fasterxml.jackson.core.type.TypeReference;

import org.jboss.logging.Logger;
import org.jboss.resteasy.annotations.cache.NoCache;
import org.jboss.resteasy.spi.ResteasyProviderFactory;
import org.keycloak.Config;
import org.keycloak.KeyPairVerifier;
import org.keycloak.authentication.CredentialRegistrator;
import org.keycloak.authentication.RequiredActionProvider;
import org.keycloak.common.ClientConnection;
import org.keycloak.common.Profile;
import org.keycloak.common.VerificationException;
import org.keycloak.common.util.PemUtils;
import org.keycloak.email.EmailTemplateProvider;
import org.keycloak.events.EventQuery;
import org.keycloak.events.EventStoreProvider;
import org.keycloak.events.EventType;
import org.keycloak.events.admin.AdminEventQuery;
import org.keycloak.events.admin.OperationType;
import org.keycloak.events.admin.ResourceType;
import org.keycloak.exportimport.ClientDescriptionConverter;
import org.keycloak.exportimport.ClientDescriptionConverterFactory;
import org.keycloak.exportimport.ExportAdapter;
import org.keycloak.exportimport.ExportOptions;
import org.keycloak.models.ClientModel;
import org.keycloak.models.ClientScopeModel;
import org.keycloak.models.Constants;
import org.keycloak.models.GroupModel;
import org.keycloak.models.KeycloakSession;
import org.keycloak.models.ModelDuplicateException;
import org.keycloak.models.ModelException;
import org.keycloak.models.RealmModel;
import org.keycloak.models.RequiredActionProviderModel;
import org.keycloak.models.UserModel;
import org.keycloak.models.UserSessionModel;
import org.keycloak.models.utils.KeycloakModelUtils;
import org.keycloak.models.utils.ModelToRepresentation;
import org.keycloak.models.utils.RepresentationToModel;
import org.keycloak.models.utils.StripSecretsUtils;
import org.keycloak.partialimport.PartialImportManager;
import org.keycloak.protocol.oidc.TokenManager;
import org.keycloak.provider.InvalidationHandler;
import org.keycloak.representations.adapters.action.GlobalRequestResult;
import org.keycloak.representations.idm.AdminEventRepresentation;
import org.keycloak.representations.idm.ClientRepresentation;
import org.keycloak.representations.idm.ClientScopeRepresentation;
import org.keycloak.representations.idm.ComponentRepresentation;
import org.keycloak.representations.idm.EventRepresentation;
import org.keycloak.representations.idm.GroupRepresentation;
import org.keycloak.representations.idm.ManagementPermissionReference;
import org.keycloak.representations.idm.PartialImportRepresentation;
import org.keycloak.representations.idm.RealmEventsConfigRepresentation;
import org.keycloak.representations.idm.RealmRepresentation;
import org.keycloak.services.ErrorResponse;
import org.keycloak.services.managers.AuthenticationManager;
import org.keycloak.services.managers.RealmManager;
import org.keycloak.services.managers.ResourceAdminManager;
import org.keycloak.services.resources.admin.ext.AdminRealmResourceProvider;
import org.keycloak.services.resources.admin.permissions.AdminPermissionEvaluator;
import org.keycloak.services.resources.admin.permissions.AdminPermissionManagement;
import org.keycloak.services.resources.admin.permissions.AdminPermissions;
import org.keycloak.storage.DatastoreProvider;
import org.keycloak.storage.ExportImportManager;
import org.keycloak.storage.LegacyStoreSyncEvent;
import org.keycloak.utils.ProfileHelper;
import org.keycloak.utils.ReservedCharValidator;

/**
 * Base resource class for the admin REST api of one realm
 *
 * @resource Realms Admin
 * @author <a href="mailto:bill@burkecentral.com">Bill Burke</a>
 * @version $Revision: 1 $
 */
public class RealmAdminResource {
    protected static final Logger logger = Logger.getLogger(RealmAdminResource.class);
    protected AdminPermissionEvaluator auth;
    protected RealmModel realm;
    private TokenManager tokenManager;
    private AdminEventBuilder adminEvent;

    @Context
    protected KeycloakSession session;

    @Context
    protected ClientConnection connection;

    @Context
    protected HttpHeaders headers;

    public RealmAdminResource(AdminPermissionEvaluator auth, RealmModel realm, TokenManager tokenManager, AdminEventBuilder adminEvent) {
        this.auth = auth;
        this.realm = realm;
        this.tokenManager = tokenManager;
        this.adminEvent = adminEvent.resource(ResourceType.REALM);
    }

    /**
     * Partial import from a JSON file to an existing realm.
     *
     * @param rep
     * @return
     */
    @Path("partialImport")
    @POST
    @Consumes(MediaType.APPLICATION_JSON)
    public Response partialImport(PartialImportRepresentation rep) {
        auth.realm().requireManageRealm();

        PartialImportManager partialImport = new PartialImportManager(rep, session, realm, adminEvent);
        return partialImport.saveResources();
    }

    /**
     * Partial export of existing realm into a JSON file.
     *
     * @param exportGroupsAndRoles
     * @param exportClients
     * @return
     */
    @Path("partial-export")
    @POST
    public RealmRepresentation partialExport(@QueryParam("exportGroupsAndRoles") Boolean exportGroupsAndRoles,
                                                     @QueryParam("exportClients") Boolean exportClients) {
        auth.realm().requireViewRealm();

        boolean groupsAndRolesExported = exportGroupsAndRoles != null && exportGroupsAndRoles;
        boolean clientsExported = exportClients != null && exportClients;

        if (groupsAndRolesExported) {
            auth.groups().requireList();
        }
        if (clientsExported) {
            auth.clients().requireView();
        }

        // service accounts are exported if the clients are exported
        // this means that if clients is true but groups/roles is false the service account is exported without roles
        // the other option is just include service accounts if clientsExported && groupsAndRolesExported
        ExportOptions options = new ExportOptions(false, clientsExported, groupsAndRolesExported, clientsExported);

        ExportImportManager exportProvider = session.getProvider(DatastoreProvider.class).getExportImportManager();

        Response.ResponseBuilder response = Response.ok();

        exportProvider.exportRealm(realm, options, new ExportAdapter() {
            @Override
            public void setType(String mediaType) {
                response.type(mediaType);
            }
            @Override
            public void writeToOutputStream(ConsumerOfOutputStream consumer) {
                response.entity((StreamingOutput) consumer::accept);
            }
        });
        return response.build();
    }

    @Path("keys")
    public KeyResource keys() {
        KeyResource resource =  new KeyResource(realm, session, this.auth);
        ResteasyProviderFactory.getInstance().injectProperties(resource);
        return resource;
    }
}
