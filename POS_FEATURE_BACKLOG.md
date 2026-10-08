# Cashere POS feature backlog

This list separates normal POS operations from features that depend on the shop's workflow or devices. “Built in this work” means implemented in the current uncommitted workspace and should be reviewed before release.

## How to prioritize the remaining work

The POS already has the usual checkout loop, catalog and stock management, payment recording, receipts, shifts, sales history, refunds, customers, purchasing, vouchers, reports, backup/restore, cashier PINs and roles. For day-to-day readiness, first confirm the shop's tax/receipt rules, staff permissions, backup recovery, and hardware on the actual checkout computer. Those determine whether existing settings and integrations are correct.

The remaining roadmap items are not all required for every shop. Order types matter for food service and delivery workflows, but simple retail may not need them. EDC and cash-drawer integration matter only if the shop wants automatic device control; manual payment recording still works. Language, number formats, UI density, sounds and licenses are preferences or release polish. Historical sales import is only needed when moving old sales into Cashere; a template is available, and a source export helps map the old POS accurately.

## POS baseline

| Feature | Status | Why it matters / next step |
|---|---|---|
| Hold and resume an order | Built in this work, with an enable/disable setting | Useful at checkout; default enabled. |
| Product stock override | Built in this work | Lets individual products inherit, block, or allow negative stock. |
| Product tax override | Built in this work | Covers products taxed differently from their category or shop default. Confirm actual rates before use. |
| SKU and internal barcode format | Built in this work | Configurable prefix and SKU width; internal EAN-13 prefix. |
| Enforce configured business hours | Built in this work | Optional checkout gate; confirm shop timezone/hours. |
| Sale number format | Built in this work | Configurable prefix, date, and sequence width. |
| Scheduled database backups and retention | Built in this work | Daily local backup while desktop is open; catches up on next launch if the app was closed at the configured time. Keep separate off-device copies for disaster recovery. |
| Owner / Manager / Cashier permissions | Partially built in this work | Cashiers are limited to POS and read-only Products by default; Owners can enable own-sales history and requests for refunds/voids. Managers retain operational admin access but cannot manage cashier accounts or security settings. Manual price overrides and cash-drawer permissions depend on those workflows. |
| Manager approval for sensitive actions | Partially built in this work | Cashier refund and void requests require an active Manager or Owner username/PIN; other sensitive actions still need approval paths. |
| Audit log | Built in this work | Records local changes to key business records with the signed-in operator, timestamp, entity, action, and changed field names. Displayed to the Owner in Security. |
| Catalog import/export | Built in this work (CSV) | Import matches by SKU; duplicate SKUs are skipped by default. Optional updates replace catalog fields but preserve live stock. Categories are matched or created. |
| Sales data export and historical import | Built in this work (XLSX) | Exports include report sheets plus Cashere import sheets for sale, item, payment, refund and refund-line history. Owner can use the template to import mapped sales; product matches are validated, duplicate sale numbers are skipped, and current stock is never changed. Foreign POS files still need mapping into the template; test with a copy of the database first. |
| Cashier voucher permissions | Built in this work | Owner can disable voucher discounts for Cashiers; checkout and authoritative sale validation both enforce the setting. |

## Depends on the shop workflow

| Feature | Status | Needed information |
|---|---|---|
| Default order type | Built in this work | Owner configures 1–10 shop-specific order labels; Cashier selects one per order. “Sale” is the default, so simple retail does not need setup. The selected label is stored with sales, held orders, receipts, history, and XLSX export. |
| Tax behavior and receipt rules | Settings exist; shop-specific rules need confirmation | Country, tax registration, whether displayed prices include tax, and required receipt fields. |
| Language and number/date formats | Partially built in this work | Number/date culture can be selected from OS default or a supported regional culture; existing labels remain English. Full app translation and currency/tax rules still need localization and shop details. |
| Sounds and notifications | Partially built in this work | Owner can enable Windows system sounds for sale success/checkout failure and toggle POS success notices for hold, resume and sale completion. Checkout errors stay visible. Other platforms and OS-level notifications need their own adapters. |
| UI density | Partially built in this work | Comfortable/Compact preference persists and applies to common input, button, picker, and date controls app-wide. Static view spacing is unchanged; review on the shop's screen size before calling the density work complete. |

## Depends on hardware or payment providers

| Feature | Status | Needed information |
|---|---|---|
| Barcode scanner configuration | Partially built in this work | Generic USB keyboard-wedge scanners can auto-add an exact barcode when configured to send Enter; model-specific SDKs and settings need the scanner model. |
| Cash drawer kick | Built for compatible Windows ESC/POS printers | Owner can enable the standard drawer pin 2 pulse after a cash payment and send a manual test pulse. Disabled by default; requires a drawer attached to the selected receipt printer's drawer port and a raw ESC/POS compatible driver. |
| Customer-facing display | Built in this work (generic secondary monitor) | Optional full-screen customer view shows live item quantities, prices, discount, tax and total. If no second monitor is connected, it stays closed. Kiosk displays or custom protocols still need a model and connection details. |
| EDC terminal pairing | Not built | Provider, terminal model, country, and API/SDK availability. Manual payment recording is available as the fallback workflow. |
| Print business logo on receipt | Built in this work on the Windows desktop printer path | Saved PNG/JPEG logos are centered above the receipt text and scaled to paper width. Test on the shop's printer for raster/grayscale quality; other platform printer paths remain limited. |
| Printer support across builds | Limited | The current receipt printer service is desktop-only; mobile/browser builds need their own printer path. |

## Security and maintenance

| Feature | Status | Recommendation |
|---|---|---|
| Local database encryption | Not built | Decide the threat model and recovery-key process before adding encryption. A lost key can make all sales data unrecoverable; this should not be a casual toggle. |
| Third-party licenses page | Built in this work | About lists the desktop runtime package graph, versions, declared license identifiers, and license URLs. Regenerate the checked-in index after restoring packages when dependency versions change. |

## Already present before this work

Cashere already includes product/catalog management, sales checkout, payment recording, receipt printing on the supported desktop path, cashier PIN sign-in, shifts, sales history/reports, refunds, customers, purchases, suppliers, vouchers, database backup/restore, and theme preferences. Existing features should still be checked against the shop's real operating requirements before production use.

## Details that would tailor the remaining features

- Shop type (general retail, café/restaurant, delivery, or other) and which order types staff should select.
- Country, currency, tax registration/rates, whether prices include tax, and required receipt fields.
- Payment provider and terminal model, plus printer, cash drawer, and any customer-display model and connection.
- Preferred app language; screen size if compact layout matters; and which events should sound or notify. Number/date culture is configurable in Preferences.
- A sample spreadsheet or export if historical sales need to be imported.
- For encryption, whether staff share the checkout computer and how an Owner wants to store and recover the encryption key.
