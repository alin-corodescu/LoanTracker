# Architecture document
# Layers
## Event editor layer
In the event editor layer you write event streams. Once a stream is finalized, it is given to the event stream layer for processing.

The modifications in this layer are auditable.

### Feature: parallel universes.

## Event Stream layer
Accepts a list of events as input to calculate intermediary states. The stream of events cannot be changed once processed.

Events contain the logic of how the state must evolve.

### Feature: Events generating additional events
Should inherit the tags from the original.

## State layer
State objects are immutable. each event generates a new state. How is the state represented and what is captured?

### Domain objects
#### Loan
Loan payment is modelled as a bill.
#### Bill 
Value
Which Person paid it.
Who did we pay it for.
    Shares.
Itemized
Tags

#### Person
#### Account
#### 

### Domain validation rules
