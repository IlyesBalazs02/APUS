import { Component, ElementRef, HostListener, ViewChild, AfterViewInit, OnDestroy } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { debounceTime, distinctUntilChanged, filter, Observable, of, switchMap } from 'rxjs';
import { FriendSearchService, UserMatch } from '../../services/friend-search.service';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-friend-search',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './friend-search.component.html',
  styleUrls: ['./friend-search.component.css']
})
export class FriendSearchComponent implements AfterViewInit, OnDestroy {
  query = new FormControl<string>('', { nonNullable: true });
  results$: Observable<UserMatch[]> = of([]);

  private resizeHandler!: () => void;

  constructor(private svc: FriendSearchService, private el: ElementRef) {
    this.results$ = this.query.valueChanges.pipe(
      debounceTime(250),
      distinctUntilChanged(),
      filter(q => (q?.trim().length ?? 0) >= 2),
      switchMap(q => this.svc.search(q!.trim()))
    );
  }

  ngAfterViewInit() {
    this.updateDropdownPosition();

    // Recalculate on resize or scroll
    this.resizeHandler = () => this.updateDropdownPosition();
    window.addEventListener('resize', this.resizeHandler);
    window.addEventListener('scroll', this.resizeHandler, true);
  }

  ngOnDestroy() {
    window.removeEventListener('resize', this.resizeHandler);
    window.removeEventListener('scroll', this.resizeHandler, true);
  }

  private updateDropdownPosition() {
    const input = this.el.nativeElement.querySelector('input');
    if (!input) return;

    const rect = input.getBoundingClientRect();
    const root = this.el.nativeElement as HTMLElement;

    root.style.setProperty('--dropdown-x', `${rect.left}px`);
    root.style.setProperty('--dropdown-y', `${rect.bottom + 4}px`);
    root.style.setProperty('--dropdown-width', `${rect.width}px`);
  }

  choose(user: UserMatch) {
    console.log('Selected user:', user);
  }
}
