import { Component, EventEmitter, Output } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { debounceTime, distinctUntilChanged, filter, Observable, of, switchMap } from 'rxjs';
import { FriendSearchService, UserMatch } from '../../services/friend-search.service';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-friend-search',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './friend-search.component.html',
})

export class FriendSearchComponent {
  //Form control bound to the <input [formControl]="query"> in the template.
  //Holds the current text value
  query = new FormControl<string>('', { nonNullable: true });
  results$: Observable<UserMatch[]> = of([]);
  @Output() selected = new EventEmitter<UserMatch>();

  constructor(private svc: FriendSearchService) {
    this.results$ = this.query.valueChanges.pipe(
      debounceTime(250),
      distinctUntilChanged(),
      filter(q => (q?.trim().length ?? 0) >= 2),
      switchMap(q => this.svc.search(q!.trim()))
    );
  }

  choose(user: UserMatch) { this.selected.emit(user); }
}