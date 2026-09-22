# AuthKit Schemas

This document contains the Mermaid diagrams describing AuthKit's token flows and architecture.

## Token Issuance & SDK Request Flow
```mermaid
sequenceDiagram
    participant Dev as Developer
    participant Keycloak as Keycloak
    participant AuthKit as AuthKit API
    participant SDK as AuthKitSdkClient
    participant API as Target API (e.g. Marketplace)

    Dev->>Keycloak: Authenticate via Keycloak (JWT access token)
    Keycloak-->>Dev: Returns access_token (Keycloak JWT)

    Dev->>AuthKit: Request Developer Token<br/>Authorization: Bearer <keycloak_token>
    AuthKit->>AuthKit: Validate Keycloak token<br/>and create DeveloperToken (JWT)
    AuthKit-->>Dev: Returns X-Developer-Token (AuthKit JWT)

    SDK->>API: Request with<br/>Authorization: Bearer <keycloak_token><br/>X-Developer-Token: <authkit_token>
    API->>AuthKit: Validate developer token via REST
    AuthKit->>Keycloak: Validate user session & roles
    AuthKit-->>API: DeveloperToken valid ✅
    API-->>SDK: 200 OK — Operation authorized
```

## External REST vs Internal gRPC Call Flow
```mermaid
flowchart TD
   Dev[Developer] -->|Authenticate| Keycloak[Keycloak JWT]
   Keycloak --> Dev

   Dev -->|Request Dev Token| AuthKit[AuthKit API]
   AuthKit -->|Validate Keycloak token| Keycloak
   AuthKit -->|Return Dev Token| Dev

   Dev -->|Request Service Token| AuthKitService[AuthKit API]
   AuthKitService -->|Validate Dev Token| AuthKit
   AuthKitService -->|Return Service Token| Dev

   Dev -->|Configure SDK| SDK[AuthKitSdkClient]

   SDK -->|REST request| API_REST[Target API REST]
   SDK -->|gRPC request| API_GRPC[Target API gRPC]

   subgraph REST_Flow
      API_REST -->|Pass tokens to middleware| AuthKit_REST[AuthKit Middleware REST]
      AuthKit_REST -->|Validate Dev & Service Tokens| TokenDB[Token Database]
      AuthKit_REST -->|Validate Keycloak JWT| Keycloak
      AuthKit_REST -->|Return auth result| API_REST
      API_REST -->|200 OK / 403 Forbidden| SDK
   end

   subgraph gRPC_Flow
      API_GRPC -->|Pass tokens to middleware| AuthKit_GRPC[AuthKit Middleware gRPC]
      AuthKit_GRPC -->|Validate Dev & Service Tokens| TokenDB
      AuthKit_GRPC -->|Validate Keycloak JWT| Keycloak
      AuthKit_GRPC -->|Return auth result| API_GRPC
      API_GRPC -->|200 OK / 403 Forbidden| SDK
   end
```

## SDK Function Call Flow (AuthKit → gRPC Microservices)
```mermaid
flowchart TD
   classDef token fill:#fef3c7,stroke:#f59e0b,stroke-width:1px,color:#b45309;
   classDef service fill:#fef3c7,stroke:#fef3c7,stroke-width:1px,color:#92400e;
   classDef internal fill:#dbeafe,stroke:#3b82f6,stroke-width:1px,color:#1e40af;

%% Actors
   Dev[Developer] -->|Has Keycloak JWT,<br/>DeveloperToken,<br/>ServiceToken| SDK[AuthKitSdkClient]
   class Dev,SDK token;

%% REST request from external SDK
   SDK -->|REST request with tokens| API_Controller[API Controller]
   API_Controller -->|Validate tokens internally via AuthKit| AuthKit[AuthKit API]

%% gRPC requests
   API_Controller -->|gRPC call| GRPC_Service[gRPC Service]
   GRPC_Service -->|gRPC call to other microservice| AnotherService_GRPC[Another gRPC Service]

%% Responses
   GRPC_Service -->|Return result| API_Controller
   AnotherService_GRPC -->|Return result| GRPC_Service
   API_Controller -->|Return response| SDK

%% Styling
   class Dev,SDK,AuthKit token;
   class API_Controller,GRPC_Service service;
   class AnotherService_GRPC internal;
```