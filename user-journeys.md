# User journey
Plan of action:
    Add a basic editor for events.
    Hook it up to a proper DB (or file in OneDrive).
    Create parallel universe feature (clone) to keep main branch correct and add confirmations for the main branch modifications.

Then I can ask CC to implement the additinal features by referring the existing components and with existing patterns.

Map user journeys to the layers and if possible, components.


# Utilities:
    Currency exchange at a specific date
    
## Basic loan management.

/add event editor with the right metadata captured (when the change was made, to which date does it pertain)
/add Query for: interest paid in the last year?
/add interest paid by each person
/add utilitati.

### Utilitati:
-> Add a bill : Title, Value
-> Bill can be split between participants.
-> How does a bill get paid?
    -> The bill is marked as paid.
    -> Proxy accounts are updated with expected values.
    -> Bill can retro-actively be updated by who paid a higher share.
    -> Since last bugetel, Alin paid X in bills, Diana paid Y in bills. -- this is what the state could tell us.
    -> Ce inseamna sa facem settlement la bugetel?
        -> Acum, sa punem bani pe contul comun, si/sau sa ne dam bani intre noi.

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
How to model the loan as a bill?

# Bugetel:
Import - draft (notez ca am platit eu X la supermarket), sau import pe bune.
## Feature:
Intrebi un agent cum sa faci ceva bazat pe user guide?


-> bonus q: how to deal with payements to/ from other people (e.g. Andi intr-o excursie sau similar)

## Support separate budgets that join for common stuff (e.g. apartament, cheltuieli comune).

## Tax simulation
Where money is going.
    from salary
    from stocks
    vacation pay logic.

    Stock selling events. Help with calculating the stock in a particular day.


## To define in code: Bugetel
Experience to import payments into the stream.

Experience to categorize the payments and create views.
    -> Excursii
    -> Bani pe luna.
    -> Pivot 
    
    Vizualizari.

## To Define in code: How to deal with temporary debt between the 2 users?
    As in, if Alin covered more of the loan for a certain month
        But we don't want to mark that as a larger payment towards his subloan
        instead, we should mark that Diana owes Alin X (interest free etc.)

    Alin's Proxy
    Diana's Proxy

    Good for bugetel tracking where we cover who paid for what.
        With bills method. + categorization.

## Integrate with a notes experience.

## References to the outside world:
    ex: factura la utilitati, broken down by ..