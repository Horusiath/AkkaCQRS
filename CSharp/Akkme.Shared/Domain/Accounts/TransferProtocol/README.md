# Transfer protocol

Appology oriented programming

**Source** is an Account aggregate actor wishing to transfer part of it's funds to another account.
**Destination** is an Account aggregate actor which is subject of transfer.

1. Source checks it current balance, and if it's positive, initializes **TransferSaga** actor.
2. Source sends **Transfer** command to transfer saga with data about amount and account id of the Destination account.
3. Transfer saga receives **Transfer** request and emits an event to persist transaction state.
4. Once persisted, saga sends **Withdraw** command to Source and **Deposit** command to Destination. Those events are also persisted.
5. Destination receives **Deposit** and emits corresponding **BalanceChanged** event. Once it's persisted, Destination sends confirmation to a saga.
6. Meanwhile Source received **Withdraw** command and emits corresponding **BalnaceChanged** event.
