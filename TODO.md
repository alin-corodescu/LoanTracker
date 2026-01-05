# Add a user guide with specific user flows:
    -> Advance payment
        -> Correct next payment
    -> Adjust shares for a given payment


    -> Different payment shares for a certain month.
        [x] Done
    -> Scrolling behavior for the dates.
    -> Editor for the event log.
        Date when the modification was made.
        Date of the event it applies to.


# Blazor PWA maybe.
    -> to be able to easily deploy to phones.

# Document architecture (review what the model has written thus far)
-> Text/ JSON file with auditing of when each modification was made.
-> Compiles into a JSON list of events, just with the date when they are applicable.
-> The backend expands the list of events into the actual event stream.
-> The event stream is queried by the frontend and does the rendering.