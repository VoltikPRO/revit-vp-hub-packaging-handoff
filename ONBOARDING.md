# Publisher onboarding (VP-Hub portal)

Steps before coding. Full detail: [`docs/revit-add-in-onboarding.md`](docs/revit-add-in-onboarding.md).

## 1. Product identity

1. Create a **product** in the publisher portal whose **Code** equals your add-in `ProductCode` (e.g. `revit.myproduct`).
2. Do not change `productCode` without coordinating with the platform operator.

## 2. Publisher keys and pinning

1. Ensure **publisher keys** exist: `PublisherId`, `Kid`, algorithm **es256**, public PEM.
2. Use **Pinning & keys** in the portal; copy the generated C# snippet into your constants.
3. On **net48 / Revit 2024**, update embedded P-256 **X/Y** when PEM changes (`reference/LicenseProbe/LicenseProbePinnedEcKey.cs`).

## 3. Entitlements (customers)

Customer orgs need entitlements and license grants for your `productCode`. Publishers manage grants in the portal; platform ops may use admin for org-wide setup.

## 4. Adopt this kit into your repo

```powershell
git clone https://github.com/VoltikPRO/revit-vp-hub-packaging-handoff.git
cd your-revit-plugin
..\revit-vp-hub-packaging-handoff\scripts\adopt-into-project.ps1 -TargetRepo .
```

Then in Cursor: *"Integrate this Revit add-in with VP-Hub using vp-hub-revit-integration skill."*

## 5. Next

- Code integration: [`INTEGRATION-CHECKLIST.md`](INTEGRATION-CHECKLIST.md)
- Release: [`RELEASE.md`](RELEASE.md)
- Support contacts: [`SUPPORT.md`](SUPPORT.md)
