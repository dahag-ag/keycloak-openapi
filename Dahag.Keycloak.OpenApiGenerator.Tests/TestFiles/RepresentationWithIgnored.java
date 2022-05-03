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

package org.keycloak.representations.idm;

import org.keycloak.representations.idm.authorization.ResourceServerRepresentation;

import java.util.List;
import java.util.Map;

/**
 * @author <a href="mailto:bill@burkecentral.com">Bill Burke</a>
 * @version $Revision: 1 $
 */
public class ClientRepresentation {
    protected String id;
    protected Boolean enabled;
    @Deprecated
    protected String[] defaultRoles;


    protected List<String> defaultClientScopes;
    protected List<String> optionalClientScopes;

    private ResourceServerRepresentation authorizationSettings;
    private Map<String, Boolean> access;
    protected String origin;

    @JsonIgnore
    public String getId() {
        return id;
    }

    public void setId(String id) {
        this.id = id;
    }

    public Boolean isEnabled() {
        return enabled;
    }
    
    private Boolean isPrivate() {
        return enabled;
    } 
}
