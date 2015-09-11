/// <reference path="../typings/rx.d.ts" />

export module samples.data {
    
    export class Store<T> {
        updates: Rx.BehaviorSubject<T>;

        constructor(public key: string) {
            this.updates = new Rx.BehaviorSubject<T>(null);
        }
    }

}