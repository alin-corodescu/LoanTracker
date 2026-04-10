# User journey
Plan of action:
    Add a basic editor for events.
    Hook it up to a proper DB (or file in OneDrive).
    Create parallel universe feature (clone) to keep main branch correct and add confirmations for the main branch modifications.

Then I can ask CC to implement the additinal features by referring the existing components and with existing patterns.

Map user journeys to the layers and if possible, components.

Bills:
    Who paid what share: OneOf<Alin, Diana, ContComun>
    Who should support the bill: Share<Alin, Diana, ContComun>

    When an e


Accounts:
    Alin, Diana, Cont Comun.
Balances:
    Alin owes

# Utilities:
Currency exchange at a specific date
    
## Basic loan management.

/add event editor with the right metadata captured (when the change was made, to which date does it pertain)
/add Query for: interest paid in the last year?
/add interest paid by each person
/add utilitati si alte costuri de apartament.


### Bills.
-> Are inputs to the system.
    -> can be categorized   
    -> can be split between participants

-> Output: 
    how much each person should have contributed to cont comun.
    
## Bugetel settlement
    Cum facm noi settle?
        Contributia mea e 2.3 luni peste Diana.
        DAca punem 3 luni,
            Put si eu 0.7
            Pune diana 3 luni
        Daca punem 2 luni,
            Pune diana 2 luni.
            Diana imi da 0.15 mie? -- cum modelez asta?
        Surplus Alin:   
            Contul comun datoreaza lui Alin 2.3 luni de bani.
                => Diana ii datoreaza lui Alin 1.15 luni.
            Diana trb sa puna 2 luni 
                => Diana ii datoreaza contului comun 2 luni de bani 
                => Diana transfer 2 luni contului comun (tx1)
            Alin trb sa puna 2 luni
                => datoria se reduce la 0.3 luni intre Alin si contul comun.
                => Diana transfera lui Alin 0.15 luni.
                    => Diana transfera la contul comun 0.15 luni.
                    => contul comun ii da lui Alin 0.15
                       => Acum Diana a pus 2.15, Alin a pus 2.15
                        =>in contul comun sunt 2 luni de bani.
                            => si nicio datorie. 


### Contul comun cum il modelez?
Putem plati chestii de acolo, si e 50/50 ownership. (sau ma rog, ratio pe care il decidem).
    Cont normal.
        Orice plata spre contul comun o putem nota.
        Orice plata din contul comun care nu e comuna o putem nota
        Orice plata din contul comun care e comuna o putem nota (optional).
    Alin plateste atat spre contul comun.
    Diana plateste atat
    
Contul comun 

Cheltuieli comune:
    -> Manually tagged de Alin si Diana (din conturile lor)
    -> Contul comun - cheltuieli non-comune accidentale din contul comun.

Tag the bills.

## Feature:
Import - draft bill (notez ca am platit eu X la supermarket),  import pe bune comes at a later time.

## Feature:
Intrebi un agent cum sa faci ceva bazat pe user guide?

## Feature:
Deal with payments to/from other people (Andi intr-o excursie).

## Feature
Support separate budgets (private, for each of us) that join for common stuff (e.g. apartament, cheltuieli comune).

## Feature:
Tax simulation
    Where money is going.
        from salary
        from stocks
        vacation pay logic.

## Feature
Stock selling events. Help with calculating the stock in a particular day.

# Feature
Import payments into the stream

# Feature
Categorize payments and create views:
    Excursii, Banii pe luna,

# Feature
Pointers to other artifacts: factura la utilitati

# Feature 
Notes like experience


## UX Feature: Parallel features.
Support operating on different stream of events. Utitilities such as copy, copy up to a certain time, copy some range of events. etc.


Apartment value.

Tax gains.
    How do we split them by person.
    Net cost of the loan.

Queries:
    interest paid in a year.

State management:   
    State objects with linkage back to events that modified them.
    Create a graph between different objects:


/// Queries:

Next payment, for a given date.
    -> From the loan state at that date.

Interest Paid thus far.
    from the database: 
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

Can different event types generate bills?
    Advance Payment.
    Loan Payment.

Assume the loan creates a bill, how do I override stuff about it?
    Currently I have a way to inject a different payment share event.

    How do I automatically add an event that updates the balance. Split type: update balance, update individual loans.

# Views

At a glance view:
    Per person:
        - remaining principal.
        - interest this year: paid thus far + future.
        - next loan payment
            - recent loan events.
        - balance with cont comun
            - recent bills and transactions that modified this balance

Color code: past, present, future. System generated vs user.

Deeper dives:
    Query language + visualization?
    + query editor.

Allow queries across event streams for effective comparisons

Bill summary.
    Filter by tags
    Color them differently for system generated vs non system generated.


Add a link to local copilot as proxy for the AI interface to query languages.


View the apartment value as he asset value for the loan - for a more optimistic view, instead of how
# ScratchPad

I add a new bill. Add directly to the DB? 
    Live queries?

    Redo the query when processing the event.

Processor shouldn't keep the loan in memory.

    => it should always query the persistence layer.
        

Balance is a query.
Loan is set of queries.
    => LoanCreatedEvent.
    => Bills related to the loan.


I started the discussion from defining the loan in a way that allows easy recomputation of the bills.

But any change to the loan essentially invalidates all future loan bills.

Loan(time) =>
    Loan(time - 1) 


Bills
    If influences balance.
        invalidate all future instances of that balance (needs recalculation).
