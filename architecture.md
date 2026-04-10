# StreamEditor
In the stream editor layer the user is composing event streams.

The modifications in this layer are auditable - all modifications should carry a timestamp.

# StreamProcessor
Take as input a raw event stream.

As a later optimization, add support for deltas.

# Event processing
Each event type defines the logic of how the event modifies the state maintained by the StreamProcessor.

The interface of event processing allows the events to emit a MaybeEvent back (for subsequent payments on the loan for example)

# State 

## Primitives
Event, Bill, Transaction.

These are the primitives that are persisted. Persistence in this instance is scoped to the lifecycle of a certain event stream being active in the app. 

## Higher level entities
Balance - keeps track of how much a person owes others.

Loan - keeps track of interest rate, next payment, etc.

# Query Service
The query service gives controlled access to data in the state.

## Specialized endpoints
### Specific point in time
Loan, with possibility to split per person.
    - next payment. - split into fee, principal, interest.
    - total / remaining interest to pay.
    - total / remaining principal.

    + related entities timeline:
        Events, Bills that have affected and will affect the loan relative to the current point in time.

Balances - per person:
    X owes Y this much:
    
    + related entities timeline:
        Events, Bills, Transactions that have affected and will affect the balance relative to the current point intime.

### Timeseries value
Numeric value to track over time.
For numeric values such as loan principal, interest paid, etc, to be displayed over time.


## Direct database access.
Endpoint which allows direct access to the underlying database. Interface? First, what's the right database to choose?


# Data model

All entities:
    // that's equivalent to one copy per time in a fully denormalized DB.
    ArrayOf<Time, DataVersion>
    Tags

Immutable (they can be put in one big collection and filtered by time)
    Bills:
        ...
    Transactions:
    Events:

Mutable:
    Loan:
        Evolves with time.
    Balance:
        Evolves with time.

    Current checkpoint based system, where the state is cloned?


For the loan, do I really need the materialization?
    No. The bills are sufficient to understand next payment.
    Remaining balance etc. is a bit harder to calculate, but maybe I can cache it?

    Or can I just query the bills that pay?

Bills carry a pointer to the loan.
    => query for recent / future bills becomes:
        bills.Where(loan == loan).Where(time < currentTime);

Bills carry a pointer to the balance?
    => query for recent becomes:
        bills.Where(affected.Contains(balance)).Where(time < currentTime)


