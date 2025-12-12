import { Injectable, OnDestroy } from '@angular/core';
import { BehaviorSubject, Subscription, interval, switchMap } from 'rxjs';
import { FriendRequestsApi } from './friend-requests.service';

@Injectable({ providedIn: 'root' })
export class FriendRequestCountService implements OnDestroy {
    private _count = new BehaviorSubject<number>(0);
    readonly count$ = this._count.asObservable();

    private pollSub?: Subscription;

    constructor(private api: FriendRequestsApi) { }

    refresh() {
        this.api.getIncomingCount().subscribe({
            next: n => this._count.next(n),
            error: _ => { }
        });
    }

    startPolling(ms = 30000) {
        if (this.pollSub) return;
        this.pollSub = interval(ms)
            .pipe(switchMap(() => this.api.getIncomingCount()))
            .subscribe({
                next: n => this._count.next(n),
                error: _ => { }
            });
        this.refresh();
    }

    stopPolling() {
        this.pollSub?.unsubscribe();
        this.pollSub = undefined;
    }

    ngOnDestroy() { this.stopPolling(); }
}
