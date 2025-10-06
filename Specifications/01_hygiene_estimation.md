# Specification: Public-Bathroom Hygiene Demand Estimator

## Goal

Estimate required quantities of hygiene products (paper towels and liquid soap) for a hotel’s **public** restrooms over a chosen time horizon, based on:

* Number of hotel guests per day
* Recurring events (how often and average attendees)
* Configurable behavioral assumptions (hand-washing frequency, towel sheets per wash, soap per wash)
* Optional safety stock and supplier lead time

Deliver:

1. A Web API in `WebApi` project (`/api/hygiene/estimate`) implementing the logic below.
2. A UI component in `WebUI` project (Angular) where a user enters inputs and gets the estimation, with export and print options.

## Definitions & Assumptions (configurable defaults)

All defaults must be externalized (e.g., `appsettings.json`) and overrideable via request payload.

### Core behavioral assumptions

* **Hand-wash probability after a restroom visit (`p_wash`)**: `0.85` (85%).
* **Average restroom visits in public areas**:
  * **Overnight guest (`visits_guest`)**: `0.8` visits/guest/day (most use in-room bathrooms).
  * **Event attendee (`visits_attendee`)**: `1.2` visits/attendee/event day.
* **Paper towels per hand-wash**:
  * **Sheets per wash (`sheets_per_wash`)**: `2.5` sheets (average).
  * **Sheets per case (`sheets_per_case`)**: `4000` sheets/case (e.g., 16 packs × 250).
* **Soap per hand-wash**:
  * **Pumps per wash**: `2.0`.
  * **mL per pump**: `1.2`.
  * **mL per wash (`ml_per_wash`)**: `2.4` mL (derived).
  * **mL per carton (`ml_per_carton`)**: `6000` mL (e.g., 6 × 1000 mL refills).

### Operational assumptions

* **Staff/visitor uplift (`uplift_misc`)**: `0.10` (10%) extra hand-washes vs. sum of guests and attendees.
* **Safety stock (`safety_stock_pct`)**: `0.20` (20%).
* **Lead time (`lead_time_days`)**: `7` calendar days.
* **Time horizon for estimate (`horizon`)**: one of `daily`, `weekly`, or `monthly` (30-day).

### Restroom/dispenser assumptions (for refill counts; quantities don’t depend on restroom count)

* **Towel dispenser capacity (`sheets_per_dispenser`)**: `600` sheets.
* **Soap dispenser capacity (`ml_per_dispenser`)**: `1000` mL.

> The API must return both the **effective** values used (defaults possibly overridden) and the estimates.

## Inputs

The **user** provides:

* `hotelGuestsPerDay` (integer, ≥0): average overnight guests/day.
* `events` (array, each with):
  * `name` (string)
  * `occurrencesPerWeek` (number ≥0; can be non-integer, e.g., 2.5)
  * `avgAttendees` (integer ≥0)
* `horizon` (string): `"daily" | "weekly" | "monthly"`
* Optional overrides for any assumption defined above.
* Optional: `numberOfPublicRestrooms` (integer ≥0) — used only to display average refills/restroom; not required for quantity calculations.

Validation rules:

* Reject negative numbers.
* Cap absurd inputs with warnings (e.g., >20 events/day equivalent, >10k guests/day) but still compute.

## Calculation Logic

Let:

* `G = hotelGuestsPerDay`
* For each event `i`: weekly attendee-days = `events[i].occurrencesPerWeek * events[i].avgAttendees`
* Convert weekly to daily average attendees:
  * `A_daily = (Σ weekly attendee-days) / 7`
* **Base public-restroom visits/day**:
  * Guests: `Vg = G * visits_guest`
  * Event attendees: `Va = A_daily * visits_attendee`
  * Total (before uplift): `V0 = Vg + Va`
* **Add staff/visitor uplift**:
  * `V = V0 * (1 + uplift_misc)`
* **Hand-washes/day**:
  * `W = V * p_wash`

**Paper towels**

* Sheets/day: `S_day = W * sheets_per_wash`
* Cases/day: `C_towel_day = S_day / sheets_per_case`
* Apply horizon multiplier `H` (`1`, `7`, `30`) and safety stock:
  * `S_h = S_day * H`
  * `S_h_ss = S_h * (1 + safety_stock_pct)`
  * `Cases_h = ceil(S_h_ss / sheets_per_case)`

