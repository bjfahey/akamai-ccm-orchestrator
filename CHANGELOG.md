# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2026-11-02

### Added
- Initial release of Akamai Cloud Certificate Manager (CCM) Universal Orchestrator Extension
- Support for inventorying certificates in CCM
- Support for certificate enrollment and reenrollment
- EdgeGrid authentication implementation for Akamai API
- CCM API client with support for:
  - List certificates (`GET /ccm/v2/certificates`)
  - Get certificate (`GET /ccm/v2/certificates/{id}`)
  - Create THIRD_PARTY certificate (`POST /ccm/v2/certificates`)
  - Upload signed certificate (`PATCH /ccm/v2/certificates/{id}`)
- Support for required CCM parameters: `contractId` + `groupId`
- Optional `accountSwitchKey` support for multi-account configurations
- Comprehensive documentation and integration manifest

### Notes
- Based on patterns from Keyfactor's akamai-cps-orchestrator
- Implements CCM v2 API (distinct from CPS v2 API)
- Uses RFC 6902 JSON Patch for certificate uploads
- Targets .NET 8.0
