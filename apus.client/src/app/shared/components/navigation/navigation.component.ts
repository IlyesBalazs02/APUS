import { Component, OnInit } from '@angular/core';
import { Observable } from 'rxjs';
import { AuthService } from '../../../core/services/auth.service';
import { Router } from '@angular/router';
import { FriendRequestCountService } from '../../../features/search/friend-requests/friend-request-count.service';

@Component({
  selector: 'app-navigation',
  standalone: false,
  templateUrl: './navigation.component.html',
  styleUrl: './navigation.component.css'
})
export class NavigationComponent implements OnInit {
  loggedIn$: Observable<boolean>;
  count$!: Observable<number>;

  constructor(private auth: AuthService, private router: Router, private frCount: FriendRequestCountService) {
    this.loggedIn$ = this.auth.loggedIn$;
  }

  ngOnInit(): void {
    this.count$ = this.frCount.count$;
  }

  logout() {
    this.auth.logout();
    this.router.navigate(['/login']);
  }
}
