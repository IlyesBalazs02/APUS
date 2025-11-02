import { Component, ElementRef, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { debounceTime, distinctUntilChanged, filter, Subscription, switchMap, of, catchError } from 'rxjs';
import { Router } from '@angular/router';

import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatOptionModule } from '@angular/material/core';
import { NgxMatSelectSearchModule } from 'ngx-mat-select-search';

import { FriendSearchService, UserMatch } from './search-bar.service';

@Component({
  selector: 'app-search-bar',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatFormFieldModule,
    MatSelectModule,
    MatOptionModule,
    NgxMatSelectSearchModule
  ],
  templateUrl: './search-bar.component.html',
  styleUrls: ['./search-bar.component.css']
})
export class SearchBarComponent implements OnDestroy {
  selectedCtrl = new FormControl<UserMatch | null>(null);
  filterCtrl = new FormControl<string>('', { nonNullable: true });
  results: UserMatch[] = [];
  private sub = new Subscription();

  constructor(
    private svc: FriendSearchService,
    private router: Router,
    private el: ElementRef
  ) { }

  onOpened() {
    this.sub.add(
      this.filterCtrl.valueChanges.pipe(
        debounceTime(250),
        distinctUntilChanged(),
        filter(q => (q?.trim().length ?? 0) >= 2),
        switchMap(q =>
          this.svc.search(q!.trim()).pipe(catchError(() => of([])))
        )
      ).subscribe(users => (this.results = users))
    );

    queueMicrotask(() => {
      const input: HTMLInputElement | null =
        this.el.nativeElement.querySelector('.ngx-mat-select-search .mat-mdc-input-element');
      input?.focus();
    });
  }

  onClosed() {
    this.filterCtrl.setValue('', { emitEvent: false });
    this.results = [];
    this.selectedCtrl.setValue(null, { emitEvent: false });

    this.sub.unsubscribe();
    this.sub = new Subscription();
  }

  onSelect(user: UserMatch) {
    this.router.navigate(['/profile', user.id]);
    this.onClosed();
  }

  ngOnDestroy() {
    this.sub.unsubscribe();
  }
}