**Soap**

* mL/day: `M_day = W * ml_per_wash`
* Cartons/day: `Cart_day = M_day / ml_per_carton`
* With horizon and safety stock:
  * `M_h = M_day * H`
  * `M_h_ss = M_h * (1 + safety_stock_pct)`
  * `Cartons_h = ceil(M_h_ss / ml_per_carton)`

**Refills (operational planning)**

* Towel refills/day (across all dispensers): `Refill_towel_day = S_day / sheets_per_dispenser`
* Soap refills/day: `Refill_soap_day = M_day / ml_per_dispenser`
* If `numberOfPublicRestrooms = R > 0`, also return **average** refills per restroom per day (divide by `R`).

**Reorder points** (optional in response)

* Daily demand with safety factor: `DD_towel = S_day * (1 + safety_stock_pct)`; `DD_soap = M_day * (1 + safety_stock_pct)`
* Reorder point by lead time:

  * `ROP_towel_sheets = DD_towel * lead_time_days`
  * `ROP_soap_ml = DD_soap * lead_time_days`
  * Express also as cases/cartons (ceil by pack size).

## Edge Cases & Notes

* If `events` is empty or all zeros, compute hotel-only demand.
* If `G=0` but events exist, compute from events only.
* If `horizon` is weekly/monthly and `occurrencesPerWeek` is fractional, keep exact ratio (no rounding) when deriving `A_daily`.
* Return warnings array for out-of-range or capped inputs.
* Always include the **assumptionsUsed** object in the response so the estimate is auditable.

## Deliverable 1: Web API (`WebApi` project)

### Endpoint

* **POST** `/api/hygiene/estimate`

### Request JSON (DTO)

**Illustrative numbers, not actual values.**

```json
{
  "hotelGuestsPerDay": 350,
  "events": [
    { "name": "Conference A", "occurrencesPerWeek": 3, "avgAttendees": 120 },
    { "name": "Wedding", "occurrencesPerWeek": 0.5, "avgAttendees": 180 }
  ],
  "horizon": "weekly",
  "overrides": {
    "p_wash": 0.9,
    "visits_guest": 0.7,
    "visits_attendee": 1.3,
    "sheets_per_wash": 2.3,
    "sheets_per_case": 4000,
    "ml_per_wash": 2.4,
    "ml_per_carton": 6000,
    "uplift_misc": 0.1,
    "safety_stock_pct": 0.2,
    "lead_time_days": 7,
    "sheets_per_dispenser": 600,
    "ml_per_dispenser": 1000
  },
  "numberOfPublicRestrooms": 8
}
```

### Response JSON

**Illustrative numbers, not actual values.**

```json
{
  "timeHorizon": "weekly",
  "demand": {
    "paperTowels": {
      "sheets": 123456,
      "cases": 31,
      "dailyAvgSheets": 17637.99,
      "refillsPerDay": {
        "totalDispenserRefills": 29.4,
        "avgPerRestroom": 3.68
      },
      "reorderPoint": {
        "sheets": 12345,
        "cases": 4
      }
    },
    "soap": {
      "ml": 345678,
      "cartons": 58,
      "dailyAvgMl": 49382.57,
      "refillsPerDay": {
        "totalDispenserRefills": 49.4,
        "avgPerRestroom": 6.18
      },
      "reorderPoint": {
        "ml": 23456,
        "cartons": 4
      }
    }
  },
  "intermediate": {
    "guestsPerDay": 350,
    "attendeesPerDay": 69.29,
    "visitsGuests": 280.0,
    "visitsAttendees": 83.15,
    "visitsTotalBeforeUplift": 363.15,
    "visitsTotalAfterUplift": 399.47,
    "handWashesPerDay": 339.55
  },
  "assumptionsUsed": {
    "p_wash": 0.9,
    "visits_guest": 0.7,
    "visits_attendee": 1.3,
    "sheets_per_wash": 2.3,
    "sheets_per_case": 4000,
    "ml_per_wash": 2.4,
    "ml_per_carton": 6000,
    "uplift_misc": 0.1,
    "safety_stock_pct": 0.2,
    "lead_time_days": 7,
    "sheets_per_dispenser": 600,
    "ml_per_dispenser": 1000
  },
  "warnings": [
    "Capped events at 20 per day equivalent for sanity check."
  ]
}
```

