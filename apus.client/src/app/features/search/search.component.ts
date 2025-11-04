import { Component, OnInit } from "@angular/core";
import { FormControl } from "@angular/forms";
import { ActivatedRoute, Router } from "@angular/router";

@Component({
    selector: 'app-settings',
    standalone: false,
    templateUrl: './search.component.html',
    styleUrls: ['./search.component.scss']
})

export class SearchComponent implements OnInit {
    q = new FormControl<string>('', { nonNullable: true });

    constructor(private route: ActivatedRoute, private router: Router) { }

    ngOnInit(): void {
        // keep the input in sync with the URL (?q=...)
        this.route.queryParamMap.subscribe(params => {
            const qp = (params.get('q') || '').trim();
            if (qp !== this.q.value) {
                this.q.setValue(qp, { emitEvent: false });
            }
        });
    }

    onSubmit(): void {
        const query = this.q.value.trim();

        // keep current active tab (users/friends/groups)
        const currentTab =
            this.route.firstChild?.snapshot.routeConfig?.path || 'users';

        // navigate to the same tab with the new q
        this.router.navigate(['/search', currentTab], {
            queryParams: { q: query || null },
            queryParamsHandling: 'merge'
        });
    }
}