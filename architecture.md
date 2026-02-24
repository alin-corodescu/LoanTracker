# Stream editor layer
In the stream editor layer the user is composing event streams.
The modifications in this layer are auditable - all modifications should carry a timestamp.

## UX Feature: Parallel features.
Support operating on different stream of events. Utitilities such as copy, copy up to a certain time, copy some range of events. etc.

## Event Stream layer
Accepts a list of events as input to calculate intermediary states. 

The stream of events cannot be changed once processed.

Events contain the logic of how the state must evolve.

## Backend feature: Events generating additional events
TODO Should inherit the tags from the original.

## State layer
State objects are immutable. each event generates a new state. How is the state represented and what is captured?

## Domain objects

## Bill 
Amount.
Who paid for it
Who did we pay it for -> between accounts Shares.
Itemized
    Name, amount.
Tags

## Balances
How much each person owes this other person. 
    => Sum of all bills btween A, B. // so I don't really need a state for that.
        it can be calculated at query time.
## Loan
Loan state makes sense to be materialized.
    => Next payment.
    => Overrides for

    State is more of a cache. between bills, transactions and events.

        => simplifies the calculation.
        => calculate ahead of time.
            The state at a certain time.
        => calculate at query time.
Loan payment is modelled as a bill.


#### Person
#### Account
#### 

### Domain validation rules


# Database with veriosining.

For one event stream, the bills can be added to the database. 


# Lifecycle of a bill:

1. BillCreatedEvent is processed.
    A bill is added to the database. 

    Graph layer links it to:
        Person who paid it.
        Person who it




transactions == settle balances (Alin transfers Diana X)
Bills == money going outside.


Stream Editor:
    Edits are logged.

UX: query event stream in the future:
    -> What are the most recent tr


Event Stream:
    event stream is processed.

    <timestamp, state> + event = <timestamp, new_state>
    
    Queries to be answered:
        At a timestamp X:
            -> What is the next payment amount.
            -> Balances: Alin -> Cont comun
            -> Which bills have been paid from account X?
            -> 

Materialized state layer:
    Loan state.
        TotalInterestPaid - query the bill items with a certain tag.


    Accounts.
    Balances.
        Alin owes Diana X.
    
    Can hold pointers to the shared persistence layer.

Share persistence layer:
    Bills?


View over a state:
    Certain tags.

=> 


Functional:
    Balance(EventStream, Timestamp, Alin, Diana)
        Balance(EventStream, Timestamp - 1, Alin, Diana) + aggregate of the last event.

How do I model a bill that is split amongst 2 people?
    Only one person/ comm account is paying.
    The rest are settled via transactions.

Bills and transactions are the core.

State around them:
    Loan.
    Balances.

Events:
    Loan Events.
    Override events (e.g. set balance to 0)

Event editor:


/// Queries:

Next payment, for a given date.
    -> From the loan state at that date.

Interest Paid thus far.
    from persistence: 
        All Biills from the loan, with timestamp < given date.

Remaining principals, per person:
    At a given state.
        Recent events:
            advance payment - is it a bill or just an event?
                Needs to be a bill for tracking purposes.
                
                Maybe acct transactions are also bills?
                    Alin paid Diana 100Eur, on behalf of Alin. 
                    Person, ACcount difference.
                        One person owns multiple accounts.
                        Balances are between Personss? but how do you deal with cont comun then?