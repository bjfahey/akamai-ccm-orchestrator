# Quick Start Guide - Akamai CCM Orchestrator

## Prerequisites

1. **Keyfactor Universal Orchestrator** v12.3+
2. **Akamai API Credentials** with CCM access
3. **.NET 8.0 SDK** (for building)

## Setup Steps

### 1. Get Akamai Credentials

In Akamai Control Center:
1. Go to **Account Admin** → **Identity & Access**
2. Create API client with CCM READ-WRITE access
3. Download credentials (contains `host`, `client_token`, `client_secret`, `access_token`)
4. Note your `contractId` and `groupId` from CCM

### 2. Build the Orchestrator

```bash
cd akamai-ccm-orchestrator
dotnet restore
dotnet build --configuration Release
```

The compiled DLL will be in `akamai-ccm-orchestrator/bin/Release/net8.0/`

### 3. Install in Keyfactor

1. Copy the compiled orchestrator to your Keyfactor Universal Orchestrator extensions folder
2. Restart the orchestrator service

### 4. Create Store Type in Keyfactor Command

Use the included `integration-manifest.json` or create manually:

**Basic Settings:**
- Name: `Akamai Cloud Certificate Manager`
- Short Name: `AkamaiCcm`
- Capability: `AkamaiCcm`
- Supports Inventory: ✅
- Supports Add: ✅
- Supports Reenrollment: ✅

**Required Properties:**
| Property | Type | Example |
|----------|------|---------|
| host | String | `akab-xxxxx.luna.akamaiapis.net` |
| access_token | Secret | `akab-xxxxx...` |
| client_token | Secret | `akab-xxxxx...` |
| client_secret | Secret | `xxxxx...` |
| ContractId | String | `ctr_X-XXXXX` |
| GroupId | String | `grp_12345` |
| AccountSwitchKey | String (Optional) | `X-XXXXX:1-XXXXX` |

### 5. Create a Certificate Store

1. In Keyfactor Command: **Certificate Stores** → **Add**
2. Select **Akamai Cloud Certificate Manager**
3. Fill in all required properties
4. Save

### 6. Test Inventory

Run an inventory job to verify the connection. You should see all signed certificates from your CCM account.

### 7. Enroll a Certificate

1. Request a certificate in Keyfactor Command
2. Choose the Akamai CCM certificate store
3. The orchestrator will:
   - Create a THIRD_PARTY enrollment in CCM
   - Upload your signed certificate
   - Activate it in CCM

## Key Differences from CPS

If you're familiar with the CPS orchestrator:

- **API**: CCM uses `/ccm/v2` (not `/cps/v2`)
- **Parameters**: Both `contractId` AND `groupId` required (CPS only needs contractId)
- **Upload**: Single PATCH operation (not the multi-step pendingChanges workflow)
- **Simpler**: CCM is more RESTful and straightforward

## Troubleshooting

**Connection fails:**
- Verify your API credentials are correct
- Ensure the API client has CCM permissions (not just CPS)
- Check that `contractId` and `groupId` are correct

**No certificates in inventory:**
- Only signed certificates appear in inventory
- Certificates in `csr_ready` state without signed certs are skipped

**Enrollment fails:**
- Verify the certificate format is correct
- Check orchestrator logs for detailed error messages
- Ensure trust chain is included if required

## Next Steps

- Review [README.md](README.md) for architecture details
- See [integration-manifest.json](integration-manifest.json) for complete store type definition
- Check the source code in `akamai-ccm-orchestrator/` for customization options
