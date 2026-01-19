# Architecture document

## Core principles
The domain state is modelled as a series of events.

The state is immutable - instead, each event creates a new copy of the state when it is modified.

The events contain the logic of how the state must evolve upon the event taking place.

## Auditable modification
Expanding on the immutability idea, the list of events itself that will make up a certain domain state is constructed as an append-only log of modifications brought to the events. 

The modifications support:
    Add
    Delete

## Parallel universes
To begin with, parallel universes can be considered completely independent event streams.

A layer on top should provide the functionality required to manage the independent event streams, including, but not limited to capturing data across universes (e.g. bringing in bulk changes from another universe)

## Validation jobs
Domain specific validation rules