> Numbers above are illustrative; the service must compute real values.

## Deliverable 2: UI Component (`WebUI` project)

### Technology

* If `WebUI` is Blazor: create `Pages/HygieneEstimator.razor`.
* If `WebUI` is React/SPA: create `src/features/hygiene/HygieneEstimator.tsx`.
* The spec below applies generically; map to your stack accordingly.

### Layout & UX

* **Form section**

  * Inputs:

    * `Hotel guests per day` (int)
    * **Events** (dynamic rows):

      * `Name`
      * `Occurrences per week` (number)
      * `Average attendees` (int)
      * [Add Event] / [Remove]
    * `Horizon` (radio: Daily / Weekly / Monthly)
    * **Optional overrides** (collapsible “Advanced”):

      * All assumption fields listed above; show units and defaults as placeholders; tooltips explaining impact.
    * `Number of public restrooms` (optional)
  * Validation and inline errors; disable submit if invalid.
  * [Estimate] button posts to `/api/hygiene/estimate`.

* **Results section**

  * Cards or table summarizing:

    * **Paper towels**: total sheets & cases for horizon; daily average; total refills/day; avg per restroom; reorder point (sheets & cases).
    * **Soap**: total mL & cartons; daily average; total refills/day; avg per restroom; reorder point (mL & cartons).
  * Show **intermediate** breakdown (collapsible) for transparency.
  * **Assumptions used** panel (the final merged defaults+overrides).

### UI acceptance criteria

* ✅ Form persists last inputs in session (optional).
* ✅ Calling the API renders results in <1s for typical inputs.
* ✅ Numbers formatted with units; pack counts are integers (ceil).
* ✅ CSV contains both demand and assumptions.
* ✅ Accessibility: labeled inputs, keyboard-navigable event rows.

---

## CSV Export (example columns)

`timeHorizon,guestsPerDay,attendeesPerDay,handWashesPerDay,paperSheetsTotal,paperCasesTotal,soapMlTotal,soapCartonsTotal,refillsTowelPerDay,refillsSoapPerDay,safetyStockPct,leadTimeDays,assumptionsJSON`

---

## Non-functional Requirements

* **Config management**: All defaults in `appsettings.json` (`HygieneEstimatorDefaults` section); DI options pattern; validation on startup.
* **Testing**: Unit tests for calculator service; snapshot tests for JSON response shapes.
* **Observability**: Log request (without PII) and computed totals; include `assumptionsUsed` hash for audit.
* **Performance**: O(1) calculation; negligible memory; no external calls besides the API itself.

---

## Worked Example (sanity check)

Input:

* `G=400`
* Events: 2/wk × 150, 1/wk × 300 ⇒ weekly attendee-days = `2*150 + 1*300 = 600` ⇒ `A_daily = 600/7 ≈ 85.71`
* Defaults elsewhere; `horizon=weekly`.

Steps:

* `Vg = 400 * 0.8 = 320`
* `Va = 85.71 * 1.2 ≈ 102.85`
* `V0 = 422.85`; `V = 422.85 * 1.10 ≈ 465.14`
* `W = 465.14 * 0.85 ≈ 395.37`
* **Towels**: `S_day = 395.37 * 2.5 ≈ 988.43` sheets/day
  Weekly with 20% SS: `988.43*7*1.2 ≈ 8298.8` ⇒ `Cases = ceil(8298.8/4000) = 3`
* **Soap**: `M_day = 395.37 * 2.4 ≈ 948.89` mL/day
  Weekly with 20% SS: `948.89*7*1.2 ≈ 7970.7` ⇒ `Cartons = ceil(7970.7/6000) = 2`

Return these and the intermediate values.

---

## Implementation Notes

* Encapsulate math in a pure service, e.g., `IHygieneEstimator`.
* Separate **input validation** from **calculation**; surface a `warnings` list.
* Use decimal for user-facing aggregated values; only `ceil` pack counts.

---

If you want, I can turn this into concrete DTO classes, a C# service skeleton, and a Blazor/React stub next.
