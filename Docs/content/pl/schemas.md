# Schematy AuthKit

Ten dokument zawiera diagramy Mermaid opisujące przepływy tokenów i architekturę AuthKit.

## Wydawanie tokenów i przepływ żądań SDK
```mermaid
sequenceDiagram
    participant Dev as Deweloper
    participant Keycloak as Keycloak
    participant AuthKit as AuthKit API
    participant SDK as AuthKitSdkClient
    participant API as Docelowe API (np. Marketplace)

    Dev->>Keycloak: Uwierzytelnienie przez Keycloak (token dostępu JWT)
    Keycloak-->>Dev: Zwraca access_token (JWT Keycloak)

    Dev->>AuthKit: Żądanie tokenu deweloperskiego<br/>Authorization: Bearer <keycloak_token>
    AuthKit->>AuthKit: Walidacja tokenu Keycloak<br/>i utworzenie DeveloperToken (JWT)
    AuthKit-->>Dev: Zwraca X-Developer-Token (JWT AuthKit)

    SDK->>API: Żądanie z<br/>Authorization: Bearer <keycloak_token><br/>X-Developer-Token: <authkit_token>
    API->>AuthKit: Walidacja tokenu deweloperskiego przez REST
    AuthKit->>Keycloak: Walidacja sesji i ról użytkownika
    AuthKit-->>API: Token deweloperski poprawny ✅
    API-->>SDK: 200 OK — Operacja autoryzowana
```

## Zewnętrzny REST vs wewnętrzny przepływ gRPC
```mermaid
flowchart TD
   Dev[Deweloper] -->|Uwierzytelnienie| Keycloak[Keycloak JWT]
   Keycloak --> Dev

   Dev -->|Żądanie tokenu dev| AuthKit[AuthKit API]
   AuthKit -->|Walidacja tokenu Keycloak| Keycloak
   AuthKit -->|Zwrot tokenu dev| Dev

   Dev -->|Żądanie tokenu serwisowego| AuthKitService[AuthKit API]
   AuthKitService -->|Walidacja tokenu dev| AuthKit
   AuthKitService -->|Zwrot tokenu serwisowego| Dev

   Dev -->|Konfiguracja SDK| SDK[AuthKitSdkClient]

   SDK -->|Żądanie REST| API_REST[Docelowe API REST]
   SDK -->|Żądanie gRPC| API_GRPC[Docelowe API gRPC]

   subgraph REST_Flow
      API_REST -->|Przekazanie tokenów do middleware| AuthKit_REST[AuthKit Middleware REST]
      AuthKit_REST -->|Walidacja tokenów dev i serwisowych| TokenDB[Baza tokenów]
      AuthKit_REST -->|Walidacja JWT Keycloak| Keycloak
      AuthKit_REST -->|Zwrot wyniku autoryzacji| API_REST
      API_REST -->|200 OK / 403 Forbidden| SDK
   end

   subgraph gRPC_Flow
      API_GRPC -->|Przekazanie tokenów do middleware| AuthKit_GRPC[AuthKit Middleware gRPC]
      AuthKit_GRPC -->|Walidacja tokenów dev i serwisowych| TokenDB
      AuthKit_GRPC -->|Walidacja JWT Keycloak| Keycloak
      AuthKit_GRPC -->|Zwrot wyniku autoryzacji| API_GRPC
      API_GRPC -->|200 OK / 403 Forbidden| SDK
   end
```

## Przepływ wywołań funkcji SDK (AuthKit → mikroserwisy gRPC)
```mermaid
flowchart TD
   classDef token fill:#fef3c7,stroke:#f59e0b,stroke-width:1px,color:#b45309;
   classDef service fill:#fef3c7,stroke:#fef3c7,stroke-width:1px,color:#92400e;
   classDef internal fill:#dbeafe,stroke:#3b82f6,stroke-width:1px,color:#1e40af;

   Dev[Deweloper] -->|Posiada Keycloak JWT,<br/>DeveloperToken,<br/>ServiceToken| SDK[AuthKitSdkClient]
   class Dev,SDK token;

   SDK -->|Żądanie REST z tokenami| API_Controller[Kontroler API]
   API_Controller -->|Wewnętrzna walidacja przez AuthKit| AuthKit[AuthKit API]

   API_Controller -->|Wywołanie gRPC| GRPC_Service[Serwis gRPC]
   GRPC_Service -->|Wywołanie gRPC do innego mikroserwisu| AnotherService_GRPC[Inny serwis gRPC]

   GRPC_Service -->|Zwrot wyniku| API_Controller
   AnotherService_GRPC -->|Zwrot wyniku| GRPC_Service
   API_Controller -->|Zwrot odpowiedzi| SDK

   class Dev,SDK,AuthKit token;
   class API_Controller,GRPC_Service service;
   class AnotherService_GRPC internal;
```
