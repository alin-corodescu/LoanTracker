# Bills Feature

## Overview

The LoanSplitter application now includes a comprehensive **Bills** feature that allows you to track all expenditures for all participants. This expands the application beyond simple loan tracking into general-purpose budget tracking.

## What are Bills?

A **Bill** is an entity that tracks expenditures for participants. Each bill contains:
- A unique name (billName)
- A description
- A date
- A list of bill items

Each **BillItem** represents a single line item in a bill and contains:
- An amount
- A person name (who paid or is responsible)
- A category (e.g., "LoanPayment", "Groceries", "Utilities")

## Domain-Driven Design

The Bill domain class encapsulates all the logic for managing bills:

### Creating Bills

1. **Manual creation**: Use the constructor with explicit bill items
2. **Split bills by shares**: Use `Bill.CreateSplitBill()` to automatically split a total amount by participant shares

### Applying Bills to Accounts

Bills have a built-in `ApplyToAccount()` method that:
- Takes an account as input
- Creates an `AccountTransaction` for each bill item
- Returns an updated account with all transactions applied
- Ensures account updates are consistent and centralized

This design eliminates code duplication and makes bill handling consistent across manual bills and loan payments.

## How Bills Work

### Automatic Bill Creation

Bills are automatically created when loan payments are made. When a `LoanPaymentEvent` is processed:
1. The loan payment is calculated and split between participants
2. A bill is automatically created with the name pattern: `{loanName}_payment_{date}`
3. Each participant's payment is recorded as a `BillItem` with category "LoanPayment"
4. The associated account is updated with transactions for each bill item
5. The bill is stored in the state for future reference

### Manual Bill Creation

You can also create bills manually for any type of expenditure using the `BillCreatedEvent`. This is useful for tracking:
- Groceries
- Utilities
- Household expenses
- Any shared costs

## JSON Event Format

### Manual Bill Creation

To create a bill manually, use the following JSON format:

```json
{
  "type": "BillCreated",
  "date": "2025-12-01",
  "billName": "groceries_december",
  "description": "December grocery shopping",
  "acctName": "creditAcct",
  "items": [
    {
      "amount": 500.00,
      "person": "Alin",
      "category": "Groceries"
    },
    {
      "amount": 300.00,
      "person": "Diana",
      "category": "Groceries"
    }
  ]
}
```

### Example with Multiple Categories

```json
{
  "type": "BillCreated",
  "date": "2025-12-15",
  "billName": "household_expenses_december",
  "description": "December household expenses",
  "acctName": "creditAcct",
  "items": [
    {
      "amount": 1200.00,
      "person": "Alin",
      "category": "Utilities"
    },
    {
      "amount": 450.00,
      "person": "Diana",
      "category": "Internet"
    },
    {
      "amount": 650.00,
      "person": "Alin",
      "category": "Groceries"
    }
  ]
}
```

## Benefits

1. **Unified Tracking**: Track both loan payments and general expenses in one place
2. **Per-Person Attribution**: See exactly who paid what and for what category
3. **Budget Analysis**: Analyze spending by category, person, or time period
4. **Audit Trail**: Every bill is stored in the event stream with full history
5. **Account Integration**: Bills automatically update account balances through the domain layer
6. **No Code Duplication**: Account transaction logic is centralized in the Bill class
7. **Flexible Splitting**: Create bills split by arbitrary shares for any expenditure type

## Creating Split Bills

You can create bills that are automatically split between participants by shares:

```csharp
var shares = new Dictionary<string, double>
{
    { "Alin", 0.6 },    // Alin pays 60%
    { "Diana", 0.4 }     // Diana pays 40%
};

var bill = Bill.CreateSplitBill(
    description: "Electric bill",
    date: new DateTime(2025, 12, 1),
    category: "Utilities",
    totalAmount: 1000.0,
    shares: shares
);

// Bill will have two items:
// - Alin: 600.0, category "Utilities"
// - Diana: 400.0, category "Utilities"
```

Shares must sum to 1.0 (representing 100% of the bill).

## Implementation Details

### Domain Entities

- `Bill`: The main entity stored in the state with a unique name
- `BillItem`: A value object representing a single line item

### Events

- `BillCreatedEvent`: Creates a new bill and updates the associated account
- `LoanPaymentEvent`: Now automatically creates a bill when processing payments

### Account Updates

When a bill is created (manually or automatically):
1. Each bill item is converted to an `AccountTransaction`
2. Transactions are appended to the specified account
3. The bill entity is stored in the state under its unique name

## Future Enhancements

Possible future improvements:
- Bill summary endpoints to aggregate spending by category/person
- Date range queries for bills
- Bill editing/correction events
- Split bills where costs are divided differently than payments
- Recurring bill support

## Example Complete Event Stream

```json
[
  {
    "type": "LoanContracted",
    "date": "2025-10-15",
    "loanName": "apartLoan",
    "principal": 1000000,
    "nominalRate": 4.5,
    "term": 360,
    "backingAccountName": "creditAcct",
    "name1": "Alin",
    "name2": "Diana"
  },
  {
    "type": "BillCreated",
    "date": "2025-11-01",
    "billName": "utilities_november",
    "description": "November utilities",
    "acctName": "creditAcct",
    "items": [
      {
        "amount": 800.00,
        "person": "Alin",
        "category": "Utilities"
      },
      {
        "amount": 400.00,
        "person": "Diana",
        "category": "Utilities"
      }
    ]
  }
]
```

When the loan payment occurs (automatically scheduled), it will create a bill like:
- Bill name: `apartLoan_payment_2025-11-30`
- Description: "Loan payment for apartLoan"
- Items: One per participant with category "LoanPayment"
