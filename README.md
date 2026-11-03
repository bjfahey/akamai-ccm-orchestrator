# Akamai Cloud Certificate Manager (CCM) Universal Orchestrator Extension

> **Integration for Keyfactor Universal Orchestrator to manage certificates in Akamai Cloud Certificate Manager (CCM)**

## Overview

The Akamai Cloud Certificate Manager (CCM) Universal Orchestrator Extension enables certificate lifecycle management within Akamai's Cloud Certificate Manager platform. This orchestrator:

- **Inventories** existing certificates in CCM
- **Enrolls** new THIRD_PARTY certificates
- **Renews** existing certificates through reenrollment

### What is Akamai CCM?

Akamai Cloud Certificate Manager (CCM) is Akamai's newer certificate management platform that provides:
- Support for THIRD_PARTY certificates (bring your own certificate)
- Integration with ACME protocol for automated certificate issuance
- Simplified API compared to CPS (`/ccm/v2` vs `/cps/v2`)
- Required parameters: `contractId` **AND** `groupId` on every API call

### CCM vs CPS

| Feature | CCM (`/ccm/v2`) | CPS (`/cps/v2`) |
|---------|----------------|----------------|
| **API Parameters** | Requires `contractId` + `groupId` | Requires only `contractId` |
| **Certificate Upload** | Single PATCH operation with JSON Patch (RFC 6902) | Multi-step `pendingChanges` workflow |
| **Media Types** | Standard `application/json` | Versioned vendor types (e.g., `application/vnd.akamai.cps.enrollment.v11+json`) |
| **THIRD_PARTY Certs** | Native support | Supported via DV enrollments |
| **Simplicity** | Simpler, more RESTful | More complex legacy API |

**This orchestrator is for CCM.** For CPS integration, see [akamai-cps-orchestrator](https://github.com/Keyfactor/akamai-cps-orchestrator).

## Requirements

- Keyfactor Universal Orchestrator version 12.3 or later
- Akamai API credentials with CCM (Cloud Certificate Manager) access
- .NET 8.0 runtime

## Installation

### 1. Akamai Platform Configuration

Create an API credential in Akamai Control Center:

1. Navigate to **Account Admin** → **Identity & Access**
2. Click **Create API client**
3. Select a user with permissions to access CCM
4. Grant **READ-WRITE** access to the CCM APIs
5. Click **Create credential** and download the credentials file

The credentials file contains:
- `client_secret`
- `host`
- `access_token`
- `client_token`

**Important:** Delete the credentials file after saving these values in Keyfactor. Treat it like a plaintext password.

### 2. Store Type Configuration

Create a certificate store type in Keyfactor Command with the following properties:

#### Basic Information
- **Name:** Akamai Cloud Certificate Manager
- **Short Name:** AkamaiCcm
- **Capability:** AkamaiCcm
- **Supports Add:** ✅ Checked
- **Supports Remove:** ❌ Unchecked
- **Supports Discovery:** ❌ Unchecked
- **Supports Reenrollment:** ✅ Checked
- **Supports Create:** ❌ Unchecked

#### Required Properties

| Property Name | Display Name | Type | Description |
|--------------|--------------|------|-------------|
| `host` | Akamai Host | String | The Akamai API host (e.g., `akab-xxxxx.luna.akamaiapis.net`) |
| `access_token` | Access Token | Secret | EdgeGrid access token |
| `client_token` | Client Token | Secret | EdgeGrid client token |
| `client_secret` | Client Secret | Secret | EdgeGrid client secret |
| `ContractId` | Contract ID | String | **Required for CCM** - Your Akamai contract ID |
| `GroupId` | Group ID | String | **Required for CCM** - Your Akamai group ID |
| `AccountSwitchKey` | Account Switch Key | String (Optional) | For multi-account configurations |

## Usage

### Creating a Certificate Store

1. In Keyfactor Command, navigate to **Certificate Stores**
2. Click **Add** and select **Akamai Cloud Certificate Manager**
3. Fill in the required properties (host, tokens, contractId, groupId)
4. Save the certificate store

### Inventory

The orchestrator will automatically discover all certificates in your CCM account that have been signed (certificates in `csr_ready` state without signed certificates are skipped).

### Enrollment/Reenrollment

To enroll a new certificate or renew an existing one:

1. The orchestrator receives a certificate from Keyfactor Command
2. Creates a new THIRD_PARTY enrollment in CCM (or uses existing certificate ID)
3. Uploads the signed certificate and trust chain to CCM via PATCH operation
4. CCM moves the enrollment from `csr_ready` to active state

## Architecture

### Project Structure

```
akamai-ccm-orchestrator/
├── akamai-ccm-orchestrator/          # Main orchestrator project
│   ├── Jobs/
│   │   ├── CcmJob.cs                 # Base job class
│   │   ├── Inventory.cs              # Inventory job
│   │   └── Management.cs             # Enrollment/reenrollment job
│   ├── Models/
│   │   ├── CcmCertificate.cs         # Certificate model
│   │   ├── CcmClient.cs              # CCM API client
│   │   └── ...
│   ├── Factories/
│   │   └── CcmClientFactory.cs
│   └── manifest.json
├── http-interface/                    # HTTP client with EdgeGrid auth
│   ├── AkamaiAuth.cs                 # EdgeGrid authentication
│   ├── HttpInterface.cs              # HTTP client wrapper
│   └── Contexts/
├── akamai-ccm-orchestrator.Tests/    # Unit tests
└── integration-manifest.json
```

### CCM API Workflow

#### Certificate Creation & Upload

```
1. POST /ccm/v2/certificates?contractId=X&groupId=Y
   → Returns certificateId, status: "csr_ready"

2. GET /ccm/v2/certificates/{id}?contractId=X&groupId=Y
   → Returns certificate with csrPem

3. PATCH /ccm/v2/certificates/{id}?contractId=X&groupId=Y
   Content-Type: application/json-patch+json
   Body: [
     { "op": "add", "path": "/signedCertificatePem", "value": "<cert PEM>" },
     { "op": "add", "path": "/trustChainPem", "value": "<chain PEM>" }
   ]
   → Certificate becomes active
```

## Key Differences from CPS Orchestrator

1. **API Version:** CCM uses `/ccm/v2`, CPS uses `/cps/v2`
2. **Required Parameters:** CCM requires both `contractId` AND `groupId`, CPS only needs `contractId`
3. **Certificate Upload:** CCM uses simple JSON Patch, CPS uses complex `pendingChanges` workflow
4. **Content Types:** CCM uses standard `application/json`, CPS uses vendor-specific media types
5. **Workflow Simplicity:** CCM is significantly simpler with fewer steps

## Based On

This orchestrator was built by combining patterns from:
- [Keyfactor akamai-cps-orchestrator](https://github.com/Keyfactor/akamai-cps-orchestrator) - C# structure, EdgeGrid auth, Keyfactor integration
- [akamai-cert-manager](../akamai-cert-manager) - CCM API workflows, endpoint patterns, query parameters

## License

Apache License 2.0

## Support

This is a community-supported integration. For issues or questions:
- Open an issue in this repository
- Contact your Keyfactor representative

## Contributing

Pull requests are welcome! Please ensure:
- Code follows existing patterns
- All tests pass
- Documentation is updated
