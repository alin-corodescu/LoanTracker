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
Amount

Which Person paid it.
Who did we pay it for -> between accounts Shares.
Itemized
Tags

#### Loan
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